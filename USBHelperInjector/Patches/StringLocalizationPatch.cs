using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    class StringLocalizationPatch
    {
        private static Dictionary<string, string> _index = Localization.DeserializeOrDefault<Dictionary<string, string>>("localeIndex.json");

        static MethodBase TargetMethod()
        {
            // Taken from de4dot
            MethodInfo method = null;
            foreach (var type in ReflectionHelper.MainModule.GetTypes())
            {
                var fields = AccessTools.GetDeclaredFields(type);
                var methods = AccessTools.GetDeclaredMethods(type);
                if (type.IsPublic)
                    continue;
                if (fields.Count != 1)
                    continue;
                if (!fields.Any(field => field.FieldType == typeof(byte[])))
                    continue;
                if (methods.Count != 1 && methods.Count != 2)
                    continue;
                if (type.GetNestedTypes(AccessTools.all).Length > 0)
                    continue;

                foreach (var m in methods)
                {
                    var parameters = m.GetParameters();
                    if (m.ReturnType == typeof(string) && parameters.FirstOrDefault()?.ParameterType == typeof(int))
                    {
                        method = m;
                        continue;
                    }
                    break;
                }

                if (method != null)
                    break;
            }

            return method;
        }

        static void Postfix(ref string __result)
        {
            if (_index == null)
                return;

            string hash;
            using (var md5 = MD5.Create())
            {
                var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(__result));
                hash = string.Concat(bytes.Select(x => x.ToString("x2")));
            }
            if (_index.TryGetValue(hash, out var key))
            {
                __result = Localization.GetString(key, __result, false);
            }
        }
    }
}
