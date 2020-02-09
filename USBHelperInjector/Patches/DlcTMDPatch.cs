using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    class DlcTMDPatch
    {
        private static object CatchBlock(Exception e)
        {
            return 2;
        }

        static MethodBase TargetMethod()
        {
            return (from type in ReflectionHelper.Types
                    where type.IsAbstract
                    && type.GetProperty("RootDownloadLocation", BindingFlags.NonPublic | BindingFlags.Instance) != null
                    from method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    where method.ReturnType.IsEnum
                    select method).FirstOrDefault();
        }

        static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();

            // local variable for storing return value before leaving protected region
            var localReturnValue = generator.DeclareLocal((original as MethodInfo).ReturnType);

            // label for jumping out of protected region
            var leaveLabel = generator.DefineLabel();


            // catch block instructions
            var catchBlock = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DlcTMDPatch), "CatchBlock")),
                new CodeInstruction(OpCodes.Ret)
            };
            codes.AddRange(catchBlock);

            // add exception block
            codes[0].blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock, null));
            catchBlock.First().blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(Exception)));
            catchBlock.Last().blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock, null));

            // replace 'ret' with 'leave <label>'
            List<int> leaveIndexes = new List<int>();
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ret)
                {
                    codes[i].opcode = OpCodes.Leave;
                    codes[i].operand = leaveLabel;
                    leaveIndexes.Add(i);
                }
            }
            // prepend 'stloc.s <local>' to every 'leave' from the previous step
            leaveIndexes.Reverse();
            foreach (var index in leaveIndexes)
            {
                codes.Insert(index, new CodeInstruction(OpCodes.Stloc_S, localReturnValue));
            }

            // add 'ldloc.s <local>; ret' instructions
            var ldlocInstruction = new CodeInstruction(OpCodes.Ldloc_S, localReturnValue);
            ldlocInstruction.labels.Add(leaveLabel);
            var retInstruction = new CodeInstruction(OpCodes.Ret);
            codes.Add(ldlocInstruction);
            codes.Add(retInstruction);

            return codes;
        }
    }
}
