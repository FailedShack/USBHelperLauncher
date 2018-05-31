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
            return await CitraRepoUtil.GetPackageAsync("nightly", "mingw");
        }

        public async Task<Package> GetCanaryPackage()
        {
            return await CitraRepoUtil.GetPackageAsync("canary", "mingw");
        }
    }
}
