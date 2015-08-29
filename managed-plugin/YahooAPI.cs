using System.Net; // WebClient
using System.Globalization; // CultureInfo

public class YahooAPI
{
    private static readonly WebClient webClient = new WebClient();

    private const string UrlTemplate = "http://finance.yahoo.com/d/quotes.csv?s={0}&f={1}";

    private static double ParseDouble(string value)
    {
        try
        {
            return double.Parse(value.Trim(), CultureInfo.InvariantCulture);
        }
        catch (System.Exception)
        {
            return -1;
        }
    }

    private static string[] GetDataFromYahoo(string symbol, string fields)
    {
        string request = string.Format(UrlTemplate, symbol, fields);

        string rawData = webClient.DownloadString(request).Trim();

        return rawData.Split(',');
    }

    public double GetBid(string symbol)
    {
        return ParseDouble(GetDataFromYahoo(symbol, "b3")[0]);
    }

    public double GetAsk(string symbol)
    {
        return ParseDouble(GetDataFromYahoo(symbol, "b2")[0]);
    }

    public string GetCapitalization(string symbol)
    {
        int fortyTwo = new Scrobbling.Auth().Get42();
        return symbol + "coucou" + fortyTwo.ToString() + "éµ"; //GetDataFromYahoo(symbol, "j1")[0];
    }

    public string[] GetValues(string symbol, string fields)
    {
        return GetDataFromYahoo(symbol, fields);
    }
}