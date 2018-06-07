using Binance.Net.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StopOrderTrader.Store
{
    [Serializable]
    public class AccountInfo 
    {
        public DateTime TickerTime { get; set; }
        public BinanceAccountInfo BinanceAccountInfo { get; set; }
    }

    [Serializable]
    public class AccountDb : Lib.EasyFileSerializer
    {
        static readonly string AccountDbPath = AppContext.BaseDirectory + $"Store\\accountdb_{DateTime.Today.ToString("yyyyMM")}.zip";

        public static AccountDb Instance { get; private set; }

        public static void Load()
        {
            // This will automatically load the db the first time it is used
            if (System.IO.File.Exists(AccountDbPath) == false)
                Instance = new AccountDb();
            else
                Instance = Deserialize<AccountDb>(AccountDbPath, true);
        }

        public static void Save() => Instance.Serialize();

        public List<AccountInfo> Tickers { get; set; } = new List<AccountInfo>();

        public AccountDb() : base(AccountDbPath, true) { }
    }
}
