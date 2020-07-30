using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using USBHelperInjector.Contracts;
using USBHelperInjector.Patches.Attributes;

namespace USBHelperInjector
{
    public class InjectorService : IInjectorService
    {
        public static Harmony Harmony { get; private set; }
        public static ILauncherService LauncherService { get; private set; }
        public static X509Certificate2 CaCert { get; private set; }
        public static string HelperVersion { get; private set; }
        public static bool Portable { get; private set; }
        public static bool ForceHttp { get; private set; }
        public static bool FunAllowed { get; private set; }
        public static string[] DisableTabs { get; private set; }
        public static string EshopRegion { get; private set; }
        public static bool SplitUnpackDirectories { get; private set; }

        public static void Init()
        {
            var factory = new ChannelFactory<ILauncherService>(new NetNamedPipeBinding(""), "net.pipe://localhost/LauncherService");
            LauncherService = factory.CreateChannel();

            ServiceHost host = new ServiceHost(typeof(InjectorService), new Uri("net.pipe://localhost/InjectorService"));
            host.AddServiceEndpoint(typeof(IInjectorService), new NetNamedPipeBinding(""), "");
            host.Open();

            LauncherService.SendInjectorSettings();

            Harmony = new Harmony("me.failedshack.usbhelperinjector");
            var assembly = Assembly.GetExecutingAssembly();
            assembly.GetTypes()
                .Where(type => VersionSpecific.Applies(type, HelperVersion) && !(Overrides.DisableOptionalPatches && Optional.IsOptional(type)))
                .Do(type => Harmony.CreateClassProcessor(type).Patch());
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

        public void SetPublicKey(string publicKey)
        {
            Overrides.PublicKey = publicKey;
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

        public void SetHelperVersion(string helperVersion)
        {
            HelperVersion = helperVersion;
        }

        public void SetPortable(bool portable)
        {
            Portable = portable;
        }

        public void SetForceHttp(bool forceHttp)
        {
            ForceHttp = forceHttp;
        }

        public void SetFunAllowed(bool funAllowed)
        {
            FunAllowed = funAllowed;
        }

        public void SetDisableTabs(string[] disableTabs)
        {
            DisableTabs = disableTabs;
        }

        public void SetLocale(string locale)
        {
            Localization.Clear();
            Localization.Namespace = "injector";
            Localization.Load(Path.Combine("locale", $"{locale}.json"));
            Localization.Load(
                Path.Combine("locale", $"{locale}.local.json"),
                Path.Combine("locale", $"{Localization.DefaultLocale}.local.json")
            );
            Localization.Override("welcome.disclaimer.unused", "override.customdisclaimer".Localize());
        }

        public void SetEshopRegion(string eshopRegion)
        {
            EshopRegion = eshopRegion;
        }

        public void SetSplitUnpackDirectories(bool splitUnpackDirectories)
        {
            SplitUnpackDirectories = splitUnpackDirectories;
        }
    }
}
