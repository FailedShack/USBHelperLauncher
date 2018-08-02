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
    class DesmumeConfiguration : EmulatorConfiguration
    {
        public DesmumeConfiguration() : base("DeSmuME")
        {
            versions.Add("Stable", GetPackageAsync);
        }

        public async Task<Package> GetPackageAsync()
        {
            JToken release = await SourceforgeUtil.GetLatestRelease("desmume", "windows");
            string dlUrl = (string)release["url"];
            string filename = (string)release["filename"];
            string version = filename.Split('/')[2];
            string arch = "x86";
            if (Environment.Is64BitOperatingSystem)
            {
                dlUrl = dlUrl.Replace("win32", "win64");
                arch = "x64";
            }
            Uri uri = new Uri(dlUrl);
            Package package = new ZipPackage(uri, GetName(), version);
            package.PostUnpack += delegate (DirectoryInfo dir)
            {
                // Wii U USB Helper expects it to be named simply 'DeSmuME.exe'
                string from = Path.Combine(dir.FullName, string.Format("DeSmuME_{0}_{1}.exe", version, arch));
                string to = Path.Combine(dir.FullName, "DeSmuME.exe");
                File.Move(from, to);
            };
            return package;
        }
    }
}
