using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using USBHelperInjector.Patches.Attributes;

namespace USBHelperInjector.Patches
{
    [Optional]
    [HarmonyPatch]
    class DownloaderQueuePatch
    {
        static MethodBase TargetMethod()
        {
            byte[] il = DownloaderPatch.TargetMethod().GetMethodBody().GetILAsByteArray();
            return ReflectionHelper.MainModule.ResolveMethod(BitConverter.ToInt32(il, 35));
        }

        // Instead of aborting the queue, this patch makes it skip the current title
        static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var type = original.DeclaringType;
            var list = (from field in type.GetFields()
                        where field.FieldType.IsGenericType
                        select field).FirstOrDefault();
            var code = codes[147];
            // Safety check
            if (code.opcode != OpCodes.Ldarg_0)
            {
                return codes;
            }
            codes.RemoveRange(148, 8);
            var toInsert = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldfld, list),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(list.FieldType, "RemoveAt", new Type[] { typeof(int) })),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stloc_1)
            };
            codes.InsertRange(148, toInsert);
            return codes;
        }
    }
}
