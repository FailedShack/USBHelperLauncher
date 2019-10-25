using Fiddler;
using System.ServiceModel;
using USBHelperInjector.Contracts;
using USBHelperLauncher.Configuration;

namespace USBHelperLauncher
{
    class LauncherService : ILauncherService
    {
        public void SetKeySite(string site, string url)
        {
            Settings.TitleKeys[site] = url;
            Settings.Save();
        }

        public void SendInjectorSettings()
        {
            Program.GetLogger().WriteLine("Sending information to injector...");
            var factory = new ChannelFactory<IInjectorService>(new NetNamedPipeBinding(), "net.pipe://localhost/InjectorService");
            var channel = factory.CreateChannel();
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
            channel.SetProxy(Program.GetProxy().GetWebProxy().Address.ToString());
            channel.SetDownloaderMaxRetries(Settings.MaxRetries);
            channel.SetDownloaderRetryDelay(Settings.DelayBetweenRetries);
            channel.SetDisableOptionalPatches(Settings.DisableOptionalPatches);
            channel.SetDisableWebSearchTab(Settings.DisableWebSearchTab);
            channel.SetHelperVersion(Program.GetHelperVersion());
            channel.SetPortable(Settings.Portable);
        }
    }
}
