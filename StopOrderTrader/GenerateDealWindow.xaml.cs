using StopOrderTrader.Trading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using static StopOrderTrader.Toolbox;

namespace StopOrderTrader
{
    public class CurrencyInfo
    {
        public string Currency { get; set; }
        public decimal Trend24Perc { get; set; }
        public decimal Volume { get; set; }
        public double Volatility24h { get; set; }
        public double Tension4d { get; set; }
    }

    public class GenerateDealWindowModel : Lib.NotifyModel
    {
        decimal _freeBTC;
        public decimal FreeBTC { get => _freeBTC; set { _freeBTC = value; OnPropertyChanged(nameof(FreeBTC)); } }
        public decimal FreeBTC_To_USDT => _freeBTC * BTC_To_USDT;

        public int PossibleDeals => (int)(FreeBTC / BTCPerDeal);

        bool _searchDone = true;
        public bool SearchDone { get => _searchDone; set { _searchDone = value; OnPropertyChanged(nameof(SearchDone)); OnPropertyChanged(nameof(SearchButtonLabel)); } }

        public string SearchButtonLabel => _searchDone ? "Calculate metrics" : "Stop";

        public ObservableCollection<CurrencyInfo> Currencies { get; set; } = new ObservableCollection<CurrencyInfo>();

        int _progress;
        public int Progress { get => _progress; set { _progress = value; OnPropertyChanged(nameof(Progress)); OnPropertyChanged(nameof(Progress)); } }

        decimal _btcPerDeal = 0.0031m;
        public decimal BTCPerDeal { get => _btcPerDeal; set { _btcPerDeal = value; OnPropertyChanged(nameof(BTCPerDeal)); OnPropertyChanged(nameof(PossibleDeals)); } }

        public ICommand Search { get; set; }
        public ICommand MakeDeals { get; set; }

        public decimal BTC_To_USDT = 1;
    }

    /// <summary>
    /// Interaktionslogik für GenerateDealWindow.xaml
    /// </summary>
    public partial class GenerateDealWindow : Window
    {
        private const int MINIMUMVOLUME = 10000;
        GenerateDealWindowModel Model = new GenerateDealWindowModel();
        List<CurrencyInfo> _searchCoins;
        int _searchCoinPtr;
        CancellationTokenSource _cancel;
        Task _task;

        public GenerateDealWindow()
        {
            InitializeComponent();
            this.CallWithWaitWindow(Setup);
        }

        public void MakeDeals()
        {
            // Get all selected deals
            if (Coins_DataGrid.SelectedItems.Count > Model.PossibleDeals)
            {
                Error("Too many deals", $"You selected {Coins_DataGrid.SelectedItems.Count}, but you only have funds for {Model.PossibleDeals}");
                return;
            }

            foreach (var item in Coins_DataGrid.SelectedItems)
            {
                // Buy enough coins to get 0.003 btc worth in order to be able to trade
                string symbol = (Coins_DataGrid.Columns[0].GetCellContent(item) as TextBlock).Text + "BTC";
                decimal btcPerAlt = TradeInterface.Client.GetPrice(symbol).GetOrThrow().Price;
                var orderPair =
                    TradeInterface.PlaceImmediateOrder(symbol, quantity: Model.BTCPerDeal / btcPerAlt, orderSide: Binance.Net.Objects.OrderSide.Buy);

                // make a db entry
                Store.DealDB.Instance.Deals.Add(new Deal()
                {
                    BuyOrder = orderPair,
                    CreationTime = DateTime.Now,
                    CurrentResult = Deal.Result.NotDoneYet,
                    CurrentState = Deal.State.WaitForBuy,
                    Symbol = symbol
                });
            }

            DialogResult = true;
        }

        private void Setup()
        {
            Model.Search = new Lib.ActionCommand(() => this.CallWithWaitWindow(Search));
            Model.MakeDeals = new Lib.ActionCommand(MakeDeals);

            var accountInfo = TradeInterface.Client.GetAccountInfo().GetOrThrow();
            Model.BTC_To_USDT = TradeInterface.Client.GetPrice("BTCUSDT").GetOrThrow().Price;
            Model.FreeBTC = accountInfo.Balances.First(x => x.Asset == "BTC").Free;

            var tradableCoins = TradeInterface.Client.Get24HPricesList().GetOrThrow()
                    .Where(x => x.Symbol.EndsWith("BTC"))
                    .Select(x => new CurrencyInfo()
                    {
                        Currency = x.Symbol.RemoveLast(3),
                        Trend24Perc = x.PriceChangePercent,
                        Volume = x.Volume
                    });

            // Prefilter
            _searchCoins = tradableCoins.Where(x => x.Volume > MINIMUMVOLUME).ToList();

            Model.Currencies.SyncWith(_searchCoins);

            DataContext = Model;
        }

        void Search()
        {
            if (Model.SearchDone == true)
            {
                Model.SearchDone = false;
                _cancel = new CancellationTokenSource();

                // Now, deep search each of them in background
                _task = AsyncSearch(_cancel.Token).ContinueWith((x) => Dispatcher.Invoke(() =>
                {
                    Model.SearchDone = true;
                }));

                Coins_DataGrid.SortBy(nameof(CurrencyInfo.Volatility24h), System.ComponentModel.ListSortDirection.Descending);
            }
            else
            {
                _cancel.Cancel();


                Model.SearchDone = true;
            }
        }

        async Task AsyncSearch(CancellationToken cancel)
        {
            void Search()
            {
                for (; _searchCoinPtr < _searchCoins.Count; _searchCoinPtr++)
                {
                    var coin = _searchCoins[_searchCoinPtr];

                    // Get course for the last 7 days
                    List<Binance.Net.Objects.BinanceKline> course = TradeInterface.Client.GetKlines(coin.Currency + "BTC", Binance.Net.Objects.KlineInterval.OneHour, limit: 24 * 7).GetOrThrow().ToList();
                    double lastClose = (double)course.Last().Close;

                    // Volatility is the r^2 fitting factor against a linear regression of the last 24 h closes
                    coin.Volatility24h = 1 - Lib.Algorithm.GetLinearRegression(course.GetLast(24).Select(x => new Point(x.CloseTime.Ticks, (double)x.Close)).ToList()).R;

                    // Tension is the deviation against a moving average of 99
                    var ma = new Indicators.MA(99);
                    if (ma.MinimumInitValuesNecessary < course.Count)
                    {
                        ma.Init(course);
                        coin.Tension4d = (ma.Points.Last().Value - lastClose) / lastClose;
                    }
                    else
                        // not enough data points ready yet
                        coin.Tension4d = double.NaN;

                    if (cancel.IsCancellationRequested)
                        return;

                    Dispatcher.Invoke(RecallSearchDone);
                }
            }
            await Task.Run((Action)Search);
        }

        private void RecallSearchDone()
        {
            // Refresh value
            Model.Currencies[_searchCoinPtr] = Model.Currencies[_searchCoinPtr];
            Model.Progress = (100 * (_searchCoinPtr + 1)) / _searchCoins.Count;
        }

        Dictionary<string, string> Formats = new Dictionary<string, string>()
        {
            { nameof(CurrencyInfo.Volatility24h), "P2" },
            { nameof(CurrencyInfo.Tension4d), "P2" },
            { nameof(CurrencyInfo.Volume), "F0" }
        };
        private void Coins_DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (Formats.TryGetValue(e.PropertyName, out string format))
                (e.Column as DataGridTextColumn).Binding.StringFormat = format;
        }

        private void Coins_DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender != null)
            {
                if (sender is DataGrid grid && grid.SelectedItems?.Count == 1)
                {
                    var dgr = grid.SelectedItem as CurrencyInfo;
                    System.Diagnostics.Process.Start("https://www.tradingview.com/symbols/" + dgr.Currency + "BTC");
                }
            }

            
        }
    }
}
