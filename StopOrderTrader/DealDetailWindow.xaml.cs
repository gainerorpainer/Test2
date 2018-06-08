using Binance.Net.Objects;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using StopOrderTrader.Trading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public decimal? OtherSellPrice { get; set; }
        public decimal? GainPercent { get; set; }
        public decimal? GainBTC { get; set; }
        public ICommand CancelAndSell { get; set; }
        public ICommand UpdateChart { get; set; }

        public SeriesCollection SeriesCollection { get; set; } = new SeriesCollection();

        public ObservableCollection<string> Labels { get; set; } = new ObservableCollection<string>();



        public bool ShowUntilToday { get => _showUntilToday; set { _showUntilToday = value; UpdateChart.Execute(null); } }

        private bool _showUntilToday = true;
    }

    /// <summary>
    /// Interaktionslogik für DealDetailWindow.xaml
    /// </summary>
    public partial class DealDetailWindow : Window
    {
        class TradeSummary
        {
            public decimal? Price;
            public decimal? Quantity;
        }

        readonly List<(double, KlineInterval)> _optimalIntervals = new List<(double, KlineInterval)>()
            {
                (4/5, KlineInterval.OneMinute),
                (4*3/5, KlineInterval.ThreeMinutes),
                (4, KlineInterval.FiveMinutes),
                (12, KlineInterval.FiveteenMinutes),
                (24, KlineInterval.ThirtyMinutes),
                (2*24, KlineInterval.OneHour),
                (4*24, KlineInterval.TwoHour),
                (8*24, KlineInterval.FourHour),
                (12*24, KlineInterval.SixHour),
                (16*24, KlineInterval.EightHour),
                (24*24, KlineInterval.TwelfHour),
                (48*24, KlineInterval.OneDay),
                (3*48*24, KlineInterval.ThreeDay),
                (7*48*24, KlineInterval.OneWeek),
                (31*48*24, KlineInterval.OneMonth)
            };

        DealDetailWindowModel Model;
        Deal _deal;

        public DealDetailWindow(Deal deal)
        {
            _deal = deal;

            InitializeComponent();
        }

        public new void ShowDialog()
        {
            Dispatcher.RefreshThenContinue(() => this.CallWithWaitWindow(Load));
            base.ShowDialog();
        }

        private void Load()
        {
            decimal currentPrice = TradeInterface.Client.GetPrice(_deal.Symbol).GetOrThrow().Price;

            var buyOrders = _deal.BuyOrder == null ? null : TradeInterface.GetActualOrders(_deal.Symbol, _deal.BuyOrder.ClientOrderId);
            var sellOrders1 = _deal.Goal1SellOrder == null ? null : TradeInterface.GetActualOrders(_deal.Symbol, _deal.Goal1SellOrder.ClientOrderId);
            var sellOrders2 = _deal.Goal2SellOrder == null ? null : TradeInterface.GetActualOrders(_deal.Symbol, _deal.Goal2SellOrder.ClientOrderId);
            var sellOtherOrders = _deal.OtherSellOrder == null ? null : TradeInterface.GetActualOrders(_deal.Symbol, _deal.OtherSellOrder.ClientOrderId);

            TradeSummary GetSummary(List<BinanceTrade> list) => new TradeSummary()
            {
                Price = list?.WeightedAverage(x => x.Price, x => x.Quantity, true),
                Quantity = list?.Sum(x => x.Quantity)
            };

            TradeSummary buy = GetSummary(buyOrders);
            TradeSummary sell1 = GetSummary(sellOrders1);
            TradeSummary sell2 = GetSummary(sellOrders2);
            TradeSummary sellOther = GetSummary(sellOtherOrders);

            Model = new DealDetailWindowModel
            {
                BuyAmountBTC = (buy.Quantity * buy.Price)?.Normalize(),
                BuyPrice = buy.Price?.Normalize(),
                SellPrice1 = sell1.Price?.Normalize(),
                SellPrice2 = sell2.Price?.Normalize(),
                OtherSellPrice = sellOther.Price?.Normalize()
            };

            // Gain calc
            if (Model.BuyPrice != null)
            {
                decimal initialAltCoins = Model.BuyAmountBTC.Value / Model.BuyPrice.Value;

                decimal altCoins = initialAltCoins;
                decimal sellAmountBTC = 0;

                void CalcSell(TradeSummary action)
                {
                    sellAmountBTC += (action.Quantity * action.Price) ?? 0;
                    altCoins -= action.Quantity ?? 0;
                }

                CalcSell(sell1);
                CalcSell(sell2);
                CalcSell(sellOther);

                Model.GainBTC = (sellAmountBTC - Model.BuyAmountBTC.Value + altCoins * currentPrice).Normalize();
                Model.GainPercent = Model.GainBTC / Model.BuyAmountBTC;
            }

            Model.CancelAndSell = new Lib.ActionCommand(CancelAndSellDeal);
            Model.UpdateChart = new Lib.ActionCommand(UpdateChart);

            CancelAndSell_Button.IsEnabled = _deal.CurrentState != Deal.State.Done;

            DataContext = Model;

            LoadChart(buyOrders, sellOrders1, sellOrders2, sellOtherOrders);
        }

        void UpdateChart()
        {
            this.CallWithWaitWindow(() =>
            {
                var buyOrders = _deal.BuyOrder == null ? null : TradeInterface.GetActualOrders(_deal.Symbol, _deal.BuyOrder.ClientOrderId);
                var sellOrders1 = _deal.Goal1SellOrder == null ? null : TradeInterface.GetActualOrders(_deal.Symbol, _deal.Goal1SellOrder.ClientOrderId);
                var sellOrders2 = _deal.Goal2SellOrder == null ? null : TradeInterface.GetActualOrders(_deal.Symbol, _deal.Goal2SellOrder.ClientOrderId);
                var sellOtherOrders = _deal.OtherSellOrder == null ? null : TradeInterface.GetActualOrders(_deal.Symbol, _deal.OtherSellOrder.ClientOrderId);

                Model.SeriesCollection.Clear();
                LoadChart(buyOrders, sellOrders1, sellOrders2, sellOtherOrders);
            });

        }

        void LoadChart(List<BinanceTrade> buyOrders, List<BinanceTrade> sellOrders1, List<BinanceTrade> sellOrders2, List<BinanceTrade> sellOtherOrders)
        {
            // Get deal details
            BinanceOrder GetOrder(ClientServerOrder order) => order == null ? null : TradeInterface.GetOrderById(_deal.Symbol, order.ClientOrderId);
            var buy = GetOrder(_deal.BuyOrder);
            var sell1 = GetOrder(_deal.Goal1SellOrder);
            var sell2 = GetOrder(_deal.Goal2SellOrder);
            var sellOther = GetOrder(_deal.OtherSellOrder);

            // Calculate "zoom"
            // Get most apart
            long Min(long? a, long? b, long? c, long? d) => Math.Min(a ?? long.MaxValue, Math.Min(b ?? long.MaxValue, Math.Min(c ?? long.MaxValue, d ?? long.MaxValue)));
            long Max(long? a, long? b, long? c, long? d) => Math.Max(a ?? long.MinValue, Math.Max(b ?? long.MinValue, Math.Max(c ?? long.MinValue, d ?? long.MinValue)));
            BinanceTrade lastBuyTrade = buyOrders?.LastOrDefault();
            BinanceTrade lastSell1Trade = sellOrders1?.LastOrDefault();
            BinanceTrade lastSell2Trade = sellOrders2?.LastOrDefault();
            BinanceTrade lastSellOtherTrade = sellOtherOrders?.LastOrDefault();
            DateTime start = DateTime.FromBinary(Min(lastBuyTrade?.Time.ToBinary(), lastSell1Trade?.Time.ToBinary(), lastSell2Trade?.Time.ToBinary(), lastSellOtherTrade?.Time.ToBinary()));
            DateTime stop = Model.ShowUntilToday ? DateTime.UtcNow :
                _deal.CurrentState == Deal.State.Done ?
                DateTime.FromBinary(Max(lastBuyTrade?.Time.ToBinary(), lastSell1Trade?.Time.ToBinary(), lastSell2Trade?.Time.ToBinary(), lastSellOtherTrade?.Time.ToBinary()))
                : DateTime.UtcNow;
            var totalTime = stop - start;

            // Try to get matching zoom
            KlineInterval interval = KlineInterval.OneMonth;

            foreach (var item in _optimalIntervals)
            {
                if (totalTime.TotalHours < item.Item1)
                {
                    interval = item.Item2;
                    break;
                }
            }

            // Download candles
            var klines = TradeInterface.Client.GetKlines(_deal.Symbol, interval, limit: 48, endTime: stop).GetOrThrow().ToList();

            // convert to simple x,y
            Model.SeriesCollection.Add(new OhlcSeries()
            {
                Title = "Course",
                Values = new ChartValues<OhlcPoint>(klines.Select(x => new OhlcPoint((double)x.Open, (double)x.High, (double)x.Low, (double)x.Close)))
            });
            Model.Labels.SyncWith(klines.Select(x => x.CloseTime.ToString("dd.MM hh:mm")));


            // Handle limits
            void AddLimit(string label, decimal price)
            {
                LineSeries lineSeries = new LineSeries()
                {
                    Title = label,
                    Values = new ChartValues<ObservablePoint>()
                    {
                        new ObservablePoint(0, (double)price),
                        new ObservablePoint(47, (double)price),
                    },
                    Fill = Brushes.Transparent,
                    PointGeometry = null,
                    StrokeDashArray = new DoubleCollection() { 2, 2 },
                };

                Panel.SetZIndex(lineSeries, 99999);

                Model.SeriesCollection.Add(lineSeries);
            }

            if (buy != null && buy.Price != decimal.Zero)
                AddLimit("Buy", buy.Price);
            if (sell1 != null)
                AddLimit("Sell1", sell1.Price);
            if (sell2 != null)
                AddLimit("Sell2", sell2.Price);
            if (sellOther != null && sellOther.Price != decimal.Zero)
                AddLimit("SellOther", sellOther.Price);


            int transformX(double time) => (int)(1.0 + (48 - 1) * ((time - klines.First().CloseTime.ToBinary()) / (klines.Last().CloseTime.ToBinary() - klines.First().CloseTime.ToBinary())));

            void AddEvent(string label, DateTime when, decimal price, Geometry pointGeometry)
            {
                ScatterSeries eventSeries = new ScatterSeries()
                {
                    Title = label,
                    PointGeometry = pointGeometry,
                    Values = new ChartValues<ObservablePoint>(),
                    StrokeThickness = 3
                };

                Panel.SetZIndex(eventSeries, 999999);

                int x = transformX(when.ToBinary());
                eventSeries.Values.Add(new ObservablePoint(x, (double)price));

                Model.SeriesCollection.Add(eventSeries);
            }

            // Handle events
            var scaledGeometry = DefaultGeometries.Diamond.Clone();
            scaledGeometry.Transform = new ScaleTransform(3, 3);
            scaledGeometry.Freeze();

            if (lastBuyTrade != null)
                AddEvent("Buy", buyOrders.Last().Time, buyOrders.EffectivePrice(), scaledGeometry);
            if (lastSell1Trade != null)
                AddEvent("Sell1", sellOrders1.Last().Time, sellOrders1.EffectivePrice(), scaledGeometry);
            if (lastSell2Trade != null)
                AddEvent("Sell2", sellOrders2.Last().Time, sellOrders2.EffectivePrice(), scaledGeometry);
            if (lastSellOtherTrade != null)
                AddEvent("SellOther", sellOtherOrders.Last().Time, sellOtherOrders.EffectivePrice(), scaledGeometry);
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        void CancelAndSellDeal()
        {
            if (Toolbox.ConfirmPopup("Really cancel?", "Do you really want to cancel this order? A 100% market price order will be checked out. This will result in the calculated gain or worse!"))
            {
                _deal.CancelAndSell();
                DialogResult = true;
            }
        }

        private void Label_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://coinmarketcap.com/currencies/" + CoinMarketCap.SymbolToWebsiteslug[_deal.Symbol.RemoveLast("BTC".Length)]);
        }
    }
}
