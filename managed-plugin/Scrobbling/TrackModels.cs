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
using System.Globalization;
using System.Text;

namespace Scrobbling
{
    public class NowPlaying
    {
        /// <summary>
        /// track (Required) : The track name.
        /// </summary>
        public string Track { get; }
        /// <summary>
        /// artist (Required) : The artist name.
        /// </summary>
        public string Artist { get; }
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
        /// Properly formatted read-only duration in full (integer) seconds.
        /// Return null if <see cref="Duration"/> is null.
        /// </summary>
        public string StringDuration
            => Duration == null ? null : ((int)Math.Round(Duration.Value.TotalSeconds, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture);

        public NowPlaying(string track, string artist)
        {
            Track = track;
            Artist = artist;
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

        public Scrobble(string track, string artist, DateTimeOffset timestamp)
            : base(track, artist)
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

            return (x.Track ?? "") == (y.Track ?? "")
                && (x.Artist ?? "") == (y.Artist ?? "")
                && (x.Album ?? "") == (y.Album ?? "")
                && (x.AlbumArtist ?? "") == (y.AlbumArtist ?? "")
                && (x.Mbid ?? "") == (y.Mbid ?? "")
                && (x.TrackNumber ?? "") == (y.TrackNumber ?? "")
                && x.StringDuration == y.StringDuration
                && x.StringTimestamp == y.StringTimestamp;
        }

        public int GetHashCode(Scrobble obj)
        {
            unchecked
            {
                int hash = 17;
                if (obj != null)
                {
                    hash = hash * 23 + (string.IsNullOrEmpty(obj.Track) ? 0 : obj.Track.GetHashCode());
                    hash = hash * 23 + (string.IsNullOrEmpty(obj.Artist) ? 0 : obj.Artist.GetHashCode());
                    hash = hash * 23 + (string.IsNullOrEmpty(obj.Album) ? 0 : obj.Album.GetHashCode());
                    hash = hash * 23 + (string.IsNullOrEmpty(obj.AlbumArtist) ? 0 : obj.AlbumArtist.GetHashCode());
                    hash = hash * 23 + (string.IsNullOrEmpty(obj.Mbid) ? 0 : obj.Mbid.GetHashCode());
                    hash = hash * 23 + (string.IsNullOrEmpty(obj.TrackNumber) ? 0 : obj.TrackNumber.GetHashCode());
                    hash = hash * 23 + (string.IsNullOrEmpty(obj.StringDuration) ? 0 : obj.StringDuration.GetHashCode());
                    hash = hash * 23 + (string.IsNullOrEmpty(obj.StringTimestamp) ? 0 : obj.StringTimestamp.GetHashCode());
                }
                return hash;
            }
        }
    }
}
