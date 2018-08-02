using Harmony;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace USBHelperInjector
{
    public class Injector
    {
        private static X509Certificate2 CaCert { get; set; }

        public static void Init()
        {
            var harmony = HarmonyInstance.Create("me.failedshack.usbhelperinjector");

            var assembly = Assembly.Load("WiiU_USB_Helper");
            var settingsType = assembly.GetType("WIIU_Downloader.Properties.Settings");
            var donationKey = settingsType.GetProperty("DonationKey");

            harmony.Patch(donationKey.GetGetMethod(), prefix: new HarmonyMethod(Overrides.GetMethod("GetDonationKey", typeof(string).MakeByRefType())));
            harmony.Patch(donationKey.GetSetMethod(), prefix: new HarmonyMethod(Overrides.GetMethod("SetDonationKey", typeof(string).MakeByRefType())));

            // Finds the Proxy property inside the NusGrabberForm (which name is obfuscated)
            var proxy = (from type in assembly.GetTypes()
                         where typeof(Form).IsAssignableFrom(type)
                         from prop in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance)
                         where prop.Name == "Proxy"
                         select prop).FirstOrDefault();

            harmony.Patch(proxy.GetGetMethod(true), prefix: new HarmonyMethod(Overrides.GetMethod("GetProxy", typeof(WebProxy).MakeByRefType())));
            harmony.Patch(proxy.GetSetMethod(true), prefix: new HarmonyMethod(Overrides.GetMethod("SetProxy", typeof(WebProxy).MakeByRefType())));

            harmony.PatchAll();
        }

        public static void TrustCertificateAuthority(X509Certificate2 cert)
        {
            CaCert = cert;
            ServicePointManager.ServerCertificateValidationCallback += ValidateServerCertificate;
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            X509Chain privateChain = new X509Chain();
            privateChain.ChainPolicy.RevocationMode = X509RevocationMode.Offline;

            privateChain.ChainPolicy.ExtraStore.Add(CaCert);
            privateChain.Build(new X509Certificate2(certificate));

            bool isValid = true;

            foreach (X509ChainStatus chainStatus in privateChain.ChainStatus)
            {
                if (chainStatus.Status != X509ChainStatusFlags.NoError)
                {
                    isValid = false;
                    break;
                }
            }

            return isValid;
        }
    }
}
