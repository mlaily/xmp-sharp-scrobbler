using Scrobbling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace xmp_sharp_scrobbler_managed
{
    public static class ScrobbleSerializer
    {
        private const char FieldSeparator = '&';
        public static string Serialize(Scrobble scrobble)
        {
            var sb = new StringBuilder();
            sb.Append(Encode(scrobble.StringTimestamp)); sb.Append(FieldSeparator);
            sb.Append(Encode(scrobble.Track)); sb.Append(FieldSeparator);
            sb.Append(Encode(scrobble.Artist)); sb.Append(FieldSeparator);
            sb.Append(Encode(scrobble.Album)); sb.Append(FieldSeparator);
            sb.Append(Encode(scrobble.AlbumArtist)); sb.Append(FieldSeparator);
            sb.Append(Encode(scrobble.TrackNumber)); sb.Append(FieldSeparator);
            sb.Append(Encode(scrobble.Mbid)); sb.Append(FieldSeparator);
            sb.Append(Encode(scrobble.StringDuration));
            return sb.ToString();
        }

        public static Scrobble Deserialize(string serializedScrobble)
        {
            if (serializedScrobble == null) throw new ArgumentNullException(nameof(serializedScrobble));

            var encodedFields = serializedScrobble.Split(FieldSeparator);
            if (encodedFields.Length != 8) throw new ArgumentException("Wrong number of fields.", nameof(serializedScrobble));

            DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(Decode(encodedFields[0])));

            TimeSpan? duration;
            string durationField = Decode(encodedFields[7]);
            duration = string.IsNullOrWhiteSpace(durationField) ? (TimeSpan?)null : TimeSpan.FromSeconds(int.Parse(durationField));

            var result = new Scrobble(artist: Decode(encodedFields[2]), track: Decode(encodedFields[1]), timestamp: timestamp)
            {
                Album = Decode(encodedFields[3]),
                AlbumArtist = Decode(encodedFields[4]),
                TrackNumber = Decode(encodedFields[5]),
                Mbid = Decode(encodedFields[6]),
                Duration = duration,
            };

            return result;
        }

        /// <summary>
        /// Encode illegal characters for serialization.
        /// </summary>
        private static string Encode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            else
                return Uri.EscapeDataString(value);
        }

        /// <summary>
        /// Decode a serialized value.
        /// </summary>
        private static string Decode(string encodedValue)
        {
            if (string.IsNullOrEmpty(encodedValue))
                return "";
            else
                return Uri.UnescapeDataString(encodedValue);
        }
    }
}
