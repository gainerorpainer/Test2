using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace StopOrderTrader.Lib
{
    static class Secrets
    {
        public class Secret
        {
            public Secret(string which)
            {
                _propertyChooser = typeof(Properties.Settings).GetProperty(which);
            }

            public string Name { get; }

            public SecureString Value
            {
                get => Encryption.DecryptString(GetValue());
                set => SetValue(Encryption.EncryptString(value));
            }

            public bool IsSet() => GetValue()?.Length > 0;

            // Private fields
            PropertyInfo _propertyChooser;

            // Statics
            static readonly Properties.Settings DefaultSettings = Properties.Settings.Default;


            // Private Methods
            string GetValue() => (string)_propertyChooser.GetValue(DefaultSettings);
            void SetValue(string val) => _propertyChooser.SetValue(DefaultSettings, val);
        }

        public static Secret APIKey => new Secret(nameof(APIKey));
        public static Secret APISecret => new Secret(nameof(APISecret));
    }
}
