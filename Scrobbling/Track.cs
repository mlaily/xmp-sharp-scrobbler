﻿using System;
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
        public static Task<ApiResponse<string>> UpdateNowPlaying(string sessionKey, NowPlaying nowPlaying)
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

        public static Task<ApiResponse<string>> Scrobble(string sessionKey, IEnumerable<Scrobble> scrobbles)
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

        public static Task<ApiResponse<string>> Scrobble(string sessionKey, Scrobble scrobble)
            => Scrobble(sessionKey, new[] { scrobble });
    }
}
