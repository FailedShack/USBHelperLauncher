using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace USBHelperLauncher.Emulator
{
    class DolphinConfiguration : EmulatorConfiguration
    {
        public DolphinConfiguration() : base("Dolphin")
        {
            versions.Add("Stable", GetPackageAsync);
        }

        public async Task<Package> GetPackageAsync()
        {
            string html;
            using (WebClient client = new WebClient())
            {
                try
                {
                    html = await client.DownloadStringTaskAsync("https://dolphin-emu.org/download/");
                }
                catch (WebException)
                {
                    throw;
                }
            }
            Match mUrl = Regex.Match(html, "href=\"(.*dolphin-master-(.*)-x64\\.7z)\"");
            return new SevenZipPackage(new Uri(mUrl.Groups[1].Value), GetName(), mUrl.Groups[2].Value);
        }
    }
}
