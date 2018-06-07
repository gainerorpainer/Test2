using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Definitions.Series;
using LiveCharts.Wpf;
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
    public class AdjustStrategyWindowModel : Lib.NotifyModel
    {
        private decimal _sell1Perc = 0.01m;
        private decimal _sell2Perc = 0.02m;
        private decimal _stopLossPerc = 0.02m;
        private decimal _buyPrice = 0m;
        private  Binance.Net.Objects.KlineInterval _candleLength = Binance.Net.Objects.KlineInterval.OneDay;
        private bool _isBuyMarketPrice = true;
        private decimal _marketPrice;

        public AdjustStrategyWindowModel() { }

        public CurrencyInfo CurrencyInfo { get; set; } = new CurrencyInfo();

        public decimal MarketPrice
        {
            get => _marketPrice;
            set
            {
                _marketPrice = value;
                if (_isBuyMarketPrice)
                    BuyPrice = value;
            }
        }

        public SeriesCollection SeriesCollection { get; set; } = new SeriesCollection()
        {
            new OhlcSeries()
            {
                Title = "KLines",
                Values = new ChartValues<OhlcPoint>()
            },
            new LineSeries()
            {
                Title = "BuyLimit",
                Values = new ChartValues<ObservablePoint>()
                {
                    new ObservablePoint(0, 0),
                    new ObservablePoint(48 - 1, 0),
                },
                Fill = Brushes.Transparent,
                PointGeometry = null,
                StrokeDashArray = new DoubleCollection() { 2, 2 },
            },
            new LineSeries()
            {
                Title = "Sell1Limit",
                Values = new ChartValues<ObservablePoint>()
                {
                    new ObservablePoint(0, 0),
                    new ObservablePoint(48 - 1, 0),
                },
                Fill = Brushes.Transparent,
                PointGeometry = null,
                StrokeDashArray = new DoubleCollection() { 2, 2 },
            },
            new LineSeries()
            {
                Title = "Sell2Limit",
                Values = new ChartValues<ObservablePoint>()
                {
                    new ObservablePoint(0, 0),
                    new ObservablePoint(48 - 1, 0),
                },
                Fill = Brushes.Transparent,
                PointGeometry = null,
                StrokeDashArray = new DoubleCollection() { 2, 2 },
            },
            new LineSeries()
            {
                Title = "StopLossLimit",
                Values = new ChartValues<ObservablePoint>()
                {
                    new ObservablePoint(0, 0),
                    new ObservablePoint(48 - 1, 0),
                },
                Fill = Brushes.Transparent,
                PointGeometry = null,
                StrokeDashArray = new DoubleCollection() { 2, 2 },
            }
        };

        public ObservableCollection<string> Labels { get; set; } = new ObservableCollection<string>();

        public bool IsBuyMarketPrice { get => _isBuyMarketPrice; set { _isBuyMarketPrice = value; OnPropertyChanged(nameof(IsBuyMarketPrice)); BuyPrice = _marketPrice; } }
        public decimal BuyPrice
        {
            get => _buyPrice;
            set
            {
                _buyPrice = value;
                OnPropertyChanged(nameof(BuyPrice));
                UpdateChartLimits();
            }
        }

        public decimal Sell1Perc
        {
            get => _sell1Perc;
            set
            {
                _sell1Perc = value < _sell2Perc ? value : _sell2Perc;
                OnPropertyChanged(nameof(Sell1Perc));
                UpdateChartLimits();
            }
        }
        public decimal Sell2Perc
        {
            get => _sell2Perc; set
            {
                _sell2Perc = value;
                OnPropertyChanged(nameof(Sell2Perc));
                Sell1Perc = _sell1Perc < _sell2Perc ? _sell1Perc : _sell2Perc;
                UpdateChartLimits();
            }
        }

        public decimal StopLossPerc
        {
            get => _stopLossPerc; set
            {
                _stopLossPerc = value;
                OnPropertyChanged(nameof(StopLossPerc));
                UpdateChartLimits();
            }
        }

        public Binance.Net.Objects.KlineInterval CandleLength { get => _candleLength; set { _candleLength = value; OnPropertyChanged(nameof(CandleLength)); DownloadKlines?.Execute(null); } }

        public string[] CandleLengthOptions => Enum.GetNames(typeof(Binance.Net.Objects.KlineInterval));

        public ICommand DownloadKlines { get; set; }


        private void UpdateChartLimits()
        {
            void SetLimit(ISeriesView lineSeries, double val)
            {
                var p1 = lineSeries.Values[0] as ObservablePoint;
                var p2 = lineSeries.Values[1] as ObservablePoint;

                p1.Y = p2.Y = val;
            }

            SetLimit(SeriesCollection[1], (double)_buyPrice);
            SetLimit(SeriesCollection[2], (double)(_buyPrice * (1 + _sell1Perc)));
            SetLimit(SeriesCollection[3], (double)(_buyPrice * (1 + _sell2Perc)));
            SetLimit(SeriesCollection[4], (double)(_buyPrice * (1 - _stopLossPerc)));
        }
    }

    /// <summary>
    /// Interaktionslogik für AdjustStrategyWindow.xaml
    /// </summary>
    public partial class AdjustStrategyWindow : Window
    {
        AdjustStrategyWindowModel Model;

        public AdjustStrategyWindow(CurrencyInfo currency)
        {
            InitializeComponent();

            Model = new AdjustStrategyWindowModel()
            {
                CurrencyInfo = currency,
                MarketPrice = Trading.TradeInterface.Client.GetPrice(currency.Symbol + "BTC").GetOrThrow().Price,
                DownloadKlines = new Lib.ActionCommand(DownloadKlines)
            };

            // Workaround for combobox
            CandleLength_Combobox.SelectedIndex = (int)Model.CandleLength;

            DownloadKlines();

            DataContext = Model;
        }

        private void DownloadKlines()
        {
            // Disable dropdown until load is complete
            CandleLength_StackPanel.IsEnabled = false;

            // Download klines async
            var klines = Trading.TradeInterface.Client.GetKlinesAsync(Model.CurrencyInfo.Symbol + "BTC", Model.CandleLength, limit: 48)
                .ContinueWith(x => Dispatcher.Invoke(() => OnArriveKlines(x.Result.GetOrThrow())));
        }

        private void OnArriveKlines(Binance.Net.Objects.BinanceKline[] result)
        {
            OhlcSeries ohlcSeries = Model.SeriesCollection[0] as OhlcSeries;
            ohlcSeries.Values.Clear();
            ohlcSeries.Values.AddRange(result.Select(x => new OhlcPoint((double)x.Open, (double)x.High, (double)x.Low, (double)x.Close)));
            Model.Labels.SyncWith(result.Select(x => x.CloseTime.ToString("dd.MM hh:mm")));

            CandleLength_StackPanel.IsEnabled = true;
        }

        private void Slider_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Model.IsBuyMarketPrice = false;
        }
    }
}
