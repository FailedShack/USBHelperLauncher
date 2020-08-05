using HarmonyLib;
using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using USBHelperInjector.IPC;
using System.Windows.Interop;
using System.Windows.Media;
using USBHelperInjector.Patches.Attributes;

namespace USBHelperInjector
{
    [ServiceBehavior(AddressFilterMode = AddressFilterMode.Any)]
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
        public static string LocaleFile { get; private set; }
        public static string EshopRegion { get; private set; }
        public static bool SplitUnpackDirectories { get; private set; }
        public static bool WineCompat { get; private set; }

        public static void Init()
        {
            var args = Environment.GetCommandLineArgs();
            var ipcType = (IPCType)Enum.Parse(typeof(IPCType), args[2]);
            var launcherUri = args[3];

            IPCUtil.CreateService(
                ipcType,
                typeof(InjectorService),
                typeof(IInjectorService),
                out var serviceUri
            );

            LauncherService = IPCUtil.CreateChannel<ILauncherService>(ipcType, launcherUri);
            LauncherService.SendInjectorSettings(serviceUri);

            Harmony = new Harmony("me.failedshack.usbhelperinjector");
            var assembly = Assembly.GetExecutingAssembly();
            assembly.GetTypes()
                .Where(type =>
                       VersionSpecific.Applies(type, HelperVersion)
                    && !(Overrides.DisableOptionalPatches && Optional.IsOptional(type))
                    && (!WineOnly.IsWineOnly(type) || WineCompat)
                )
                .Do(type => Harmony.CreateClassProcessor(type).Patch());

            if (WineCompat)
            {
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            }
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

        public void SetLocaleFile(string localeFile)
        {
            LocaleFile = localeFile;
        }

        public void SetEshopRegion(string eshopRegion)
        {
            EshopRegion = eshopRegion;
        }

        public void SetSplitUnpackDirectories(bool splitUnpackDirectories)
        {
            SplitUnpackDirectories = splitUnpackDirectories;
        }

        public void SetWineCompat(bool wineCompat)
        {
            WineCompat = wineCompat;
        }
    }
}
