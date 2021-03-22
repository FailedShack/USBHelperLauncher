using System;
using Fiddler;
using System.ServiceModel;
using USBHelperInjector.IPC;
using USBHelperLauncher.Configuration;

namespace USBHelperLauncher
{
    [ServiceBehavior(AddressFilterMode = AddressFilterMode.Any)]
    class LauncherService : ILauncherService
    {
        public void SetKeySite(string site, string url)
        {
            Settings.TitleKeys[site] = url;
            Settings.Save();
        }

        public void SetPlayMusic(bool playMusic)
        {
            Settings.BackgroundMusic = playMusic;
            Settings.Save();
        }

        public void SendInjectorSettings(Uri uri)
        {
            Program.Logger.WriteLine($"Sending information to injector ({uri})...");
            var channel = IPCUtil.CreateChannel<IInjectorService>(Settings.IPCType, uri.ToString());
            if (Program.OverridePublicKey)
            {
                channel.SetDonationKey(Program.GenerateDonationKey());
                channel.SetPublicKey(Program.PublicKey);
            }
            if (Settings.TitleKeys.Count == 0)
            {
                channel.ForceKeySiteForm();
            }
            channel.TrustCertificateAuthority(CertMaker.GetRootCertificate().GetRawCertData());
            channel.SetProxy(Program.Proxy.GetWebProxy().Address.ToString());
            channel.SetDownloaderMaxRetries(Settings.MaxRetries);
            channel.SetDownloaderRetryDelay(Settings.DelayBetweenRetries);
            channel.SetDisableOptionalPatches(Settings.DisableOptionalPatches);
            channel.SetDisableTabs(Settings.DisableTabs);
            channel.SetLocaleFile(Program.Locale.LocaleFile);
            channel.SetEshopRegion(Program.Locale.ChosenLocale.Split('-')[1]);
            channel.SetDefaultFont(Settings.DefaultFont);
            channel.SetHelperVersion(Program.HelperVersion);
            channel.SetPortable(Settings.Portable);
            channel.SetForceHttp(Settings.ForceHttp);
            channel.SetFunAllowed(!Settings.NoFunAllowed);
            channel.SetPlayMusic(Settings.BackgroundMusic);
            channel.SetSplitUnpackDirectories(Settings.SplitUnpackDirectories);
            channel.SetWineCompat(Program.WineCompat);
        }
    }
}
