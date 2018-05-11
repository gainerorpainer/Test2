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

namespace StopOrderTrader
{

    public class DealDetailWindowModel : Lib.NotifyModel
    {
        public decimal? BuyAmountBTC { get; set; }
        public decimal? BuyPrice { get; set; }
        public decimal? SellPrice1 { get; set; }
        public decimal? SellPrice2 { get; set; }
        public decimal? PanicSellPrice { get; set; }
        public decimal? GainPercent { get; set; }
        public decimal? GainBTC { get; set; }
    }

    /// <summary>
    /// Interaktionslogik für DealDetailWindow.xaml
    /// </summary>
    public partial class DealDetailWindow : Window
    {
        DealDetailWindowModel Model;
        Deal _deal;

        public DealDetailWindow(Deal deal)
        {
            _deal = deal;

            InitializeComponent();
        }

        public new void ShowDialog()
        {
            Dispatcher.RefreshThenContinue(() => this.CallWithWaitWindow(LoadAsync));
            base.ShowDialog();
        }

        private void LoadAsync()
        {
            decimal currentPrice = TradeInterface.Client.GetPrice(_deal.Symbol).GetOrThrow().Price;

            Binance.Net.Objects.BinanceTrade GetActualOrder(ClientServerOrder clientServerOrder)
            {
                if (clientServerOrder is null)
                    return null;

                var order = TradeInterface.GetOrderById(_deal.Symbol, clientServerOrder.ClientOrderId);
                if (order.Status == Binance.Net.Objects.OrderStatus.Filled)
                    return TradeInterface.Client.GetMyTrades(_deal.Symbol).GetOrThrow().First(x => x.OrderId == order.OrderId);
                else return null;
            }

            var buyOrder = GetActualOrder(_deal.BuyOrder);
            var sellorder1 = GetActualOrder(_deal.SellOrder1);
            var sellorder2 = GetActualOrder(_deal.SellOrder2);
            Model = new DealDetailWindowModel
            {
                BuyAmountBTC = buyOrder?.Quantity * buyOrder?.Price,
                BuyPrice = buyOrder?.Price,
                SellPrice1 = sellorder1?.Price,
                SellPrice2 = sellorder2?.Price,
                PanicSellPrice = GetActualOrder(_deal.PanicSellOrder)?.Price
            };

            // Gain calc
            if (Model.BuyPrice != null)
            {
                decimal initialAltCoins = Model.BuyAmountBTC.Value / Model.BuyPrice.Value;

                decimal altCoins = initialAltCoins;
                decimal sellAmountBTC = 0;

                if (sellorder1 != null)
                {
                    sellAmountBTC += sellorder1.Quantity * sellorder1.Price;
                    altCoins -= sellorder1.Quantity;
                }

                if (sellorder2 != null)
                {
                    sellAmountBTC += sellorder2.Quantity * sellorder2.Price;
                    altCoins -= sellorder2.Quantity;
                }

                Model.GainBTC = sellAmountBTC - Model.BuyAmountBTC.Value + altCoins * currentPrice;
                Model.GainPercent = Model.GainBTC / Model.BuyAmountBTC;
            }

            DataContext = Model;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
