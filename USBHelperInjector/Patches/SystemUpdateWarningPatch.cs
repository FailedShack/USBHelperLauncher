using HarmonyLib;
using System.Reflection;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    class SystemUpdateWarningPatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.DeclaredProperty(ReflectionHelper.Settings.Type, "Show552Warning").GetGetMethod(true);
        }

        static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }
}
