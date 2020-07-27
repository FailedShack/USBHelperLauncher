
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    class CommandLineArgsPatch
    {
        static MethodBase TargetMethod()
        {
            return ReflectionHelper.EntryPoint;
        }

        static void Prefix(ref string[] __0)
        {
            __0 = __0.Take(1).ToArray();
        }
    }
}
