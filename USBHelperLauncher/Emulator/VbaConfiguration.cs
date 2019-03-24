using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using USBHelperLauncher.Utils;

namespace USBHelperLauncher.Emulator
{
    class VbaConfiguration : EmulatorConfiguration
    {
        public VbaConfiguration() : base("VisualBoyAdvance")
        {
            versions.Add("Stable", GetPackageAsync);
        }

        public async Task<Package> GetPackageAsync()
        {
            JToken release = await SourceforgeUtil.GetLatestRelease("vba", "windows");
            string dlUrl = (string)release["url"];
            string filename = (string)release["filename"];
            Uri uri = new Uri(dlUrl);
            Package package = new ZipPackage(uri, GetName(), filename.Split('/')[2]);
            package.PostUnpack += delegate (DirectoryInfo dir)
            {
                // Wii U USB Helper expects it to be named simply 'VisualBoyAdvance.exe'
                string from = Path.Combine(dir.FullName, "VisualBoyAdvance-SDL.exe");
                string to = Path.Combine(dir.FullName, "VisualBoyAdvance.exe");
                File.Move(from, to);
            };
            return package;
        }
    }
}
