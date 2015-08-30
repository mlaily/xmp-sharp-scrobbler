using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SharpScrobbler
{
    public string SessionKey { get; }

    public SharpScrobbler(string sessionKey)
    {
        SessionKey = sessionKey;
    }
    public static void Initialize()
    {

    }

    public static string AskUserForNewAuthorizedSessionKey()
    {
        throw new NotImplementedException();
    }
}
