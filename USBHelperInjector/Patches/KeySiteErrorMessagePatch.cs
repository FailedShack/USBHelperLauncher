using Harmony;
using Harmony.ILCopying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace USBHelperInjector.Patches
{
    [Optional]
    [HarmonyPatch]
    class KeySiteErrorMessagePatch
    {
        static MethodBase TargetMethod()
        {
            return (from method in ReflectionHelper.NusGrabberForm.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    where method.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(string) })
                    && method.ReturnType == typeof(byte[])
                    select method).FirstOrDefault();
        }

        static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();

            var localException = generator.DeclareLocal(typeof(Exception));

            var stringFormatIndices = new List<int>();
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call && ((MethodInfo)codes[i].operand) == AccessTools.Method(typeof(string), "Format", new[] { typeof(string), typeof(object) }))
                {
                    stringFormatIndices.Add(i);
                }
            }
            if (stringFormatIndices.Count != 2)
            {
                return codes;
            }

            stringFormatIndices.Reverse();
            foreach (var index in stringFormatIndices)
            {
                var readAllBytesIndex = codes.FindIndex(index, 10, i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand) == AccessTools.Method(typeof(System.IO.File), "ReadAllBytes", new[] { typeof(string) }));
                string message = readAllBytesIndex == -1
                    ? "No backup was found. Unfortunately the app cannot work without it."
                    : "Wii U USB Helper will now try to use a backup version. Please note that some features might not work as expected.";

                var labelLoadWiiU = generator.DefineLabel();
                var labelContinueArgs = generator.DefineLabel();

                codes[index].operand = AccessTools.Method(typeof(string), "Format", new[] { typeof(string), typeof(object), typeof(object) });
                codes.InsertRange(index, new List<CodeInstruction>
                {
                    // Override format string
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Ldstr, "An error occurred while trying to retrieve the title keys for {0}:\n\n{1}\n\n" + message),

                    // Use "WiiU" or "3DS" as the first format argument
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldstr, "wiiu"),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(string), "Contains")),
                    new CodeInstruction(OpCodes.Brtrue_S, labelLoadWiiU),
                    new CodeInstruction(OpCodes.Ldstr, "3DS"),
                    new CodeInstruction(OpCodes.Br_S, labelContinueArgs),
                    new CodeInstruction(OpCodes.Ldstr, "WiiU")
                    {
                        labels = new List<Label> { labelLoadWiiU }
                    },

                    // Use the exception as the second format argument
                    new CodeInstruction(OpCodes.Ldloc_S, localException)
                    {
                        labels = new List<Label> { labelContinueArgs }
                    }
                });

                // Exit if no backup is available
                if (readAllBytesIndex == -1)
                {
                    var leaveIndex = codes.FindIndex(index, i => i.opcode == OpCodes.Leave || i.opcode == OpCodes.Leave_S);
                    codes.Insert(leaveIndex, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(System.Windows.Forms.Application), "Exit")));
                }
            }

            // Keep exception at the start of the catch block
            var catchBlockStart = codes.FindLastIndex(stringFormatIndices[0], i => i.blocks.Any(block => block.blockType == ExceptionBlockType.BeginCatchBlock));
            var originalCatchInstr = codes[catchBlockStart];
            var copiedCatchInstr = new CodeInstruction(originalCatchInstr.opcode, originalCatchInstr.operand);

            // Keep labels+blocks, just change operation
            originalCatchInstr.opcode = OpCodes.Dup;
            originalCatchInstr.operand = null;

            var catchBlockInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Stloc_S, localException),
                copiedCatchInstr
            };
            codes.InsertRange(catchBlockStart + 1, catchBlockInstructions);

            return codes;
        }
    }
}
