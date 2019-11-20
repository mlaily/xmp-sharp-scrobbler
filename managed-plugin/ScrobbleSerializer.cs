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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Scrobbling;

namespace XmpSharpScrobbler
{
    internal static class ScrobbleSerializer
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

            DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(Decode(encodedFields[0]), CultureInfo.InvariantCulture));

            TimeSpan? duration;
            string durationField = Decode(encodedFields[7]);
            duration = string.IsNullOrWhiteSpace(durationField) ? (TimeSpan?)null : TimeSpan.FromSeconds(int.Parse(durationField, CultureInfo.InvariantCulture));

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
