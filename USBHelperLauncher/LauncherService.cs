using Fiddler;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using USBHelperInjector.Contracts;
using USBHelperLauncher.Configuration;

namespace USBHelperLauncher
{
    class LauncherService : ILauncherService
    {
        public void SetKeySite(string site, string url)
        {
            if (Settings.TitleKeys == null)
            {
                Settings.TitleKeys = new Dictionary<string, string>();
            }
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
            channel.TrustCertificateAuthority(CertMaker.GetRootCertificate().GetRawCertData());
            channel.SetProxy(Program.GetProxy().GetWebProxy().Address.ToString());
            channel.SetDownloaderMaxRetries(Settings.MaxRetries);
            channel.SetDownloaderRetryDelay(Settings.DelayBetweenRetries);
            channel.ApplyPatches(Settings.DisableOptionalPatches);
            return Task.FromResult(0);
        }
    }
}
