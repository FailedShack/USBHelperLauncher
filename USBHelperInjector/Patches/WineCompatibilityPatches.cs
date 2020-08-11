using System.Reflection;
using System.Windows.Forms;
using HarmonyLib;
using USBHelperInjector.Patches.Attributes;

namespace USBHelperInjector.Patches
{
    [WineOnly]
    [HarmonyPatch]
    class WineDisableDownloadManagerOverlay
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.DeclaredProperty(ReflectionHelper.Settings, "ShowDownloadManagerTip").GetGetMethod(true);
        }

        static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    [WineOnly]
    [HarmonyPatch]
    class WineDisablePlayGame
    {
        static MethodBase TargetMethod()
        {
            return ReflectionHelper.NusGrabberForm.Methods.PlayGame;
        }

        static bool Prefix()
        {
            MessageBox.Show(
                "Starting games directly from USB Helper is not supported in Wine",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            return false;
        }
    }
}
