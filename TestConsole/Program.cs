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

            Console.Read();
        }
    }
}
