using HarmonyLib;
using System.Reflection;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    class ForceKeySiteFormPatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.DeclaredProperty(ReflectionHelper.Settings.Type, "TicketsPath").GetGetMethod(true);
        }

        static bool Prefix(ref string __result)
        {
            if (Overrides.ForceKeySiteForm)
            {
                __result = string.Empty;
                return false;
            }
            return true;
        }
    }
}
