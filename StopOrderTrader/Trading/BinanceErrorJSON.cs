using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StopOrderTrader.Trading
{
    public partial class BinanceErrorJSON
    {
        public static BinanceErrorJSON FromJson(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<BinanceErrorJSON>(json);

            }
            catch (JsonReaderException ex)
            {
                return new BinanceErrorJSON() { Msg = json, Code = 0 };
            }
        }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }
    }
}
