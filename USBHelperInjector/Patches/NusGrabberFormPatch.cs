using HarmonyLib;
using System.Reflection;
using System.Windows.Forms;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    class NusGrabberFormPatch
    {
        public static event FormClosingEventHandler FormClosing;

        static MethodBase TargetMethod()
        {
            return ReflectionHelper.NusGrabberForm.Constructor;
        }

        static void Postfix(Form __instance)
        {
            // Fixes window missing from taskbar when UseShellExecute = false
            __instance.Load += (sender, e) =>
            {
                __instance.ShowInTaskbar = false;
                __instance.ShowInTaskbar = true;
            };
            __instance.FormClosing += (sender, e) => FormClosing?.Invoke(sender, e);
        }
    }
}
