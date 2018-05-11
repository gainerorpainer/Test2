using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
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
    public class LoginWindowModel : Lib.NotifyModel
    {
        string errorText;
        public string ErrorText { get => errorText; set  { errorText = value; OnPropertyChanged(nameof(ErrorText)); } }

        public ICommand OKCommand { get; set; }
    }

    /// <summary>
    /// Interaktionslogik für LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindowModel Model { get; } = new LoginWindowModel();

        public LoginWindow()
        {
            Model.OKCommand = new Lib.ActionCommand(() => Button_Click(this, null));

            InitializeComponent();

            DataContext = Model;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // OK
            if (UseExisting_RadioButton.IsChecked ?? false)
            {
                // Try to lookup existing
                if (Lib.Secrets.APIKey.IsSet() && Lib.Secrets.APISecret.IsSet())
                {
                    // We are done
                    DialogResult = true;
                }
                else
                    // Error
                    Error("Could not find existing login, setup new!");
            }
            else
            {
                // New one
                string insecString = Key_PasswordBox.Password;
                Key_PasswordBox.Password = null;
                insecString = Secret_PasswordBox.Password;
                Secret_PasswordBox.Password = null;

                Lib.Secrets.APIKey.Value = Lib.Encryption.ToSecureString(ref insecString);
                Lib.Secrets.APISecret.Value = Lib.Encryption.ToSecureString(ref insecString);

                DialogResult = true;
            }
        }

        private void Error(string err)
        {
            Model.ErrorText = err;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ANY_PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SetupNew_RadioButton.IsChecked = true;
        }
    }
}
