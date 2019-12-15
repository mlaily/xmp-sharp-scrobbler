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
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Scrobbling;
using XmpSharpScrobbler;
using XmpSharpScrobbler.Misc;
using Xunit;

namespace UnitTests
{
    public class ScrobbleSerializerTests
    {
        private static readonly IEqualityComparer<Scrobble> _equalityComparer = new ScrobbleEqualityComparer();

        public static IEnumerable<object[]> SerializationTestData()
        {
            {
                var scrobble = new Scrobble("", "", DateTimeOffset.FromUnixTimeMilliseconds(0));
                var serialized = "0&&&&&&&";
                yield return new object[] { scrobble, serialized };
            }

            {
                var scrobble = new Scrobble("track", "artist", DateTimeOffset.FromUnixTimeMilliseconds(123_456))
                {
                    Album = "album",
                    AlbumArtist = "albumArtist",
                    Duration = TimeSpan.FromSeconds(789),
                    Mbid = "mbid",
                    TrackNumber = "trackNumber"
                };
                var serialized = "123&track&artist&album&albumArtist&trackNumber&mbid&789";
                yield return new object[] { scrobble, serialized };
            }

            {
                var scrobble = new Scrobble("♥", "♥", DateTimeOffset.FromUnixTimeMilliseconds(123_456))
                {
                    Album = "♥",
                    AlbumArtist = "♥",
                    Duration = TimeSpan.FromSeconds(789),
                    Mbid = "♥",
                    TrackNumber = "♥"
                };
                var serialized = "123&%E2%99%A5&%E2%99%A5&%E2%99%A5&%E2%99%A5&%E2%99%A5&%E2%99%A5&789";
                yield return new object[] { scrobble, serialized };
            }

            {
                var scrobble = new Scrobble("&♥", "&♥", DateTimeOffset.FromUnixTimeMilliseconds(123_456))
                {
                    Album = "&♥",
                    AlbumArtist = "&♥",
                    Duration = TimeSpan.FromSeconds(789),
                    Mbid = "&♥",
                    TrackNumber = "&♥"
                };
                var serialized = "123&%26%E2%99%A5&%26%E2%99%A5&%26%E2%99%A5&%26%E2%99%A5&%26%E2%99%A5&%26%E2%99%A5&789";
                yield return new object[] { scrobble, serialized };
            }

            {
                var scrobble = new Scrobble(default, default, default);
                var serialized = "-62135596800&&&&&&&";
                yield return new object[] { scrobble, serialized };
            }
        }

        [Theory]
        [MemberData(nameof(SerializationTestData))]
        public void Serialize_Returns_Expected_Result(Scrobble scrobble, string expected)
        {
            // Act

            var actual = ScrobbleSerializer.Serialize(scrobble);

            // Assert

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(SerializationTestData))]
        public void Deerialize_Returns_Expected_Result(Scrobble expected, string serialized)
        {
            // Act

            var actual = ScrobbleSerializer.Deserialize(serialized);

            // Assert

            Assert.Equal(expected, actual, _equalityComparer);
        }
    }
}
