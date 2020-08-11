using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows.Forms;
using USBHelperInjector.Patches.Attributes;

namespace USBHelperInjector.Patches
{
    [Optional]
    [HarmonyPatch]
    class Disable3DSDownloadMain
    {
        internal static MethodBase TargetMethod()
        {
            var shwGetter = ReflectionHelper.Settings.GetProperty("ShowHaxchiWarning").GetGetMethod();
            return (from method in ReflectionHelper.NusGrabberForm.Type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    where method.GetParameters().Length == 1
                    && method.GetParameters()[0].ParameterType.IsAbstract
                    let instructions = PatchProcessor.GetOriginalInstructions(method, out _)
                    where instructions.Any(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand) == shwGetter)
                    select method).FirstOrDefault();
        }

        internal static bool Prefix(object __0)
        {
            bool is3DSTitle = Is3DSTitle(__0);
            if (is3DSTitle)
                ShowWarning();
            return !is3DSTitle;
        }

        internal static bool Is3DSTitle(object obj)
        {
            var prop = obj.GetType().GetProperty("System");
            if (prop == null)
                return false;
            return (int)prop.GetValue(obj) == 0;
        }

        internal static void ShowWarning()
        {
            MessageBox.Show("Downloading 3DS titles is not possible anymore, as Nintendo patched the 3DS eShop servers and implemented additional security measures.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Additional patch suppressing the "<game> has an update/DLC you have not downloaded yet..." dialogs
    [Optional]
    [HarmonyPatch]
    class Disable3DSDownloadDialog
    {
        static MethodBase TargetMethod()
        {
            return (from method in ReflectionHelper.NusGrabberForm.Type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    where method.GetParameters().Length == 1
                    && method.GetParameters()[0].ParameterType == ReflectionHelper.TitleTypes.Game
                    && method.GetMethodBody().LocalVariables.Count == 0
                    && !method.IsSpecialName
                    select method).FirstOrDefault();
        }

        static bool Prefix(object __0)
        {
            return Disable3DSDownloadMain.Prefix(__0);
        }
    }

    // Prevent USB Helper from getting stuck if the user tries to play a game that hasn't been downloaded yet
    [Optional]
    [HarmonyPatch]
    class Disable3DSDownloadPlayQueue
    {
        static MethodBase TargetMethod()
        {
            return ReflectionHelper.NusGrabberForm.Methods.PlayGame;
        }

        static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();

            var branchIndex = codes.FindIndex(i => i.opcode == OpCodes.Beq);
            var continueLabel = generator.DefineLabel();
            codes[branchIndex + 1].labels.Add(continueLabel);

            var checkTitle = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Disable3DSDownloadMain), "Is3DSTitle")),
                new CodeInstruction(OpCodes.Brfalse_S, continueLabel),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Disable3DSDownloadMain), "ShowWarning")),
                new CodeInstruction(OpCodes.Ret)
            };
            codes.InsertRange(branchIndex + 1, checkTitle);

            return codes;
        }
    }

    // Disable System Archive downloads
    [Optional]
    [HarmonyPatch]
    class SharedFontPatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(ReflectionHelper.MainModule.GetType("NusHelper.Emulators.Citra"), "DownloadSharedFont");
        }

        static bool Prefix()
        {
            return false;
        }
    }

    [Optional]
    [HarmonyPatch]
    class DownloadArchivePatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(ReflectionHelper.MainModule.GetType("NusHelper.Emulators.Citra"), "DownloadArchive");
        }

        static bool Prefix()
        {
            return false;
        }
    }
}
