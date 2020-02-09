using HarmonyLib;
using System.Linq;
using System.Reflection;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    class PublicKeyPatch
    {
        static MethodBase TargetMethod()
        {
            return (from type in ReflectionHelper.MainModule.GetTypes()
                    from prop in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Static)
                    where prop.Name == "keysPub"
                    select prop).FirstOrDefault().GetGetMethod(true);
        }

        static bool Prefix(ref string __result)
        {
            if (Overrides.PublicKey != null)
            {
                __result = Overrides.PublicKey;
                return false;
            }
            return true;
        }
    }
}
