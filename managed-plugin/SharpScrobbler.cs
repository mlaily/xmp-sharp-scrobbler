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
        private static readonly TimeSpan DefaultErrorBubbleDisplayTime = TimeSpan.FromSeconds(5);

        private Cache cache;

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
            await ShowBubbleOnErrorAsync(Track.UpdateNowPlaying(SessionKey, nowPlaying));
        }

        public async void OnTrackCanScrobble(string artist, string track, string album, int durationMs, string trackNumber, string mbid, long utcUnixTimestamp)
        {
            Scrobble scrobble = CreateScrobble(artist, track, album, durationMs, trackNumber, mbid, utcUnixTimestamp);

            await HandleScrobblingAsync(isSingleNewScrobble: true, scrobbles: new[] { scrobble });

            // Now we try and see if we can scrobble the cache content.
            try
            {
                var cachedScrobbles = await cache.RetrieveAsync();
                if (cachedScrobbles.Any())
                {
                    // We have something!
                    var partitions = cachedScrobbles.Batch(50);// We can only scrobble 50 tracks at the same time.
                    foreach (var partition in partitions)
                    {
                        var eagerPartition = partition.ToList();
                        var success = await HandleScrobblingAsync(isSingleNewScrobble: false, scrobbles: eagerPartition);
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

        private async Task<bool> HandleScrobblingAsync(bool isSingleNewScrobble, IEnumerable<Scrobble> scrobbles)
        {
            Scrobble newScrobble = null;
            // If this is a new single scrobble, we will have to cache it on error.
            if (isSingleNewScrobble) newScrobble = scrobbles.Single();

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
                        if (isSingleNewScrobble) await cache.StoreAsync(newScrobble);
                        // Useless to continue until we have a valid session key.
                        return false;
                    }
                    // 11.Service Offline - This service is temporarily offline, try again later.
                    // 16.The service is temporarily unavailable, please try again.
                    else if (scrobblingResult.Error.Code == 11 || scrobblingResult.Error.Code == 16)
                    {
                        // TODO: log
                        if (isSingleNewScrobble) await cache.StoreAsync(newScrobble);
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
                // Probably a networking error - cache the scrobble for later.
                if (isSingleNewScrobble) await cache.StoreAsync(newScrobble);
                // Useless to continue right now.
                return false;
            }
        }

        public void OnTrackCompletes()
        {

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
                ShowErrorBubble($"{ex?.GetType()?.Name + " - " ?? ""}{ex.Message}");
                // TODO: log
            }
        }

        private static void ShowErrorBubble(string message)
        {
            Util.ShowInfoBubble($"XMPlay Sharp Scrobbler: Error! {message}", DefaultErrorBubbleDisplayTime);
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