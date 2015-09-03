using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Xml.Linq;

namespace Scrobbling
{
    public static class Track
    {
        /// <param name="sessionKey">authentication token.</param>
        public static Task<ApiResult<string>> UpdateNowPlaying(string sessionKey, NowPlaying nowPlaying)
        {
            if (sessionKey == null) throw new ArgumentNullException(nameof(sessionKey));
            if (nowPlaying == null) throw new ArgumentNullException(nameof(nowPlaying));
            if (nowPlaying.Artist == null) throw new ArgumentException("A required NowPlaying property was not provided.", nameof(nowPlaying.Artist));
            if (nowPlaying.Track == null) throw new ArgumentException("A required NowPlaying property was not provided.", nameof(nowPlaying.Track));

            List<ApiArg> args = new List<ApiArg>();
            // required
            args.Add(new ApiArg("artist", nowPlaying.Artist));
            args.Add(new ApiArg("track", nowPlaying.Track));
            // optional
            if (nowPlaying.Album != null) args.Add(new ApiArg("album", nowPlaying.Album));
            if (nowPlaying.TrackNumber != null) args.Add(new ApiArg("trackNumber", nowPlaying.TrackNumber));
            if (nowPlaying.Mbid != null) args.Add(new ApiArg("mbid", nowPlaying.Mbid));
            if (nowPlaying.StringDuration != null) args.Add(new ApiArg("duration", nowPlaying.StringDuration));
            if (nowPlaying.AlbumArtist != null) args.Add(new ApiArg("albumArtist", nowPlaying.AlbumArtist));

            return Common.PostAsync("track.updateNowPlaying", sessionKey, x => x.Value, args.ToArray());
        }

        public static Task<ApiResult<string>> Scrobble(string sessionKey, IEnumerable<Scrobble> scrobbles)
        {
            if (sessionKey == null) throw new ArgumentNullException(nameof(sessionKey));
            if (scrobbles == null) throw new ArgumentNullException(nameof(scrobbles));

            List<ApiArg> args = new List<ApiArg>();
            int i = 0;
            foreach (var scrobble in scrobbles)
            {
                if (i > 49) // [0 <= i <= 49]
                {
                    // the exception is thrown only now so that we don't have to eagerly enumerate the sequence before
                    throw new ArgumentException("No more than 50 scrobbles can be sent in the same request.", nameof(scrobbles));
                }
                // required
                args.Add(new ApiArg($"artist[{i}]", scrobble.Artist));
                args.Add(new ApiArg($"track[{i}]", scrobble.Track));
                args.Add(new ApiArg($"timestamp[{i}]", scrobble.StringTimestamp));
                // optional
                if (scrobble.Album != null) args.Add(new ApiArg($"album[{i}]", scrobble.Album));
                if (scrobble.TrackNumber != null) args.Add(new ApiArg($"trackNumber[{i}]", scrobble.TrackNumber));
                if (scrobble.Mbid != null) args.Add(new ApiArg($"mbid[{i}]", scrobble.Mbid));
                if (scrobble.StringDuration != null) args.Add(new ApiArg($"duration[{i}]", scrobble.StringDuration));
                if (scrobble.AlbumArtist != null) args.Add(new ApiArg($"albumArtist[{i}]", scrobble.AlbumArtist));
                i++;
            }

            return Common.PostAsync("track.scrobble", sessionKey, x => x.Value, args.ToArray());
        }

        public static Task<ApiResult<string>> Scrobble(string sessionKey, Scrobble scrobble)
            => Scrobble(sessionKey, new[] { scrobble });
    }

    public class NowPlaying
    {
        /// <summary>
        /// artist (Required) : The artist name.
        /// </summary>
        public string Artist { get; }
        /// <summary>
        /// track (Required) : The track name.
        /// </summary>
        public string Track { get; }
        /// <summary>
        /// album (Optional) : The album name.
        /// </summary>
        public string Album { get; set; }
        /// <summary>
        /// trackNumber (Optional) : The track number of the track on the album.
        /// </summary>
        public string TrackNumber { get; set; }
        /// <summary>
        /// mbid (Optional) : The MusicBrainz Track ID.
        /// </summary>
        public string Mbid { get; set; }
        /// <summary>
        /// albumArtist (Optional) : The album artist - if this differs from the track artist.
        /// </summary>
        public string AlbumArtist { get; set; }
        /// <summary>
        /// duration (Optional) : The length of the track in seconds.
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Properly formatted read-only duration.
        /// Return null if <see cref="Duration"/> is null.
        /// </summary>
        public string StringDuration => Duration?.TotalSeconds.ToString(NumberFormatInfo.InvariantInfo);

        public NowPlaying(string artist, string track)
        {
            Artist = artist;
            Track = track;
        }
    }

    public class Scrobble : NowPlaying
    {
        /// <summary>
        /// (Required) : The time the track started playing, in UNIX timestamp format (integer number of seconds since 00:00:00, January 1st 1970 UTC). This must be in the UTC time zone.
        /// If this instance is not in the UTC time zone, but provides time zone info, <see cref="StringTimestamp"/> will be correctly converted to a UTC UNIX timestamp.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Properly formatted read-only UNIX UTC timestamp.
        /// </summary>
        public string StringTimestamp => Timestamp.ToUniversalTime().ToUnixTimeSeconds().ToString(NumberFormatInfo.InvariantInfo);

        public Scrobble(string artist, string track, DateTimeOffset timestamp)
            : base(artist, track)
        {
            Timestamp = timestamp;
        }
    }
}
