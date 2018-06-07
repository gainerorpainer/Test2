using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StopOrderTrader.Trading
{
    static class CoinMarketCap
    {
        class AllListingClass
        {
            public class CoinClass
            {
                public int id { get; set; }
                public string name { get; set; }
                public string symbol { get; set; }
                public string website_slug { get; set; }
            }

            public CoinClass[] data { get; set; }
        }


        const string LISTINGURL = "https://api.coinmarketcap.com/v2/listings/";

        public static Dictionary<string, string> SymbolToWebsiteslug { get; }

        static CoinMarketCap()
        {
            SymbolToWebsiteslug = JsonGetAndParse<AllListingClass>(LISTINGURL)
                .data.GroupBy(x => x.symbol)
                .Select(x => new { Symbol = x.Key, Websiteslug = x.OrderBy(y => y.id).First().website_slug })
                .ToDictionary(x => x.Symbol, x => x.Websiteslug);
        }

        static T JsonGetAndParse<T>(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Accept = "application/json";

            using (WebResponse response = GetResponseNoException(request))
            using (System.IO.StreamReader sr = new System.IO.StreamReader(response.GetResponseStream()))
            {
                // Parse Json
                return JsonConvert.DeserializeObject<T>(sr.ReadToEnd());
            }
        }

        static HttpWebResponse GetResponseNoException(HttpWebRequest req)
        {
            try
            {
                return (HttpWebResponse)req.GetResponse();
            }
            catch (WebException we)
            {
                if (we.Response is HttpWebResponse resp)
                    return resp;
                else
                    throw we;
            }
        }
    }
}
