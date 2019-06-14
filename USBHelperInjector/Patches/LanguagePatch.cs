using Harmony;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace USBHelperInjector.Patches
{
    [Optional]
    [HarmonyPatch]
    class LanguagePatch
    {
        static MethodBase TargetMethod()
        {
            // v0.6.1.655: GClass32.Class43.method_0
            return (from type in ReflectionHelper.Types
                    where type.GetProperty("Dlc", BindingFlags.Public | BindingFlags.Instance) != null
                    from nested in type.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance)
                    where nested.GetFields(BindingFlags.Public | BindingFlags.Instance).Any(p => p.FieldType == type)
                    from method in nested.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    select method).FirstOrDefault();
        }

        static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            var cultureGetter = typeof(CultureInfo).GetProperty("CurrentCulture").GetGetMethod();
            var uiCultureGetter = typeof(CultureInfo).GetProperty("CurrentUICulture").GetGetMethod();

            var index = codes.FindIndex(i => i.opcode == OpCodes.Call && ((MethodBase)i.operand) == cultureGetter);
            if (index != -1)
            {
                codes[index].operand = uiCultureGetter;
            }

            return codes;
        }
    }
}
