// Copyright(c) 2015-2019 Melvyn Laïly
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MoreLinq;
using Scrobbling;
using XmpSharpScrobbler.PluginInfrastructure;

namespace XmpSharpScrobbler
{
    public class SharpScrobbler : IDisposable
    {
        private const string NullSessionKeyErrorMessage = "Please authenticate with Last.fm!";
        private static readonly TimeSpan DefaultErrorBubbleDisplayTime = TimeSpan.FromSeconds(5);

        private Cache _cache;
        private Scrobble _lastPotentialScrobbleInCaseOfCacheFailure;
        private object _lastPotentialScrobbleInCaseOfCacheFailureLock = new object();

        public string SessionKey { get; private set; }
        public void SetSessionKey(string key) => SessionKey = key;

        public SharpScrobbler()
        {
            _cache = new Cache();
        }

        public string AskUserForNewAuthorizedSessionKey(IntPtr ownerWindowHandle)
        {
            using (var configurationForm = new Configuration())
            {
                if (configurationForm.ShowDialog(new Win32Window(ownerWindowHandle)) == DialogResult.OK)
                {
                    // refresh with the new session key
                    SessionKey = configurationForm.SessionKey;
                }
                return SessionKey;
            }
        }

        public async void OnTrackStartsPlaying(string title, string artist, string album, string trackNumber, int durationMs)
        {
            NowPlaying nowPlaying = CreateScrobble(title, artist, album, trackNumber, null, durationMs);

            Logger.Log(LogLevel.Info, $"Now playing: '{title}', artist: '{artist}', album: '{album}'");

            if (SessionKey != null)
            {
                await ShowBubbleOnErrorAsync(Track.UpdateNowPlaying(SessionKey, nowPlaying));
            }
            else
            {
                Logger.Log(LogLevel.Warn, $"Invalid session key. {NullSessionKeyErrorMessage}");
                ShowErrorBubble(NullSessionKeyErrorMessage);
            }
        }

        public async void OnTrackCanScrobble(string title, string artist, string album, string trackNumber, int durationMs, long utcUnixTimestamp)
        {
            Scrobble scrobble = CreateScrobble(title, artist, album, trackNumber, null, durationMs, utcUnixTimestamp);

            Logger.Log(LogLevel.Info, $"Track played on {scrobble.Timestamp.ToLocalTime():s} ready to scrobble: '{title}', artist: '{artist}', album: '{album}'");

            // Cache the scrobble and wait for the end of the track to actually send it.
            try
            {
                await _cache.StoreAsync(scrobble);
            }
            catch (Exception ex)
            {
                lock (_lastPotentialScrobbleInCaseOfCacheFailureLock)
                {
                    _lastPotentialScrobbleInCaseOfCacheFailure = scrobble;
                }
                Logger.Log(LogLevel.Warn, $"An error occured while trying to store a scrobble into the cache file: {ex}");
                ShowErrorBubble(ex);
            }
        }

        public async void OnTrackCompletes()
        {
            // If caching failed and we have a track only in memory waiting to be scrobbled, try to do it now.
            // We copy the reference atomically to avoid locking while we try to execute the web request.
            Scrobble fromLastPotentialScrobbleInCaseOfCacheFailure = null;
            lock (_lastPotentialScrobbleInCaseOfCacheFailureLock)
            {
                if (_lastPotentialScrobbleInCaseOfCacheFailure != null)
                {
                    fromLastPotentialScrobbleInCaseOfCacheFailure = _lastPotentialScrobbleInCaseOfCacheFailure;
                    _lastPotentialScrobbleInCaseOfCacheFailure = null;
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
                catch
                {
                    Logger.Log(LogLevel.Warn,
                        $"An error occured while trying to scrobble the track '{fromLastPotentialScrobbleInCaseOfCacheFailure.Track}'. The cache is disabled, so the scrobble will be lost.");
                }
            }

            // Try to scrobble the cache content.
            try
            {
                var retrievalResult = await _cache.RetrieveAsync();
                if (retrievalResult.Scrobbles.Any())
                {
                    // We have something!
                    Logger.Log(LogLevel.Vrbs, $"{retrievalResult.Scrobbles.Count} scrobble{(retrievalResult.Scrobbles.Count > 1 ? "s" : "")} found in the cache.");

                    var partitions = retrievalResult.Scrobbles.Batch(50); // We can only scrobble 50 tracks at the same time.
                    foreach (var partition in partitions)
                    {
                        var eagerPartition = partition.ToList();
                        var success = await HandleScrobblingAsync(eagerPartition);
                        if (success)
                        {
                            // Now we need to remove the successfully scrobbled tracks from the cache.
                            await _cache.RemoveScrobblesAsync(eagerPartition);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Cache error (Scrobbling errors are catched inside HandleScrobblingAsync())
                Logger.Log(LogLevel.Warn, $"An error occured while trying to retrieve scrobbles from the cache: {ex}");
            }
        }

        /// <returns>True on success, False otherwise.</returns>
        private async Task<bool> HandleScrobblingAsync(IReadOnlyCollection<Scrobble> scrobbles)
        {
            if (SessionKey == null)
            {
                ShowErrorBubble(NullSessionKeyErrorMessage);
                return false;
            }
            try
            {
                // Try scrobbling the current scrobble(s).
                Logger.Log(LogLevel.Vrbs, $"Sending {scrobbles.Count} scrobble{(scrobbles.Count > 1 ? "s" : "")}.");
                var scrobblingResult = await Track.Scrobble(SessionKey, scrobbles);
                if (scrobblingResult.Success)
                {
                    return true;
                }
                else
                {
                    Logger.Log(LogLevel.Warn, $"Error received from Last.fm: {scrobblingResult.Error.Code}. {scrobblingResult.Error.Message}");
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
                        // Useless to continue right now.
                        return false;
                    }
                    else
                    {
                        // Unknown error code: failure, the scrobble is probably invalid
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warn, $"An error occured while trying to send the scrobbles: {ex}");
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
            Imports.ShowInfoBubble($"Scrobbler Error! {message}", DefaultErrorBubbleDisplayTime);
        }
        private static void ShowErrorBubble(Exception ex)
        {
            Imports.ShowInfoBubble($"Scrobbler Error! {ex?.GetType()?.Name + " - " ?? ""}{ex.Message}", DefaultErrorBubbleDisplayTime);
        }

        private static Scrobble CreateScrobble(string title, string artist, string album, string trackNumber, string mbid, int durationMs, long utcUnixTimestamp = 0)
            => new Scrobble(title, artist, DateTimeOffset.FromUnixTimeSeconds(utcUnixTimestamp))
            {
                Album = string.IsNullOrWhiteSpace(album) ? null : album,
                TrackNumber = string.IsNullOrWhiteSpace(trackNumber) ? null : trackNumber,
                Mbid = string.IsNullOrWhiteSpace(mbid) ? null : mbid,
                Duration = durationMs <= 0 ? null : new TimeSpan?(TimeSpan.FromMilliseconds(durationMs)),
            };

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cache.Dispose();
            }
        }
    }
}