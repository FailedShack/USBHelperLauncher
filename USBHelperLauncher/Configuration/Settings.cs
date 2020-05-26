using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using USBHelperLauncher.Net;

namespace USBHelperLauncher.Configuration
{
    public class Settings
    {
        private const string file = "conf.json";
        private static List<KeyValuePair<PropertyInfo, Setting>> _properties;

        private static List<KeyValuePair<PropertyInfo, Setting>> Properties
        {
            get
            {
                if (_properties == null)
                    _properties = typeof(Settings)
                        .GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        .Select(x => new KeyValuePair<PropertyInfo, Setting>(x, Setting.From(x)))
                        .Where(x => x.Value != null)
                        .ToList();
                return _properties;
            }
        }

        [Setting("Launcher")]
        private static string DoNotModify { get; set; }

        [Setting("Launcher", false)]
        public static bool HostsExpert { get; set; }

        [Setting("Launcher", true)]
        public static bool ShowUpdateNag { get; set; }

        [Setting("Launcher", true)]
        public static bool ShowTranslateNag { get; set; }

        [Setting("Launcher", false)]
        public static bool ShowHostsWarning { get; set; }

        [Setting("Launcher", true)]
        public static bool ShowCloudWarning { get; set; }

        [Setting("Launcher", 1000)]
        public static int SessionBufferSize { get; set; }

        [Setting("Launcher", 64 * 1000)]
        public static int SessionSizeLimit { get; set; }

        [Setting("Launcher")]
        public static string Locale { get; set; }

        [Setting("Launcher")]
        public static string TranslationsBuild { get; set; }

        [Setting("Launcher")]
        public static string LastMessage { get; set; }

        [Setting("Launcher", forgetful: true)]
        public static Dictionary<string, string> EndpointFallbacks { get; set; } = new Dictionary<string, string>()
        {
            { typeof(ContentEndpoint).Name, "https://cdn.shiftinv.cc/wiiuusbhelper/cdn/" }
        };

        [Setting("Launcher")]
        public static Dictionary<string, string> TitleKeys { get; set; } = new Dictionary<string, string>();

        [Setting("Injector", false)]
        public static bool DisableOptionalPatches { get; set; }

        [Setting("Injector", new string[] { "toolWeb", "toolMods", "toolChat" })]
        public static string[] DisableTabs { get; set; }

        [Setting("Injector", 5)]
        public static int MaxRetries { get; set; }

        [Setting("Injector", 1000)]
        public static int DelayBetweenRetries { get; set; }

        [Setting("Injector", false)]
        public static bool Portable { get; set; }

        [Setting("Injector", false)]
        public static bool ForceHttp { get; set; }

        [Setting("Injector", false)]
        public static bool NoFunAllowed { get; set; }

        public static void Save()
        {
            DoNotModify = Program.GetVersion();
            JObject conf = new JObject();
            foreach (var prop in Properties)
            {
                var section = prop.Value.Section;
                if (conf[section] == null)
                {
                    conf[section] = new JObject();
                }
                var obj = conf[section];
                var value = prop.Key.GetValue(null);
                if (value != null)
                {
                    obj[prop.Key.Name] = JToken.FromObject(value);
                }
            }
            File.WriteAllText(Path.Combine(Program.GetLauncherPath(), file), conf.ToString());
        }

        public static void Load()
        {
            string path = Path.Combine(Program.GetLauncherPath(), file);
            JObject conf;
            if (File.Exists(path))
            {
                conf = JObject.Parse(File.ReadAllText(path));
            }
            else
            {
                conf = new JObject();
            }
            foreach (var setting in Properties.OrderByDescending(x => x.Key.Name == nameof(DoNotModify)))
            {
                var forget = setting.Value.Forgetful && DoNotModify != Program.GetVersion();
                var token = conf.SelectToken(string.Join(".", setting.Value.Section, setting.Key.Name));
                var value = token == null || forget ? setting.Value.Default : token.ToObject(setting.Key.PropertyType);
                if (value != null)
                {
                    setting.Key.SetValue(null, value);
                }
            }
        }
    }
}
