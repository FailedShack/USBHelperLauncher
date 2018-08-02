using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USBHelperLauncher.Utils;

namespace USBHelperLauncher.Emulator
{
    class FceuxConfiguration : EmulatorConfiguration
    {
        public FceuxConfiguration() : base("FCEUX")
        {
            versions.Add("Stable", GetPackageAsync);
        }

        public async Task<Package> GetPackageAsync()
        {
            JToken release = await SourceforgeUtil.GetLatestRelease("fceultra", "windows");
            string dlUrl = (string)release["url"];
            string filename = (string)release["filename"];
            Uri uri = new Uri(dlUrl);
            return new ZipPackage(uri, GetName(), filename.Split('/')[2]);
        }
    }
}
