using Fiddler;
using System.ServiceModel;
using System.Threading.Tasks;
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

        public Task SendInjectorSettings()
        {
            Program.GetLogger().WriteLine("Sending information to injector...");
            var factory = new ChannelFactory<IInjectorService>(new NetNamedPipeBinding(), "net.pipe://localhost/InjectorService");
            var channel = factory.CreateChannel();
            if (Program.PatchPublicKey)
            {
                channel.SetDonationKey(Program.GenerateDonationKey());
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
            return Task.FromResult(0);
        }
    }
}
