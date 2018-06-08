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
using System.Windows.Controls.Primitives;
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

        public bool MakeDeal { get; set; }
        public string Symbol { get; set; }
        public decimal Change24 { get; set; }
        public decimal Volume24 { get; set; }
        public decimal MarketCap { get; set; }
        public double Volatility24h { get; set; }
        public double Tension4d { get; set; }
        public double Trend3m { get; set; }
    }

    public class GenerateDealWindowModel : Lib.NotifyModel
    {
        decimal _freeBTC;
        public decimal FreeBTC { get => _freeBTC; set { _freeBTC = value; OnPropertyChanged(nameof(FreeBTC)); OnPropertyChanged(nameof(FreeBTC_UDST)); } }
        public decimal FreeBTC_UDST => _freeBTC * BTC_To_USDT;

        public int PossibleDeals => (int)(FreeBTC / BTCPerDeal);

        bool _searchDone = true;
        public bool SearchDone { get => _searchDone; set { _searchDone = value; OnPropertyChanged(nameof(SearchDone)); OnPropertyChanged(nameof(SearchButtonLabel)); } }

        public string SearchButtonLabel => _searchDone ? "Calculate metrics" : "Stop";

        public ObservableCollection<CurrencyInfo> Currencies { get; set; } = new ObservableCollection<CurrencyInfo>();

        int _progress;
        public int Progress { get => _progress; set { _progress = value; OnPropertyChanged(nameof(Progress)); OnPropertyChanged(nameof(Progress)); } }

        decimal _btcPerDeal = 0.0031m;
        public decimal BTCPerDeal { get => _btcPerDeal; set { _btcPerDeal = value; OnPropertyChanged(nameof(BTCPerDeal)); OnPropertyChanged(nameof(PossibleDeals)); OnPropertyChanged(nameof(BTCPerDeal_USDT)); } }

        public decimal BTCPerDeal_USDT => _btcPerDeal * BTC_To_USDT;

        public ICommand Search { get; set; }
        public ICommand MakeDeals { get; set; }

        public decimal BTC_To_USDT = 1;

        decimal _globalChange24h = 0;
        public decimal GlobalChange24h { get => _globalChange24h; set { _globalChange24h = value; OnPropertyChanged(nameof(GlobalChange24h)); } }
    }

    /// <summary>
    /// Interaktionslogik für GenerateDealWindow.xaml
    /// </summary>
    public partial class GenerateDealWindow : Window
    {
        private const int MINIMUMCAP = 0;
        private const int MARKETINFOPAGING = 60;
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
            //var dict = _searchCoins.ToDictionary(x => x.Symbol);

            //_cancel.Cancel();

            //foreach (var item in Coins_DataGrid.SelectedItems)
            //{
            //    string symbol = (Coins_DataGrid.Columns[0].GetCellContent(item) as TextBlock).Text;
            //    new AdjustStrategyWindow(dict[symbol]).ShowDialog();
            //}

            //return;





            // Get all selected deals
            if (Coins_DataGrid.SelectedItems.Count > Model.PossibleDeals)
            {
                InfoPopup("Too many deals", $"You selected {Coins_DataGrid.SelectedItems.Count}, but you only have funds for {Model.PossibleDeals}", MessageBoxImage.Warning);
                return;
            }




            this.CallWithWaitWindow(() =>
            {

                foreach (var item in Model.Currencies.Where(x=> x.MakeDeal))
                {
                    // Buy enough coins to get 0.003 btc worth in order to be able to trade
                    string symbol = (Coins_DataGrid.Columns[0].GetCellContent(item) as TextBlock).Text + "BTC";
                    decimal btcPerAlt = TradeInterface.Client.GetPrice(symbol).GetOrThrow().Price;
                    ClientServerOrder orderPair = null;
                    for (int i = 0; i < 3; i++)
                    {
                        // Try with ever lower going quantity
                        try
                        {
                            decimal quantity = (1m - 0.01m * i) * Model.BTCPerDeal / btcPerAlt;
                            orderPair = TradeInterface.PlaceImmediateOrder(symbol, quantity: quantity, orderSide: Binance.Net.Objects.OrderSide.Buy);
                            break;
                        }
                        catch (BinanceAPIException ex)
                        {
                            // Rethrow if the error is not "insufficent balance"
                            if (ex.Message.Contains("insufficient balance") == false)
                                throw;
                        }
                    }

                    if (orderPair == null)
                        throw new Exception("Could not place buy order, insufficient funds?");

                    // make a db entry
                    Store.DealDb.MainInstance.Deals.Add(new Deal()
                    {
                        Id = Store.DealDb.GetNewId(),
                        BuyOrder = orderPair,
                        CreationTime = DateTime.Now,
                        CurrentResult = Deal.Result.NotDoneYet,
                        CurrentState = Deal.State.WaitForBuy,
                        Symbol = symbol,
                        BuyPrice = btcPerAlt,
                        Sell1Perc = 0.01m,
                        Sell2Perc = 0.02m,
                        SellStopLossPerc = 0.1m
                    });
                }
            }).ContinueWith(x => Dispatcher.Invoke(() => DialogResult = true));
        }

        private void Setup()
        {
            // Setup commands
            Model.Search = new Lib.ActionCommand(() => this.CallWithWaitWindow(Search));
            Model.MakeDeals = new Lib.ActionCommand(MakeDeals);

            // Setup btc conversions
            var accountInfo = TradeInterface.Client.GetAccountInfo().GetOrThrow();
            Model.BTC_To_USDT = TradeInterface.Client.GetPrice("BTCUSDT").GetOrThrow().Price;
            Model.FreeBTC = accountInfo.Balances.First(x => x.Asset == "BTC").Free;

            // Donwload all tradable coins
            var allCoins = TradeInterface.Client.Get24HPricesList().GetOrThrow()
                    .Where(x => x.Symbol.EndsWith("BTC"));

            // Download additional market info from CryptoCompare
            Dictionary<string, CoinInfo> coinDict = new Dictionary<string, CoinInfo>();
            for (int i = 0; i < allCoins.Count(); i = i + MARKETINFOPAGING)
            {
                CryptoCompare.GetCoinInfo(allCoins.Select(x => x.Symbol.RemoveLast("BTC".Length)).Skip(i).Take(MARKETINFOPAGING)).ToList().ForEach(x => coinDict.Add(x.Key, x.Value));
            }

            // Merge infos
            var tradableCoins = allCoins.Select(x =>
            {
                coinDict.TryGetValue(x.Symbol.RemoveLast("BTC".Length), out CoinInfo coinInfo);

                return new CurrencyInfo()
                {
                    Symbol = x.Symbol.RemoveLast("BTC".Length),
                    Volume24 = coinInfo?.Volume24 ?? 0,
                    Change24 = x.PriceChangePercent / 100m,
                    MarketCap = coinInfo?.MarketCap ?? 0
                };
            });

            // Calculate global change
            Model.GlobalChange24h = tradableCoins.WeightedAverage(x => x.Change24, x => x.Volume24).Value;

            // Prefilter
            _searchCoins = tradableCoins.Where(x => x.MarketCap > MINIMUMCAP).ToList();

            Model.Currencies.SyncWith(_searchCoins);

            DataContext = Model;

            Search();
        }

        void Search()
        {
            if (Model.SearchDone == true)
            {
                Model.SearchDone = false;
                _cancel = new CancellationTokenSource();

                Coins_DataGrid.SortBy(nameof(CurrencyInfo.Tension4d), System.ComponentModel.ListSortDirection.Descending);

                // Now, deep search each of them in background
                _task = AsyncSearch(_cancel.Token).ContinueWith((x) => Dispatcher.Invoke(() =>
                {
                    Model.SearchDone = true;
                }));
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
                    List<Binance.Net.Objects.BinanceKline> course = TradeInterface.Client.GetKlines(coin.Symbol + "BTC", Binance.Net.Objects.KlineInterval.OneHour, limit: 24 * 7).GetOrThrow().ToList();
                    double lastClose = (double)course.Last().Close;

                    // Volatility is the r^2 fitting factor against a linear regression of the last 24 h closes
                    coin.Volatility24h = 1 - Lib.Algorithm.GetLinearRegression(course.TakeLast(24).Select(x => new Point(x.CloseTime.Ticks, (double)x.Close)).ToList()).R;

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

                    // Get course for last 3 month
                    var course3month = TradeInterface.Client.GetKlines(coin.Symbol + "BTC", Binance.Net.Objects.KlineInterval.OneDay, limit: 28 * 3).GetOrThrow();
                    var ma2 = new Indicators.MA(25);
                    if (ma2.MinimumInitValuesNecessary < course3month.Count())
                    {
                        ma2.Init(course3month.TakeLast(28 * 3));
                        coin.Trend3m = (ma2.Points.Last().Value - ma2.Points.First().Value) / ma2.Points.Last().Value;
                    }
                    else
                        // not enough data points ready yet
                        coin.Trend3m = double.NaN;



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
            { nameof(CurrencyInfo.Volume24), "C0" },
            { nameof(CurrencyInfo.Change24), "P2" },
            { nameof(CurrencyInfo.Volatility24h), "P2" },
            { nameof(CurrencyInfo.Trend3m), "P2" },
            { nameof(CurrencyInfo.Tension4d), "P2" },
            { nameof(CurrencyInfo.MarketCap), "C0" }
        };
        private void Coins_DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (Formats.TryGetValue(e.PropertyName, out string format))
                (e.Column as DataGridTextColumn).Binding.StringFormat = format;

            if (e.Column is DataGridTextColumn)
            {
                (e.Column as DataGridTextColumn).ElementStyle = FindResource("TextRight") as Style;
                e.Column.IsReadOnly = true;
            }
            else if (e.Column.Header.ToString() == "MakeDeal")
            {
               
            }
        }

        private void Coins_DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender != null)
            {
                if (sender is DataGrid grid && grid.SelectedItems?.Count == 1)
                {
                    var dgr = grid.SelectedItem as CurrencyInfo;

                    System.Diagnostics.Process.Start("https://coinmarketcap.com/currencies/" + CoinMarketCap.SymbolToWebsiteslug[dgr.Symbol]);
                }
            }
        }
    }
}
