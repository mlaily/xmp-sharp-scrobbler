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
        private static readonly TimeSpan ErrorBubbleDisplayTime = TimeSpan.FromSeconds(5);
        public string SessionKey { get; set; }

        public async void NowPlaying(string artist, string track, string album, int durationMs, string trackNumber, string mbid)
        {
            NowPlaying nowPlaying = CreateScrobble(artist, track, album, durationMs, trackNumber, mbid);
            await ShowBubbleOnErrorAsync(Track.UpdateNowPlaying(SessionKey, nowPlaying));
        }

        public async void Scrobble(string artist, string track, string album, int durationMs, string trackNumber, string mbid, long utcUnixTimestamp)
        {
            Scrobble scrobble = CreateScrobble(artist, track, album, durationMs, trackNumber, mbid, utcUnixTimestamp);
            await ShowBubbleOnErrorAsync(Track.Scrobble(SessionKey, scrobble));
        }

        private async Task ShowBubbleOnErrorAsync<T>(Task<ApiResponse<T>> request)
        {
            try
            {
                var response = await request;
                if (!response.Success)
                {
                    Util.ShowInfoBubble($"XMPlay Sharp Scrobbler: Error! {response.Error.Message}", ErrorBubbleDisplayTime);
                    // TODO: log
                }
            }
            catch (Exception)
            {
                // TODO: log
            }
        }

        public string AskUserForNewAuthorizedSessionKey(IntPtr ownerWindowHandle)
        {
            Configuration configurationForm = new Configuration();
            if (configurationForm.ShowDialog(new Win32Window(ownerWindowHandle)) == System.Windows.Forms.DialogResult.OK)
            {
                // refresh with the new session key
                SessionKey = configurationForm.SessionKey;
            }
            return SessionKey;
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