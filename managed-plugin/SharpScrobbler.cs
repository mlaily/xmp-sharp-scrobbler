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
        public string SessionKey { get; set; }

        public async void NowPlaying(string artist, string track, string album, int durationMs, string trackNumber, string mbid)
        {
            NowPlaying nowPlaying = CreateScrobble(artist, track, album, durationMs, trackNumber, mbid);
            try
            {
                var response = await Track.UpdateNowPlaying(SessionKey, nowPlaying);
                if (!response.Success)
                {
                    // TODO: log
                }
            }
            catch (Exception)
            {
                // TODO: log
            }
        }

        public void Scrobble(string artist, string track, string album, int durationMs, string trackNumber, string mbid, long utcUnixTimestamp)
        {
            Scrobble scrobble = CreateScrobble(artist, track, album, durationMs, trackNumber, mbid, utcUnixTimestamp);
            Track.Scrobble(SessionKey, scrobble).ContinueWith(x => { });
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