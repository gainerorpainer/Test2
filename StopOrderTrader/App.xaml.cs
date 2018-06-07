using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace StopOrderTrader
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            StopOrderTrader.Properties.Settings.Default.Reload();
            Store.DealDb.Load();
            Store.AccountDb.Load();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            StopOrderTrader.Properties.Settings.Default.Save();
            Store.DealDb.Save();
            Store.AccountDb.Save();
        }
    }
}
