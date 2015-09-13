using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Scrobbling
{
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

    public class ScrobbleEqualityComparer : IEqualityComparer<Scrobble>
    {
        public bool Equals(Scrobble x, Scrobble y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null ^ y == null) return false;

            return x.Track == y.Track
                && x.Artist == y.Artist
                && x.Album == y.Album
                && x.AlbumArtist == y.AlbumArtist
                && x.Mbid == y.Mbid
                && x.Timestamp == y.Timestamp;
        }

        public int GetHashCode(Scrobble obj)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (obj.Track?.GetHashCode() ?? 0);
                hash = hash * 23 + (obj.Artist?.GetHashCode() ?? 0);
                hash = hash * 23 + (obj.Album?.GetHashCode() ?? 0);
                hash = hash * 23 + (obj.AlbumArtist?.GetHashCode() ?? 0);
                hash = hash * 23 + (obj.Mbid?.GetHashCode() ?? 0);
                hash = hash * 23 + obj.Timestamp.GetHashCode();
                return hash;
            }
        }
    }
}
