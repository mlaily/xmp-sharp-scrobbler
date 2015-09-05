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

        public void NowPlaying(string artist, string track, string album, int durationMs, string trackNumber, string mbid)
        {
            NowPlaying nowPlaying = new NowPlaying(artist, track)
            {
                Album = string.IsNullOrWhiteSpace(album) ? null : album,
                Duration = durationMs <= 0 ? null : new TimeSpan?(TimeSpan.FromMilliseconds(durationMs)),
                TrackNumber = string.IsNullOrWhiteSpace(trackNumber) ? null : trackNumber,
                Mbid = string.IsNullOrWhiteSpace(mbid) ? null : mbid,
            };
            Track.UpdateNowPlaying(SessionKey, nowPlaying).ContinueWith(x => { });
        }

        public void Scrobble(string artist, string track, string album, int durationMs, string trackNumber, string mbid, long utcUnixTimestamp)
        {
            Scrobble scrobble = new Scrobble(artist, track, DateTimeOffset.FromUnixTimeSeconds(utcUnixTimestamp))
            {
                Album = string.IsNullOrWhiteSpace(album) ? null : album,
                Duration = durationMs <= 0 ? null : new TimeSpan?(TimeSpan.FromMilliseconds(durationMs)),
                TrackNumber = string.IsNullOrWhiteSpace(trackNumber) ? null : trackNumber,
                Mbid = string.IsNullOrWhiteSpace(mbid) ? null : mbid,
            };
            Track.Scrobble(SessionKey, scrobble).ContinueWith(x => { });
        }


        public static void Initialize()
        {

        }

        public static string AskUserForNewAuthorizedSessionKey(IntPtr ownerWindowHandle)
        {
            Configuration configurationForm = new Configuration();
            if (configurationForm.ShowDialog(new Win32Window(ownerWindowHandle)) == System.Windows.Forms.DialogResult.OK)
            {

            }
            throw new NotImplementedException();
        }
    }
}