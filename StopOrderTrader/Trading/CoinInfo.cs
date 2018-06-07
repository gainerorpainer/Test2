using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StopOrderTrader.Trading
{
    class CoinInfo
    {
        public string Symbol { get; set; }
        public decimal? Volume24 { get; set; }
        public decimal? MarketCap { get; set; }
    }
}
