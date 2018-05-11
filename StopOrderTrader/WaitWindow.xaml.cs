using StopOrderTrader.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace StopOrderTrader
{
    /// <summary>
    /// Interaktionslogik für WaitWindow.xaml
    /// </summary>
    public partial class WaitWindow : Window
    {
        public WaitWindow()
        {
            InitializeComponent();

            //// Transform xml to new format
            //List<Deal2> newModel = new List<Deal2>();
            //foreach (var olddeal in Store.DealDB.Instance.Deals)
            //{
            //    long GetOrderId(string symbol, string id)
            //    {
            //        var clientorder = TradeInterface.GetOrderById(symbol, id);
            //        return TradeInterface.Client.GetMyTrades(symbol).GetOrThrow().First(x => x.OrderId == clientorder.OrderId).OrderId;
            //    }


            //    Deal2 newdeal = new Deal2()
            //    {
            //        CreationTime = olddeal.CreationTime,
            //        CurrentResult = (Deal2.Result)olddeal.CurrentResult,
            //        CurrentState = (Deal2.State)olddeal.CurrentState,
            //        Symbol = olddeal.Symbol,
            //        LastChangedTime = olddeal.LastChangedTime,
            //        BuyOrder =  olddeal.BuyOrder == null ? null : new ClientServerOrder()
            //        {
            //            ClientOrderId = olddeal.BuyOrder,
            //            OrderId = GetOrderId(olddeal.Symbol, olddeal.BuyOrder)
            //        },
            //        SellOrder1 = olddeal.SellOrder1 == null ? null : new ClientServerOrder()
            //        {
            //            ClientOrderId = olddeal.SellOrder1,
            //            OrderId = GetOrderId(olddeal.Symbol, olddeal.SellOrder1)
            //        },
            //        SellOrder2 = olddeal.SellOrder2 == null ? null : new ClientServerOrder()
            //        {
            //            ClientOrderId = olddeal.SellOrder2,
            //            OrderId = GetOrderId(olddeal.Symbol, olddeal.SellOrder2)
            //        },
            //    };
            //    newModel.Add(newdeal);

            //    if (olddeal.BuyOrder != null)
            //        newdeal.CurrentState = Deal2.State.WaitForLeftovers;

            //}

            //using (var fs = System.IO.File.Create("Store\\newdb.xml"))
            //{
            //    new System.Xml.Serialization.XmlSerializer(newModel.GetType()).Serialize(fs, newModel);
            //}
        }
    }
}
