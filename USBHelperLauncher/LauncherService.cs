using Fiddler;
using System;
using System.Net;
using System.ServiceModel;
using System.Text;
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

        public string SetCustomProxy(string address, string username, string password)
        {
            string authString = null;
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                authString = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", username, password)));
            }

            string error = null;
            if (!string.IsNullOrEmpty(address))
            {
                int index = address.IndexOf("://");
                if (index != -1)
                {
                    string originalAddress = address;
                    string scheme = address.Substring(0, index);
                    address = address.Substring(index + 3); // remove "<scheme>://" from address
                    if (scheme.StartsWith("socks"))
                    {
                        address = "socks=" + address;
                    }
                    Program.GetLogger().WriteLine("Rewrote proxy address \"{0}\" to \"{1}\"", originalAddress, address);
                }

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://example.com");
                request.Proxy = Program.GetProxy().GetWebProxy();
                request.Timeout = 2000;
                request.Method = "GET";
                request.Headers["X-FiddlerCustomProxy"] = address;
                request.Headers["Proxy-Authorization"] = authString;
                try
                {
                    request.GetResponse();
                }
                catch (Exception e)
                {
                    error = e.Message;
                }
            }

            Program.GetProxy().CustomProxyAddress = error == null ? address : null;
            Program.GetProxy().CustomProxyAuthorization = error == null ? authString : null;

            if (error == null)
            {
                Program.GetLogger().WriteLine("Set custom proxy to \"{0}\"", address);
            }
            else
            {
                Program.GetLogger().WriteLine("Could not set custom proxy to \"{0}\" (error: {1})", address, error);
            }

            return error;
        }

        public void SendInjectorSettings()
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
        }
    }
}
