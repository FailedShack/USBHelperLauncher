using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using USBHelperInjector.Patches.Attributes;

namespace USBHelperInjector.Patches
{
    [Optional]
    [HarmonyPatch]
    class LanguagePatch
    {
        static MethodBase TargetMethod()
        {
            // v0.6.1.655: GClass32.Class43.method_0
            return (from nested in ReflectionHelper.TitleTypes.Game.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance)
                    where nested.GetFields(BindingFlags.Public | BindingFlags.Instance).Any(p => p.FieldType == ReflectionHelper.TitleTypes.Game)
                    from method in nested.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    select method).FirstOrDefault();
        }

        static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var index = codes.FindIndex(i => i.opcode == OpCodes.Ldloc_0);
            var getRegion = codes.Find(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "get_Region");
            var toInsert = codes.GetRange(index - 2, 2); // ldarg.0, ldfld {title}

            // Skip from first check to title data method call
            codes.RemoveRange(4, index - 6);
            toInsert.ForEach(x => x.labels.Clear());
            toInsert.AddRange(new List<CodeInstruction>
            {
                getRegion,
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LanguagePatch), nameof(GetEshopRegion))),
                new CodeInstruction(OpCodes.Stloc_0)
            });
            codes.InsertRange(4, toInsert);

            return codes;
        }

        static string GetEshopRegion(string region)
        {
            // Keep Japanese and Korean title regions as title IDs differ
            if (region == "JPN")
                return "JP";
            if (region == "KOR")
                return "KR";

            return InjectorService.EshopRegion;
        }
    }
}
