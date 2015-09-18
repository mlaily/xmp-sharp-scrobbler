// Copyright(c) 2015 Melvyn Laïly
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
using MoreLinq;
using Scrobbling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xmp_sharp_scrobbler_managed
{
    public class SharpScrobbler
    {
        private const string NullSessionKeyErrorMessage = "Please authenticate with Last.fm!";
        private static readonly TimeSpan DefaultErrorBubbleDisplayTime = TimeSpan.FromSeconds(5);

        private Cache cache;
        private Scrobble lastPotentialScrobbleInCaseOfCacheFailure;
        private object lastPotentialScrobbleInCaseOfCacheFailureLock = new object();

        public string SessionKey { get; set; }

        public SharpScrobbler()
        {
            cache = new Cache();
        }

        public string AskUserForNewAuthorizedSessionKey(IntPtr ownerWindowHandle)
        {
            Configuration configurationForm = new Configuration();
            if (configurationForm.ShowDialog(new Win32Window(ownerWindowHandle)) == DialogResult.OK)
            {
                // refresh with the new session key
                SessionKey = configurationForm.SessionKey;
            }
            return SessionKey;
        }

        public async void OnTrackStartsPlaying(string artist, string track, string album, int durationMs, string trackNumber, string mbid)
        {
            NowPlaying nowPlaying = CreateScrobble(artist, track, album, durationMs, trackNumber, mbid);
            if (SessionKey != null)
            {
                await ShowBubbleOnErrorAsync(Track.UpdateNowPlaying(SessionKey, nowPlaying));
            }
            else
            {
                ShowErrorBubble(NullSessionKeyErrorMessage);
            }
        }

        public async void OnTrackCanScrobble(string artist, string track, string album, int durationMs, string trackNumber, string mbid, long utcUnixTimestamp)
        {
            Scrobble scrobble = CreateScrobble(artist, track, album, durationMs, trackNumber, mbid, utcUnixTimestamp);
            // Cache the scrobble and wait for the end of the track to actually send it.
            try
            {
                await cache.StoreAsync(scrobble);
            }
            catch (Exception ex)
            {
                lock (lastPotentialScrobbleInCaseOfCacheFailureLock)
                {
                    lastPotentialScrobbleInCaseOfCacheFailure = scrobble;
                }
                ShowErrorBubble(ex);
            }
        }

        public async void OnTrackCompletes()
        {
            // If caching failed and we have a track only in memory waiting to be scrobbled, try to do it now.
            // We copy the reference atomically to avoid locking while we try to execute the web request.
            Scrobble fromLastPotentialScrobbleInCaseOfCacheFailure = null;
            lock (lastPotentialScrobbleInCaseOfCacheFailureLock)
            {
                if (lastPotentialScrobbleInCaseOfCacheFailure != null)
                {
                    fromLastPotentialScrobbleInCaseOfCacheFailure = lastPotentialScrobbleInCaseOfCacheFailure;
                    lastPotentialScrobbleInCaseOfCacheFailure = null;
                }
            }
            if (fromLastPotentialScrobbleInCaseOfCacheFailure != null)
            {
                // We don't even try to catch any potential exception since this code path is
                // already a fallback in case the cache file cannot be accessed.
                // If this scrobble fails, it will be lost anyway.
                try
                {
                    if (SessionKey == null)
                    {
                        ShowErrorBubble(NullSessionKeyErrorMessage);
                        return;
                    }
                    await Track.Scrobble(SessionKey, fromLastPotentialScrobbleInCaseOfCacheFailure);
                }
                catch { }
            }

            // Try to scrobble the cache content.
            try
            {
                var retrievalResult = await cache.RetrieveAsync();
                if (retrievalResult.Scrobbles.Any())
                {
                    // We have something!
                    var partitions = retrievalResult.Scrobbles.Batch(50); // We can only scrobble 50 tracks at the same time.
                    foreach (var partition in partitions)
                    {
                        var eagerPartition = partition.ToList();
                        var success = await HandleScrobblingAsync(eagerPartition);
                        if (success)
                        {
                            // Now we need to remove the successfully scrobbled tracks from the cache.
                            await cache.RemoveScrobblesAsync(eagerPartition);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Cache error (Scrobbling errors are catched inside HandleScrobblingAsync())
                // TODO: log
            }
        }

        private async Task<bool> HandleScrobblingAsync(IEnumerable<Scrobble> scrobbles)
        {
            if (SessionKey == null)
            {
                ShowErrorBubble(NullSessionKeyErrorMessage);
                return false;
            }
            try
            {
                // Try scrobbling the current scrobble(s).
                var scrobblingResult = await Track.Scrobble(SessionKey, scrobbles);
                if (scrobblingResult.Success)
                {
                    return true;
                }
                else
                {
                    // Check the error reported by Last.fm.
                    // 9. Invalid session key - Please re-authenticate
                    if (scrobblingResult.Error.Code == 9)
                    {
                        ShowErrorBubble(scrobblingResult.Error.Message);
                        // Useless to continue until we have a valid session key.
                        return false;
                    }
                    // 11.Service Offline - This service is temporarily offline, try again later.
                    // 16.The service is temporarily unavailable, please try again.
                    else if (scrobblingResult.Error.Code == 11 || scrobblingResult.Error.Code == 16)
                    {
                        // TODO: log
                        // Useless to continue right now.
                        return false;
                    }
                    else
                    {
                        // Unknown error code: failure, the scrobble is probably invalid
                        // TODO: log
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                // TODO: log
                // Probably a networking error, useless to continue right now.
                return false;
            }
        }

        private async Task ShowBubbleOnErrorAsync<T>(Task<ApiResponse<T>> request)
        {
            try
            {
                var response = await request;
                if (!response.Success)
                {
                    ShowErrorBubble(response.Error.Message);
                    // TODO: log
                }
            }
            catch (Exception ex)
            {
                ShowErrorBubble(ex);
                // TODO: log
            }
        }

        private static void ShowErrorBubble(string message)
        {
            Util.ShowInfoBubble($"Scrobbler Error! {message}", DefaultErrorBubbleDisplayTime);
        }
        private static void ShowErrorBubble(Exception ex)
        {
            Util.ShowInfoBubble($"Scrobbler Error! {ex?.GetType()?.Name + " - " ?? ""}{ex.Message}", DefaultErrorBubbleDisplayTime);
        }

        private static Scrobble CreateScrobble(string artist, string track, string album, int durationMs, string trackNumber, string mbid, long utcUnixTimestamp = 0)
            => new Scrobble(artist, track, DateTimeOffset.FromUnixTimeSeconds(utcUnixTimestamp))
            {
                Album = string.IsNullOrWhiteSpace(album) ? null : album,
                Duration = durationMs <= 0 ? null : new TimeSpan?(TimeSpan.FromMilliseconds(durationMs)),
                TrackNumber = string.IsNullOrWhiteSpace(trackNumber) ? null : trackNumber,
                Mbid = string.IsNullOrWhiteSpace(mbid) ? null : mbid,
            };
    }
}