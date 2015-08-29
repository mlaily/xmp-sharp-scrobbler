using Scrobbling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var tokenResult = Auth.GetToken().Result;
            var url = Auth.GetAuthorizeTokenUrl(tokenResult.Result);
            Process.Start(url);
            Console.WriteLine("Press enter once the token is authorized in the browser.");
            Console.ReadLine();
            var sessionResult = Auth.GetSession(tokenResult.Result).Result;



            var scrobble = new Scrobble("Russ Chimes", "Targa (Original Mix)", DateTimeOffset.Now.AddMinutes(-4))
            {
                Album = "Midnight Club EP",
                TrackNumber = "1/3",
                Duration = new TimeSpan(0, 5, 16)
            };

            var updateNowPlayingResult = Track.UpdateNowPlaying(sessionKey, scrobble).Result;

            var scrobbleResult = Track.Scrobble(sessionKey, scrobble).Result;



            Console.Read();
        }
    }
}
