using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using USBHelperInjector.Patches.Attributes;

namespace USBHelperInjector.Patches
{
    [Optional]
    [HarmonyPatch]
    class DownloadManagerOverlayPatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.DeclaredProperty(ReflectionHelper.Settings, "ShowDownloadManagerTip").GetGetMethod(true);
        }

        static bool Prefix(ref bool __result)
        {
            // Check if RivaTuner hooks are loaded into the process; RivaTuner can sometimes break d3d9
            var modules = Process.GetCurrentProcess().Modules.Cast<ProcessModule>();
            if (modules.Any(m => m.ModuleName.ToLowerInvariant().StartsWith("rtsshooks")))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
