using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace StopOrderTrader.Store
{
    [Serializable]
    public class DealDB : Lib.EasyFileSerializer
    {
        static readonly string DealDBPath = AppContext.BaseDirectory + "Store\\dealdb.xml";

        public static DealDB Instance { get; private set; }

        public static void Load()
        {
            // This will automatically load the db the first time it is used
            if (System.IO.File.Exists(DealDBPath) == false)
                Instance = new DealDB() { Deals = new List<Trading.Deal>() };
            else
                Instance = Toolbox.DeserializeXml<DealDB>(DealDBPath);
        }

        public static void Save() => Instance.Serialize();

        DealDB() : base(DealDBPath) { }

        public List<Trading.Deal> Deals { get; set; }
    }
}
