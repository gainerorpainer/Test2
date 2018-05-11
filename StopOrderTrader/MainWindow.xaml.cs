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
using StopOrderTrader.Trading;
using static StopOrderTrader.Toolbox;

namespace StopOrderTrader
{
    public class MainWindowModel : Lib.NotifyModel
    {
        decimal _totalBTC;
        public decimal TotalBTC { get => _totalBTC; set { _totalBTC = value; OnPropertyChanged(nameof(TotalBTC)); } }
        public decimal TotalBTC_To_USDT => _totalBTC * BTC_To_USDT;

        decimal _freeBTC;
        public decimal FreeBTC { get => _freeBTC; set { _freeBTC = value; OnPropertyChanged(nameof(FreeBTC)); } }
        public decimal FreeBTC_To_USDT => _freeBTC * BTC_To_USDT;

        decimal _totalWalletBTC;
        public decimal TotalWalletBTC { get => _totalWalletBTC; set { _totalWalletBTC = value; OnPropertyChanged(nameof(TotalWalletBTC)); } }
        public decimal TotalWallet_To_USDT => _totalWalletBTC * BTC_To_USDT;

        public decimal BTC_To_USDT = 1;

        public ObservableCollection<Binance.Net.Objects.BinanceOrder> Orders { get; } = new ObservableCollection<Binance.Net.Objects.BinanceOrder>();

        public ObservableCollection<Deal> Deals { get; set; } = new ObservableCollection<Deal>();

        public ICommand RefreshOrder { get; set; }
        public ICommand RunStateMachine { get; set; }
        public ICommand GenerateDeal { get; set; }
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

        private readonly string[] ExcludeColumn = new string[] { "" };

        public void OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            string columnName = (e.PropertyDescriptor as System.ComponentModel.PropertyDescriptor).DisplayName;
            if (ExcludeColumn.Contains(columnName))
                e.Cancel = true;
        }


        private void RefreshOrders()
        {
            // Download all open orders
            Model.Orders.SyncWith(TradeInterface.Client.GetOpenOrders().GetOrThrow());
        }

        private void RefreshDeals()
        {
            StateMachine.Run(Store.DealDB.Instance.Deals);
            Model.Deals.SyncWith(Store.DealDB.Instance.Deals);
        }

        private void OnGenerateDeal()
        {
            GenerateDealWindow wnd = new GenerateDealWindow();
            if (wnd.ShowDialog() ?? false)
                RefreshDeals();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selected = (sender as DataGrid)?.SelectedItems;
            if (selected?.Count == 1)
            {
                DealDetailWindow wnd = new DealDetailWindow(selected[0] as Deal);
                wnd.ShowDialog();
            }
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

                TradeInterface.Client.SetApiCredentials(Lib.Encryption.ToInsecureString(Lib.Secrets.APIKey.Value), Lib.Encryption.ToInsecureString(Lib.Secrets.APISecret.Value));
            }

            SetupCommands();

            RefreshDeals();
            RefreshOrders();

            DataContext = Model;
        }

        private void SetupCommands()
        {
            Model.RefreshOrder = new Lib.ActionCommand(() => this.CallWithWaitWindow(RefreshOrders));
            Model.RunStateMachine = new Lib.ActionCommand(() => this.CallWithWaitWindow(RefreshDeals));
            Model.GenerateDeal = new Lib.ActionCommand(OnGenerateDeal);
        }

        private bool SetupTradingAccount()
        {
            var accountInfo = TradeInterface.Client.GetAccountInfo();
            if (accountInfo.Fails())
            {
                Error("Login Error", $"Cannot connect to Binance: \"{accountInfo.Error.Message} ({accountInfo.Error.Code})\"");
                return false;
            }

            // Get BTC -> USDT conv
            Model.BTC_To_USDT = TradeInterface.Client.GetPrice("BTCUSDT").GetOrThrow().Price;

            var btc = accountInfo.Data.Balances.First(x => x.Asset == "BTC");
            Model.FreeBTC = btc.Free;
            Model.TotalBTC = btc.Total;

            var conversions = TradeInterface.Client.GetAllPrices().GetOrThrow().Where(x => x.Symbol.EndsWith("BTC")).ToDictionary(x => x.Symbol.RemoveLast(3), x => x.Price);
            Model.TotalWalletBTC = accountInfo.Data.Balances.Select(x => conversions.ContainsKey(x.Asset) ? conversions[x.Asset] * x.Total : 0).Sum() + btc.Total;

            return true;
        }



        #endregion
    }
}
