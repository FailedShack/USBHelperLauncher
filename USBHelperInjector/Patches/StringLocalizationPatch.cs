using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using USBHelperInjector.Properties;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    class StringLocalizationPatch
    {
        static Dictionary<string, string> index, locale;

        static MethodBase TargetMethod()
        {
            index = DeserializeOrDefault<Dictionary<string, string>>("localeIndex.json");
            locale = DeserializeOrDefault<Dictionary<string, string>>(InjectorService.LocaleFile);

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
            if (index == null || locale == null)
                return;

            string hash;
            using (var md5 = MD5.Create())
            {
                var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(__result));
                hash = string.Concat(bytes.Select(x => x.ToString("x2")));
            }
            if (index.TryGetValue(hash, out string key))
            {
                if (key == "welcome.disclaimer.unused")
                {
                    __result = Resources.Disclaimer;
                }
                else
                {
                    __result = locale[key];
                }
            }
        }

        static T DeserializeOrDefault<T>(string filename)
        {
            T obj = default;
            try
            {
                using (var file = File.OpenText(filename))
                using (var reader = new JsonTextReader(file))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    obj = serializer.Deserialize<T>(reader);
                }
            }
            catch (Exception e) when (e is IOException || e is JsonException)
            {
                // Default case
            }
            return obj;
        }
    }
}
