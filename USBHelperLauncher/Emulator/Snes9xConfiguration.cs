using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USBHelperLauncher.Utils;

namespace USBHelperLauncher.Emulator
{
    class Snes9xConfiguration : EmulatorConfiguration
    {
        public Snes9xConfiguration() : base("Snes9x")
        {
            versions.Add("Stable", GetPackageAsync);
        }

        public async Task<Package> GetPackageAsync()
        {
            JObject release = await GithubUtil.GetRelease("snes9xgit", "snes9x", "latest");
            Package package = null;
            foreach (JToken asset in release["assets"].Children())
            {
                string dlUrl = (string)asset["browser_download_url"];
                Uri uri = new Uri(dlUrl);
                string fileName = Path.GetFileName(uri.LocalPath);
                if (fileName.Contains("win32"))
                {
                    package = new ZipPackage(uri, GetName(), (string)release["tag_name"]);
                    if (fileName.Contains("x64") && Environment.Is64BitOperatingSystem)
                    {
                        package.PostUnpack += delegate (DirectoryInfo dir)
                        {
                            // Wii U USB Helper expects it to be named simply 'snes9x.exe'
                            string from = Path.Combine(dir.FullName, "snes9x-x64.exe");
                            string to = Path.Combine(dir.FullName, "snes9x.exe");
                            File.Move(from, to);
                        };
                        break;
                    }
                }
            }
            return package;
        }
    }
}
