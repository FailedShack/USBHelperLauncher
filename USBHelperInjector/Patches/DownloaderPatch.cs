using Harmony;
using System.Linq;
using System.Reflection;

namespace USBHelperInjector.Patches
{
    // Allows us to override the number of tries
    // and time between tries of the downloader.
    [Optional]
    [HarmonyPatch]
    internal class DownloaderPatch
    {
        internal static MethodBase TargetMethod()
        {
            return (from type in ReflectionHelper.Types
                    from prop in type.GetProperties()
                    where prop.Name == "MaxSpeed"
                    from method in prop.DeclaringType.GetMethods()
                    where method.GetParameters().Length == 3
                    select method).FirstOrDefault();
        }

        static bool Prefix(ref int __1, ref int __2)
        {
            __1 = Overrides.MaxRetries;
            __2 = Overrides.DelayBetweenRetries;
            return true;
        }
    }
}
