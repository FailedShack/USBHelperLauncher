using Harmony;
using System.Reflection;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    internal class SettingsDonationKeyGet
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.DeclaredProperty(ReflectionHelper.Settings, "DonationKey").GetGetMethod(true);
        }

        static bool Prefix(ref string __result)
        {
            if (Overrides.DonationKey != null)
            {
                __result = Overrides.DonationKey;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch]
    internal class SettingsDonationKeySet
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.DeclaredProperty(ReflectionHelper.Settings, "DonationKey").GetSetMethod(true);
        }

        static bool Prefix(ref string value)
        {
            Overrides.RaiseDonationKeyChangeEvent(value);
            return true;
        }
    }
}
