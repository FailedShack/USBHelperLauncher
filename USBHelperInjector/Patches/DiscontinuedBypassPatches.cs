using Harmony;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using USBHelperInjector.Patches.Attributes;

namespace USBHelperInjector.Patches
{
    [VersionSpecific("0.6.1.653")]
    [HarmonyPatch]
    class DiscontinuedBypass653
    {
        static MethodBase TargetMethod()
        {
            return ReflectionHelper.EntryPoint;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.Take(212).Concat(instructions.Skip(212 + 62));
        }
    }

    [VersionSpecific("0.6.1.654", "0.6.1.655")]
    [HarmonyPatch]
    class DiscontinuedBypass654_655
    {
        static MethodBase TargetMethod()
        {
            return (from method in ReflectionHelper.EntryPoint.DeclaringType.GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                    where method.ReturnType == typeof(bool) select method).FirstOrDefault();
        }

        static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }
}
