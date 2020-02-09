using HarmonyLib;
using System.Reflection;
using System.Windows.Forms;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    class NusGrabberFormTaskbarPatch
    {
        static MethodBase TargetMethod()
        {
            return ReflectionHelper.NusGrabberForm.Constructor;
        }

        static void Postfix(Form __instance)
        {
            __instance.Load += (sender, e) =>
            {
                __instance.ShowInTaskbar = false;
                __instance.ShowInTaskbar = true;
            };
        }
    }
}
