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
            //var tokenResult = Auth.GetToken().Result;
            //var url = Auth.GetAuthorizeTokenUrl(tokenResult.Result);
            //Process.Start(url);
            //Console.WriteLine("Press enter once the token is authorized in the browser.");
            //Console.ReadLine();
            //var sessionResult = Auth.GetSession(tokenResult.Result).Result;


            var updateNowPlayingResult = 
                Track.UpdateNowPlaying(sessionKey, artist: "Russ Chimes", track: "Targa (Original Mix)", album: "Midnight Club EP", trackNumber: "1/3", duration: new TimeSpan(0, 5, 16)).Result;

            Console.Read();
        }
    }
}
