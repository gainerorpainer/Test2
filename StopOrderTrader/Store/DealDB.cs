using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace StopOrderTrader.Store
{
    [Serializable]
    public class DealDb : Lib.EasyFileSerializer
    {
        static readonly string DealDbPath = AppContext.BaseDirectory + "Store\\dealdb.xml";

        public static DealDb MainInstance { get; private set; }

        public static void Load()
        {
            // This will automatically load the db the first time it is used
            if (System.IO.File.Exists(DealDbPath) == false)
                MainInstance = new DealDb(DealDbPath);
            else
                MainInstance = Deserialize<DealDb>(DealDbPath);

            // Set deal counter to a reasonable value if you have any
            int? highestId = MainInstance.Deals.LastOrDefault()?.Id;
            if (highestId >= Properties.Settings.Default.NextDealId)
                Properties.Settings.Default.NextDealId = (highestId ?? -1) + 1;
            //TransformFromOld();
        }

        internal static int GetNewId()
        {
            return Properties.Settings.Default.NextDealId++;
        }

        public static void Save() => MainInstance.Serialize();


        public DealDb() : this(DealDbPath) { }
        public DealDb(string filepath) : base(filepath) { }

        public System.Collections.ObjectModel.ObservableCollection<Trading.Deal> Deals { get; set; } = new System.Collections.ObjectModel.ObservableCollection<Trading.Deal>();

        private static void TransformFromOld()
        {
            Trading.TradeInterface.Load();

            foreach (var item in MainInstance.Deals)
            {
                if (item.BuyOrder != null)
                    item.BuyPrice = Trading.TradeInterface.GetActualOrders(item.Symbol, item.BuyOrder.ClientOrderId).EffectivePrice();
                item.Sell1Perc = 0.01m;
                item.Sell2Perc = 0.02m;
                item.SellStopLossPerc = 0;
            }


            return;

            int i = 0;
            foreach (var item in MainInstance.Deals)
            {
                item.Id = i++;
                //if (item.BuyOrder != null)
                //{
                //    var placedOrder = Trading.TradeInterface.GetOrderById(item.Symbol, item.BuyOrder.ClientOrderId);
                //    item.BuyOrder.FilledOrders = Trading.TradeInterface.GetActualOrders(item.Symbol, placedOrder.OrderId).Select(x => x.OrderId).ToList();
                //}

                //if (item.Goal1SellOrder != null)
                //{
                //    var placedOrder = Trading.TradeInterface.GetOrderById(item.Symbol, item.Goal1SellOrder.ClientOrderId);
                //    item.Goal1SellOrder.FilledOrders = Trading.TradeInterface.GetActualOrders(item.Symbol, placedOrder.OrderId).Select(x => x.OrderId).ToList();
                //}

                //if (item.Goal2SellOrder != null)
                //{
                //    var placedOrder = Trading.TradeInterface.GetOrderById(item.Symbol, item.Goal2SellOrder.ClientOrderId);
                //    item.Goal2SellOrder.FilledOrders = Trading.TradeInterface.GetActualOrders(item.Symbol, placedOrder.OrderId).Select(x => x.OrderId).ToList();
                //}

                //if (item.OtherSellOrder != null)
                //{
                //    var placedOrder = Trading.TradeInterface.GetOrderById(item.Symbol, item.OtherSellOrder.ClientOrderId);
                //    item.OtherSellOrder.FilledOrders = Trading.TradeInterface.GetActualOrders(item.Symbol, placedOrder.OrderId).Select(x => x.OrderId).ToList();
                //}
            }

            Save();
        }

    }
}
