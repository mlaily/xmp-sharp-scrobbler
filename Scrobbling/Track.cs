using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Scrobbling
{
    public static class Track
    {
        /// <param name="sessionKey">authentication token.</param>
        /// <param name="artist">artist (Required) : The artist name.</param>
        /// <param name="track">track (Required) : The track name.</param>
        /// <param name="album">album (Optional) : The album name.</param>
        /// <param name="trackNumber">trackNumber (Optional) : The track number of the track on the album.</param>
        /// <param name="mbid">mbid (Optional) : The MusicBrainz Track ID.</param>
        /// <param name="duration">duration (Optional) : The length of the track in seconds.</param>
        /// <param name="albumArtist">albumArtist (Optional) : The album artist - if this differs from the track artist.</param>
        public static Task<ApiResult<string>> UpdateNowPlaying(
            string sessionKey,
            string artist,
            string track,
            string album = null,
            string trackNumber = null,
            string mbid = null,
            TimeSpan? duration = null,
            string albumArtist = null)
        {
            if (sessionKey == null) throw new ArgumentNullException(nameof(sessionKey));
            if (artist == null) throw new ArgumentNullException(nameof(artist));
            if (track == null) throw new ArgumentNullException(nameof(track));

            List<ApiArg> args = new List<ApiArg>();
            // required
            args.Add(new ApiArg("artist", artist));
            args.Add(new ApiArg("track", track));
            // optional
            if (album != null) args.Add(new ApiArg("album", album));
            if (trackNumber != null) args.Add(new ApiArg("trackNumber", trackNumber));
            if (mbid != null) args.Add(new ApiArg("mbid", mbid));
            if (duration != null) args.Add(new ApiArg("duration", duration.Value.TotalSeconds.ToString(NumberFormatInfo.InvariantInfo)));
            if (albumArtist != null) args.Add(new ApiArg("albumArtist", albumArtist));

            return Common.PostAsync("track.updateNowPlaying", sessionKey, x => x.Value, args.ToArray());
        }

        //public static string GetAuthorizeTokenUrl(string token)
        //    => $"http://www.last.fm/api/auth?api_key={Common.ApiKey}&token={token}";

        //public static Task<ApiResult<Session>> GetSession(string authorizedToken)
        //{
        //    var requestString = Common.CreateRequestString("auth.getSession", addSignature: true, args: new ApiArg("token", authorizedToken));
        //    return Common.GetAsync(requestString, x => new Session(x));
        //}
    }

    //public class Session
    //{
    //    public string UserName { get; }
    //    public string Key { get; }
    //    public int Subscriber { get; }

    //    public Session(XElement xml)
    //    {
    //        var sessionElement = xml.Element("session");
    //        UserName = sessionElement.Element("name").Value;
    //        Key = sessionElement.Element("key").Value;
    //        Subscriber = int.Parse(sessionElement.Element("subscriber").Value);
    //    }
    //}
    //<? xml version='1.0' encoding='utf-8'?>
    //<lfm status = "ok" >
    //  < nowplaying >
    //    < track corrected="0">Test Track</track>
    //     <artist corrected = "0" > Test Artist</artist>
    //     <album corrected = "0" ></ album >
    //     < albumArtist corrected= "0" ></ albumArtist >
    //     < ignoredMessage code= "0" ></ ignoredMessage >
    //   </ nowplaying >
    // </ lfm >
}
