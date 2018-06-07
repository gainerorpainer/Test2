using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StopOrderTrader.Trading
{
    static class CryptoCompare
    {

        public partial class InfoClass
        {
            [JsonProperty("RAW")]
            public Dictionary<string, Raw> Raw { get; set; }

            [JsonProperty("DISPLAY")]
            public Dictionary<string, Display> Display { get; set; }
        }

        public partial class Display
        {
            [JsonProperty("USD")]
            public Dictionary<string, string> Usd { get; set; }
        }

        public partial class Raw
        {
            [JsonProperty("USD")]
            public Usd Usd { get; set; }
        }

        public partial class Usd
        {
            [JsonProperty("TYPE")]
            public string Type { get; set; }

            [JsonProperty("MARKET")]
            public string Market { get; set; }

            [JsonProperty("FROMSYMBOL")]
            public string Fromsymbol { get; set; }

            [JsonProperty("TOSYMBOL")]
            public string Tosymbol { get; set; }

            [JsonProperty("FLAGS")]
            public string Flags { get; set; }

            [JsonProperty("PRICE")]
            public decimal Price { get; set; }

            [JsonProperty("LASTUPDATE")]
            public long Lastupdate { get; set; }

            [JsonProperty("LASTVOLUME")]
            public decimal Lastvolume { get; set; }

            [JsonProperty("LASTVOLUMETO")]
            public decimal Lastvolumeto { get; set; }

            [JsonProperty("LASTTRADEID")]
            public string Lasttradeid { get; set; }

            [JsonProperty("VOLUMEDAY")]
            public decimal Volumeday { get; set; }

            [JsonProperty("VOLUMEDAYTO")]
            public decimal Volumedayto { get; set; }

            [JsonProperty("VOLUME24HOUR")]
            public decimal Volume24Hour { get; set; }

            [JsonProperty("VOLUME24HOURTO")]
            public decimal Volume24Hourto { get; set; }

            [JsonProperty("OPENDAY")]
            public decimal Openday { get; set; }

            [JsonProperty("HIGHDAY")]
            public decimal Highday { get; set; }

            [JsonProperty("LOWDAY")]
            public decimal Lowday { get; set; }

            [JsonProperty("OPEN24HOUR")]
            public decimal Open24Hour { get; set; }

            [JsonProperty("HIGH24HOUR")]
            public decimal High24Hour { get; set; }

            [JsonProperty("LOW24HOUR")]
            public decimal Low24Hour { get; set; }

            [JsonProperty("LASTMARKET")]
            public string Lastmarket { get; set; }

            [JsonProperty("CHANGE24HOUR")]
            public decimal Change24Hour { get; set; }

            [JsonProperty("CHANGEPCT24HOUR")]
            public decimal Changepct24Hour { get; set; }

            [JsonProperty("CHANGEDAY")]
            public decimal Changeday { get; set; }

            [JsonProperty("CHANGEPCTDAY")]
            public decimal Changepctday { get; set; }

            [JsonProperty("SUPPLY")]
            public decimal Supply { get; set; }

            [JsonProperty("MKTCAP")]
            public decimal Mktcap { get; set; }

            [JsonProperty("TOTALVOLUME24H")]
            public decimal Totalvolume24H { get; set; }

            [JsonProperty("TOTALVOLUME24HTO")]
            public decimal Totalvolume24Hto { get; set; }
        }



        public static Dictionary<string, CoinInfo> GetCoinInfo(IEnumerable<string> data)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://min-api.cryptocompare.com/data/pricemultifull?tsyms=USD&fsyms=" + string.Join(",", data));
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Accept = "application/json";

            using (WebResponse response = GetResponseNoException(request))
            using (System.IO.StreamReader sr = new System.IO.StreamReader(response.GetResponseStream()))
            {
                string txt = sr.ReadToEnd();

                // Parse Json
                var mappedResult = JsonConvert.DeserializeObject<InfoClass>(txt);

                return mappedResult.Raw.ToDictionary(x => x.Key, x => new CoinInfo()
                {
                    Symbol = x.Key,
                    MarketCap = x.Value.Usd.Mktcap,
                    Volume24 = x.Value.Usd.Totalvolume24H
                });
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
