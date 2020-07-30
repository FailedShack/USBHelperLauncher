using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace USBHelperInjector
{
    public class Localization
    {
        public const string DefaultLocale = "en-US";

        public static string Namespace;

        private static readonly Dictionary<string, string> _overrides = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> _locale = new Dictionary<string, string>();

        public static string GetString(string key, string def, bool addNamespace = true)
        {
            if (addNamespace)
            {
                key = AddNamespace(key);
            }
            if (_locale.Count == 0)
            {
                Debug.Assert(false, $"{nameof(GetString)}(\"{key}\") called with no initialized locale");
                return def;
            }
            if (_locale.TryGetValue(key, out var str))
            {
                return str;
            }
            Debug.Assert(false, $"{nameof(GetString)}(\"{key}\") yielded no results");
            return def;
        }

        public static void Load(string localeFile, string fallbackFile = null)
        {
            var newDict = DeserializeOrDefault<Dictionary<string, string>>(localeFile);
            if (newDict == null && fallbackFile != null)
            {
                newDict = DeserializeOrDefault<Dictionary<string, string>>(fallbackFile);
            }
            if (newDict == null)
            {
                return;
            }

            foreach (var kv in newDict.Concat(_overrides))
            {
                _locale[kv.Key] = kv.Value;
            }
        }

        public static void Clear()
        {
            _locale.Clear();
        }

        public static void Override(string key, string newValue)
        {
            _overrides[key] = _locale[key] = newValue;
        }

        internal static T DeserializeOrDefault<T>(string filename)
        {
            T obj = default;
            try
            {
                using (var file = File.OpenText(filename))
                using (var reader = new JsonTextReader(file))
                {
                    var serializer = new JsonSerializer();
                    obj = serializer.Deserialize<T>(reader);
                }
            }
            catch (Exception e) when (e is IOException || e is JsonException)
            {
                Debug.Assert(false, e.Message);
            }
            return obj;
        }

        private static string AddNamespace(string key)
        {
            return Namespace != null && !key.Contains(":")
                ? $"{Namespace}:{key}"
                : key;
        }
    }


    public static class LocalizeExtension
    {
        public static string Localize(this string str)
        {
            return Localization.GetString(str, str);
        }
    }
}
