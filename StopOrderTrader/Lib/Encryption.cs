using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace StopOrderTrader.Lib
{
    static class Encryption
    {
        static byte[] entropy = Encoding.Unicode.GetBytes("2q347zasdsjAphs139H`%89íjsdpk1");

        public static string EncryptString(SecureString input)
        {
            string insecure = ToInsecureString(input);
            byte[] encryptedData = System.Security.Cryptography.ProtectedData.Protect(
                Encoding.Unicode.GetBytes(insecure),
                entropy,
                System.Security.Cryptography.DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }

        public static SecureString DecryptString(string encryptedData)
        {
            try
            {
                byte[] decryptedData = System.Security.Cryptography.ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedData),
                    entropy,
                    System.Security.Cryptography.DataProtectionScope.CurrentUser);
                string raw = Encoding.Unicode.GetString(decryptedData);
                var result = ToSecureString(ref raw);
                return result;
            }
            catch
            {
                return new SecureString();
            }
        }

        public static SecureString ToSecureString(ref string input)
        {
            SecureString secure = new SecureString();
            foreach (char c in input)
            {
                secure.AppendChar(c);
            }
            secure.MakeReadOnly();

            input = null;

            return secure;
        }

        public static string ToInsecureString(SecureString input)
        {
            string returnValue = string.Empty;
            IntPtr ptr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(input);
            try
            {
                returnValue = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(ptr);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(ptr);
            }
            return returnValue;
        }
    }
}
