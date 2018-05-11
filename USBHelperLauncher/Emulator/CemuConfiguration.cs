using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using USBHelperLauncher.Utils;

namespace USBHelperLauncher.Emulator
{
    class CemuConfiguration : EmulatorConfiguration
    {
        public CemuConfiguration() : base("Cemu")
        {
            versions.Add("Stable", GetPackageAsync);
            extensions.Add(GetCemuHookPackageAsync);
            extensions.Add(GetGraphicPacksPackageAsync);
        }

        public async Task<Package> GetPackageAsync()
        {
            string html;
            using (WebClient client = new WebClient())
            {
                try
                {
                    html = await client.DownloadStringTaskAsync("http://cemu.info/");
                }
                catch (WebException)
                {
                    throw;
                }
            }
            Match mUrl = Regex.Match(html, "<a href=\"(.+?)\".+?name=\"download\"");
            string url = mUrl.Groups[1].Value;
            Match mVersion = Regex.Match(url, @"cemu_(.+?)\.zip");
            return new Package(new Uri(url), GetName(), mVersion.Groups[1].Value);
        }

        public async Task<Package> GetCemuHookPackageAsync()
        {
            string html;
            using (WebClient client = new WebClient())
            {
                try
                {
                    html = await client.DownloadStringTaskAsync("https://cemuhook.sshnuke.net/");
                }
                catch (WebException)
                {
                    throw;
                }
            }
            MatchCollection matches = Regex.Matches(html, "href=\"(.*?)cemuhook_(\\d+[a-z]?)_(\\d{4})\\.zip");
            string latest = null;
            Match latestMatch = null;
            foreach (Match match in matches)
            {
                string version = match.Groups[3].Value;
                if (latest == null || version.CompareTo(latest) > 0)
                {
                    latest = version;
                    latestMatch = match;
                }
            }
            if (latestMatch == null)
            {
                return null;
            }
            string cemuVersion = latestMatch.Groups[2].Value;
            string url = latestMatch.Groups[1].Value + "cemuhook_" + cemuVersion + "_" + latest + ".zip";
            Package package = new Package(new Uri(url), "CemuHook", string.Join(".", latest.ToCharArray()));
            package.SetMeta("CemuVersion", ParseCemuVersion(cemuVersion));
            return package;
        }

        public async Task<Package> GetGraphicPacksPackageAsync()
        {
            JObject release = await GithubUtil.GetRelease("slashiee", "cemu_graphic_packs", "latest");
            string version = "v2-" + ((string)release["tag_name"]).Replace("appveyor", "");
            string dlUrl = (string)release["assets"][0]["browser_download_url"];
            return new Package(new Uri(dlUrl), "Graphic Packs", version, "graphicPacks");
        }

        private string ParseCemuVersion(string version)
        {
            Match match = Regex.Match(version, @"(\d)(\d+)(\d)([a-z]?)");
            return match.Groups[1].Value + "." + match.Groups[2].Value + "." + match.Groups[3].Value + match.Groups[4].Value;
        }
    }
}
