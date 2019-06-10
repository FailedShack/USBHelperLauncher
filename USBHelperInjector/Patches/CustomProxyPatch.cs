using Harmony;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;

namespace USBHelperInjector.Patches
{
    [Optional]
    [HarmonyPatch]
    class CustomProxyPatch
    {
        static MethodBase TargetMethod()
        {
            // v0.6.1.655: NusGrabberForm.cmdSetProxy_Click
            return (from method in ReflectionHelper.NusGrabberForm.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    where method.ReturnType == typeof(void) && method.GetParameters().Length == 2
                    && method.GetMethodBody().LocalVariables.Any(l => l.LocalType == typeof(NetworkCredential))
                    select method).FirstOrDefault();
        }

        static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            // Fix password field
            int indexLdFldPassword = codes.FindIndex(i => i.opcode == OpCodes.Newobj && ((MethodBase)i.operand) == typeof(NetworkCredential).GetConstructor(new[] { typeof(string), typeof(string) }));
            indexLdFldPassword = codes.FindLastIndex(indexLdFldPassword, i => i.opcode == OpCodes.Ldfld);
            codes[indexLdFldPassword] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomProxyPatch), "GetPasswordBox"));

            // Fix credentials
            int indexDefaultCredentials = codes.FindIndex(i => i.opcode == OpCodes.Callvirt && ((MethodBase)i.operand) == typeof(WebProxy).GetProperty("UseDefaultCredentials").GetSetMethod());
            codes[indexDefaultCredentials] = new CodeInstruction(OpCodes.Pop);


            // Remove proxy availability check
            int indexWebRequestCreate = codes.FindIndex(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand) == typeof(WebRequest).GetMethod("Create", new[] { typeof(string) }));
            indexWebRequestCreate = codes.FindLastIndex(indexWebRequestCreate, i => i.opcode == OpCodes.Ldc_I4);
            int indexEndFinally = codes.FindIndex(i => i.opcode == OpCodes.Endfinally);

            int indexSetProxyEnd = codes.FindIndex(i => i.opcode == OpCodes.Call && ((MethodBase)i.operand) == SettingsProxySet.TargetMethod());
            int indexSetProxyStart = codes.FindLastIndex(indexSetProxyEnd, i => i.opcode == OpCodes.Ldarg_0);

            var setProxyInstructions = codes.GetRange(indexSetProxyStart, indexSetProxyEnd - indexSetProxyStart + 1);
            setProxyInstructions[0].blocks.Clear();

            codes.RemoveRange(indexWebRequestCreate, indexEndFinally - indexWebRequestCreate + 1);
            codes.InsertRange(indexWebRequestCreate, setProxyInstructions);

            return codes;
        }

        private static object GetPasswordBox(object instance)
        {
            return (from field in instance.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    where field.FieldType.FullName == "Telerik.WinControls.UI.RadTextBox"
                    let value = field.GetValue(instance)
                    where (string)value.GetType().GetProperty("Name").GetValue(value) == "radTextBox1"
                    select value).FirstOrDefault();
        }
    }
}
