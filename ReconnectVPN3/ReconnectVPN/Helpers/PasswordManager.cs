using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Security.Credentials.UI;

namespace ReconnectVPN.Helpers
{
    public static class PasswordManager
    {
        public static void SetPassword(string vpnName, string password, string userName)
        {
            PasswordVault myVault = new PasswordVault();
            myVault.Add(new PasswordCredential(vpnName, userName, password));
        }

        public static void RemovePassword(string vpnName, string userName)
        {
            PasswordVault myVault = new PasswordVault();

            var password = myVault.Retrieve(vpnName, userName);
            if (password != null)
                myVault.Remove(password);
        }

        public static async Task<string> SignInAsync(string vpnName, string userName)
        {
            var result = await UserConsentVerifierInterop.RequestVerificationForWindowAsync(Process.GetCurrentProcess().MainWindowHandle, "このVPNにアクセスするには資格情報が必要です。");
            if (result != UserConsentVerificationResult.Verified)
                return null;

            try
            {
                var vault = new PasswordVault();
                var credentials = vault.Retrieve(vpnName, userName);
                return credentials?.Password;
            }
            catch (COMException)
            {
                return null;
            }
        }
    }
}
