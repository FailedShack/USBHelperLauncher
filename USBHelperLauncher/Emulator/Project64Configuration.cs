using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace USBHelperLauncher.Emulator
{
    class Project64Configuration : EmulatorConfiguration
    {
        private const string Url = "https://www.pj64-emu.com";

        public Project64Configuration() : base("Project64")
        {
            versions.Add("Stable", GetPackageAsync);
        }

        public async Task<Package> GetPackageAsync()
        {
            string html;
            using (WebClient client = new WebClient())
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                client.Headers.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2;)");
                try
                {
                    html = await client.DownloadStringTaskAsync(Url + "/download/project64-latest");
                }
                catch (WebException)
                {
                    throw;
                }
            }
            Match mUrl = Regex.Match(html, @"window\.location = '(.*)'");
            Match mVersion = Regex.Match(html, @"Project64 (v\d\.\d\.\d)");
            return new InnoPackage(new Uri(Url + mUrl.Groups[1].Value), GetName(), mVersion.Groups[1].Value);
        }
    }
}
