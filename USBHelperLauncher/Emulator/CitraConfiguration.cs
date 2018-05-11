using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using USBHelperLauncher.Utils;

namespace USBHelperLauncher.Emulator
{
    class CitraConfiguration : EmulatorConfiguration
    {
        public CitraConfiguration() : base("Citra")
        {
            versions.Add("Nightly", GetNightlyPackage);
            versions.Add("Canary", GetCanaryPackage);
        }

        public async Task<Package> GetNightlyPackage()
        {
            return await GetPackageAsync("nightly");
        }

        public async Task<Package> GetCanaryPackage()
        {
            return await GetPackageAsync("canary");
        }

        public async Task<Package> GetPackageAsync(string version)
        {
            JObject release = await GithubUtil.GetRelease("citra-emu", "citra-" + version, "latest");
            Package package = null;
            foreach (JToken asset in release["assets"].Children())
            {
                string dlUrl = (string)asset["browser_download_url"];
                Uri uri = new Uri(dlUrl);
                string fileName = Path.GetFileName(uri.LocalPath);
                Match match = Regex.Match(fileName, @"citra-windows-(.+?)-\d+-(.+?).zip");
                if (match.Success)
                {
                    string compiler = match.Groups[1].Value;
                    package = new Package(uri, GetName(), match.Groups[2].Value);
                    package.SetMeta("Compiler", compiler);
                    // Let's take mingw if we can, it's faster
                    if (compiler == "mingw")
                    {
                        break;
                    }
                }
            }
            return package;
        }
    }
}
