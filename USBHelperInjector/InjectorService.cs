using Harmony;
using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Threading.Tasks;
using USBHelperInjector.Contracts;
using USBHelperInjector.Patches;
using USBHelperInjector.Properties;

namespace USBHelperInjector
{
    public class InjectorService : IInjectorService
    {
        public static ILauncherService LauncherService { get; private set; }
        public static X509Certificate2 CaCert { get; private set; }

        public static void Init()
        {
            MessageBoxPatch.Replace("tp9+kFO7LOSD0AZ5zUBHrA==", Resources.Disclaimer);

            var factory = new ChannelFactory<ILauncherService>(new NetNamedPipeBinding(), "net.pipe://localhost/LauncherService");
            LauncherService = factory.CreateChannel();

            ServiceHost host = new ServiceHost(typeof(InjectorService), new Uri("net.pipe://localhost/InjectorService"));
            host.AddServiceEndpoint(typeof(IInjectorService), new NetNamedPipeBinding(), "");
            host.Open();

            LauncherService.SendInjectorSettings();

            var harmony = HarmonyInstance.Create("me.failedshack.usbhelperinjector");
            var assembly = Assembly.GetExecutingAssembly();
            assembly.GetTypes().Do(type =>
            {
                var parentMethodInfos = type.GetHarmonyMethods();
                if (parentMethodInfos != null && parentMethodInfos.Count() > 0)
                {
                    var info = HarmonyMethod.Merge(parentMethodInfos);
                    var processor = new PatchProcessor(harmony, type, info);
                    if (!(Overrides.DisableOptionalPatches && Optional.IsOptional(type)))
                    {
                        processor.Patch();
                    }
                }
            });
        }

        public void ForceKeySiteForm()
        {
            Overrides.ForceKeySiteForm = true;
        }

        // Should make the given CA certificate be trusted (currently only disables HTTPs validation)
        public void TrustCertificateAuthority(byte[] rawCertData)
        {
            CaCert = new X509Certificate2(rawCertData);
            ServicePointManager.ServerCertificateValidationCallback += ValidateServerCertificate;
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // Disable HTTPs validation
            return true;
        }

        public void SetDonationKey(string donationKey)
        {
            Overrides.DonationKey = donationKey;
        }

        public void SetDownloaderMaxRetries(int maxRetries)
        {
            Overrides.MaxRetries = maxRetries;
        }

        public void SetDownloaderRetryDelay(int delay)
        {
            Overrides.DelayBetweenRetries = delay;
        }

        public void SetProxy(string address)
        {
            Overrides.Proxy = new WebProxy(address);
        }

        public void SetDisableOptionalPatches(bool disableOptional)
        {
            Overrides.DisableOptionalPatches = disableOptional;
        }
    }
}
