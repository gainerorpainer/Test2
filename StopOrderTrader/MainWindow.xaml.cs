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
using System.Windows.Navigation;
using System.Windows.Shapes;

using Binance.Net;
using Binance.Net.Objects;
using StopOrderTrader.Trading;
using static StopOrderTrader.Toolbox;

namespace StopOrderTrader
{
    public class MainWindowModel : Lib.NotifyModel
    {
        decimal _totalBTC;
        public decimal TotalBTC { get => _totalBTC; set { _totalBTC = value.Normalize(); OnPropertyChanged(nameof(TotalBTC)); } }
        public decimal TotalBTC_To_USDT => (_totalBTC * BTC_To_USDT).Normalize();

        decimal _freeBTC;
        public decimal FreeBTC { get => _freeBTC; set { _freeBTC = value.Normalize(); OnPropertyChanged(nameof(FreeBTC)); } }
        public decimal FreeBTC_To_USDT => (_freeBTC * BTC_To_USDT).Normalize();

        decimal _totalWalletBTC;
        public decimal TotalWalletBTC { get => _totalWalletBTC; set { _totalWalletBTC = value.Normalize(); OnPropertyChanged(nameof(TotalWalletBTC)); } }
        public decimal TotalWallet_To_USDT => (_totalWalletBTC * BTC_To_USDT).Normalize();

        public decimal BTC_To_USDT = 1;

        public ObservableCollection<Binance.Net.Objects.BinanceOrder> Orders { get; } = new ObservableCollection<Binance.Net.Objects.BinanceOrder>();

        public ObservableCollection<Deal> Deals { get; set; } = new ObservableCollection<Deal>();

        public ICommand RefreshOrder { get; set; }
        public ICommand RunStateMachine { get; set; }
        public ICommand GenerateDeal { get; set; }
        public ICommand DedustLeftovers { get; set; }
        public ICommand ArchiveDeals { get; set; }
    }

    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindowModel Model { get; private set; } = new MainWindowModel();

        public MainWindow()
        {
            InitializeComponent();

            // Detach the long, blocking "Setup" procedure after rendering
            Dispatcher.RefreshThenContinue(() => this.CallWithWaitWindow(Setup));
        }


        private void RefreshOpenOrders()
        {
            // Download all open orders
            try
            {
                Model.Orders.SyncWith(TradeInterface.Client.GetOpenOrders().GetOrThrow());
            }
            catch (BinanceAPIException ex)
            {
                Popup("API Exception", ex.Message, MessageBoxImage.Error);
            }

        }

        private void RefreshDeals()
        {
            try
            {
                StateMachine.Run(Store.DealDb.MainInstance.Deals);
                Model.Deals.SyncWith(Store.DealDb.MainInstance.Deals.AsEnumerable().Reverse());
            }
            catch (BinanceAPIException ex)
            {
                Popup("API Exception", ex.Message, MessageBoxImage.Error);
            }
        }

        private void OnGenerateDeal()
        {
            GenerateDealWindow wnd = new GenerateDealWindow();
            if (wnd.ShowDialog() ?? false)
            {
                this.CallWithWaitWindow(() =>
                {
                    RefreshDeals();
                    RefreshOpenOrders();
                });
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selected = (sender as DataGrid)?.SelectedItems;
            if (selected?.Count == 1)
            {
                DealDetailWindow wnd = new DealDetailWindow(selected[0] as Deal);
                wnd.ShowDialog();
                if (wnd.DialogResult == true)
                {
                    RefreshDeals();
                    RefreshOpenOrders();
                }
            }
        }

        private void OnDedust()
        {
            // Try to sell Leftover for each deal
            var doneDeals = Store.DealDb.MainInstance.Deals.Where(x => (x.CurrentState == Deal.State.Done) && (x.Leftovers != 0)).ToList();
            string txt = "";
            foreach (var deal in doneDeals)
            {
                txt += $"{deal.Id} - {deal.Symbol} ({deal.CreationTime.ToString("dd.MM.yyyy hh:mm")}): ";
                try
                {
                    var trade = TradeInterface.PlaceImmediateOrder(deal.Symbol, deal.Leftovers, OrderSide.Sell);
                    var trades = TradeInterface.GetActualOrders(deal.Symbol, trade.ClientOrderId);
                    decimal quantity = trades.Sum(x => x.Quantity).Normalize();
                    decimal filledPrice = trades.EffectivePrice().Normalize();

                    deal.Leftovers = (deal.Leftovers - quantity).Normalize();

                    decimal btc = (quantity * filledPrice).Normalize();
                    txt += $"Sold {quantity} for a price of {filledPrice} (={btc})";
                }
                catch (BinanceAPIException ex)
                {
                    txt += $"API Exception: {ex.Message}";
                }
                catch (TradeInterface.TradingException ex)
                {
                    txt += $"Trading Exception: {ex.Message}";
                }
                txt += Environment.NewLine;
            }

            Popup("Result", txt, MessageBoxImage.Information);

            if (MessageBox.Show("Do you want to reset all leftovers to \"0\"?", "Reset Leftovers?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                foreach (var deal in doneDeals)
                    deal.Leftovers = 0;

            RefreshDeals();
        }


        private void OnArchiveDeals()
        {
            // Get selected
            var selected = Deals_DataGrid.SelectedItems.Cast<Deal>();

            if (selected.Count() == 0)
            {
                Popup("Info", "You can archive Deals by selecting them in the list and then pressing this button.", MessageBoxImage.Information);
                return;
            }

            var doneDeals = selected.Where(x => x.CurrentState == Deal.State.Done);

            if (doneDeals.Count() == 0)
            {
                Popup("Info", "Your selection does not contain any \"done\" deals. No deals were archived", MessageBoxImage.Information);
                return;
            }

            // make a new db instance
            var archiveDb = new Store.DealDb(AppContext.BaseDirectory + $"Store\\dealdb_{DateTime.Now.ToString("yyyyMMdd")}_{RandomString(5)}.xml");
            foreach (var item in doneDeals)
            {
                // Add to new 
                archiveDb.Deals.Add(item);

                // Remove from old 
                Store.DealDb.MainInstance.Deals.Remove(item);
            }

            // Serialize both
            archiveDb.Serialize();
            Store.DealDb.Save();

            RefreshDeals();
        }




        #region Setup

        private void Setup()
        {
            // Try to load stored login information
            while (SetupTradingAccount() == false)
            {
                // Something does not work, so now we loop into the login screen until it works
                LoginWindow loginWindow = new LoginWindow();

                if (loginWindow.ShowDialog() == false)
                {
                    Close();
                    return;
                }
            }

            SetupCommands();

            RefreshDeals();
            RefreshOpenOrders();

            DataContext = Model;
        }

        private void SetupCommands()
        {
            Model.RefreshOrder = new Lib.ActionCommand(() => this.CallWithWaitWindow(RefreshOpenOrders));
            Model.RunStateMachine = new Lib.ActionCommand(() => this.CallWithWaitWindow(RefreshDeals));
            Model.GenerateDeal = new Lib.ActionCommand(OnGenerateDeal);
            Model.DedustLeftovers = new Lib.ActionCommand(() => this.CallWithWaitWindow(OnDedust));
            Model.ArchiveDeals = new Lib.ActionCommand(OnArchiveDeals);
        }

        private bool SetupTradingAccount()
        {
            try
            {
                TradeInterface.Load();
            }
            catch (Exception ex)
            {
                Popup("Setup error", $"Encoutered an error during setup: \"{ex.Message}\"", MessageBoxImage.Error);
                return false;
            }

            // Sync server time
            var timeInfo = TradeInterface.Client.GetServerTime(true);
            if (timeInfo.Fails())
            {
                Popup("Sync Error", $"Could not sync with Binance server: \"{timeInfo.Error.Message} ({timeInfo.Error.Code})\"", MessageBoxImage.Error);
                return false;
            }

            var accountInfo = TradeInterface.Client.GetAccountInfo();
            if (accountInfo.Fails())
            {
                Popup("Login Error", $"Cannot connect to Binance: \"{accountInfo.Error.Message} ({accountInfo.Error.Code})\"", MessageBoxImage.Error);
                return false;
            }

            // Get BTC -> USDT conv
            Model.BTC_To_USDT = TradeInterface.Client.GetPrice("BTCUSDT").GetOrThrow().Price;

            var btc = accountInfo.Data.Balances.First(x => x.Asset == "BTC");
            Model.FreeBTC = btc.Free;
            Model.TotalBTC = btc.Total;

            var conversions = TradeInterface.Client.GetAllPrices().GetOrThrow().Where(x => x.Symbol.EndsWith("BTC")).ToDictionary(x => x.Symbol.RemoveLast(3), x => x.Price);
            Model.TotalWalletBTC = accountInfo.Data.Balances.Select(x => conversions.ContainsKey(x.Asset) ? conversions[x.Asset] * x.Total : 0).Sum() + btc.Total;

            // Serialize account info
            Store.AccountDb.Instance.Tickers.Add(new Store.AccountInfo()
            {
                TickerTime = DateTime.Now,
                BinanceAccountInfo = accountInfo.Data
            });

            return true;
        }

        #endregion

        #region Visuals

        // Exclude columns from deals grid
        private readonly string[] ExcludeColumn = new string[] { nameof(Deal.BuyPrice) };


        // Special format for certain columns
        Dictionary<string, string> Formats = new Dictionary<string, string>()
        {
            { nameof(Deal.Sell1Perc), "P2" },
            { nameof(Deal.Sell2Perc), "P2" },
            { nameof(Deal.SellStopLossPerc), "P2" },
        };
        private void Deals_DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            string columnName = (e.PropertyDescriptor as System.ComponentModel.PropertyDescriptor).DisplayName;
            if (ExcludeColumn.Contains(columnName))
                e.Cancel = true;

            if (Formats.TryGetValue(e.PropertyName, out string format))
                (e.Column as DataGridTextColumn).Binding.StringFormat = format;
        }


        // Exclude columns from open orders grid
        private readonly string[] ExcludeColumn2 = new string[] { nameof(BinanceOrder.IcebergQuantity), nameof(BinanceOrder.IsWorking), nameof(BinanceOrder.ClientOrderId), nameof(BinanceOrder.OrderId) };
        public void OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            string columnName = (e.PropertyDescriptor as System.ComponentModel.PropertyDescriptor).DisplayName;
            if (ExcludeColumn2.Contains(columnName))
                e.Cancel = true;
        }

        #endregion


    }
}
