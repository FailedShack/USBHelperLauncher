using HarmonyLib;
using System.Reflection;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    class KeySite3DSSettingsPatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.DeclaredProperty(ReflectionHelper.Settings.Type, "TicketsPath3DS").GetGetMethod(true);
        }

        static bool Prefix(object __instance, ref string __result)
        {
            var hasWiiUTitleKeys = !string.IsNullOrEmpty(ReflectionHelper.Settings.GetValue<string>("TicketsPath", __instance));
            __result = hasWiiUTitleKeys ? "http://3ds.titlekeys.gq/" : string.Empty;
            return false;
        }
    }
}
