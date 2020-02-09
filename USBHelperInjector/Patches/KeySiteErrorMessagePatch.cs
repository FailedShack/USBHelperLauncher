using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using USBHelperInjector.Patches.Attributes;

namespace USBHelperInjector.Patches
{
    [Optional]
    [HarmonyPatch]
    class KeySiteErrorMessagePatch
    {
        static MethodBase TargetMethod()
        {
            return (from method in ReflectionHelper.NusGrabberForm.Type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
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

                codes.RemoveAt(index);
                codes.InsertRange(index, new List<CodeInstruction>
                {
                    // Override error message
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Ldloc_S, localException),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldstr, message),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(KeySiteErrorMessagePatch), "FormatExceptionMessage"))
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

        // Called from the transpiler using reflection
        static string FormatExceptionMessage(Exception exception, string url, string postfix)
        {
            string exceptionMessage;
            if (exception is WebException webEx && webEx.Status == WebExceptionStatus.ProtocolError)
            {
                var response = (HttpWebResponse)webEx.Response;
                exceptionMessage = KeySiteFormValidationPatch.GetCustomHttpErrorMessage((int)response.StatusCode, response.StatusDescription);
            }
            else
            {
                exceptionMessage = exception.ToString();
            }

            var urlType = url.Contains("wiiu") ? "WiiU" : "3DS";
            return string.Format("An error occurred while trying to retrieve the title keys for {0}:\n\n{1}\n\n{2}", urlType, exceptionMessage, postfix);
        }
    }
}
