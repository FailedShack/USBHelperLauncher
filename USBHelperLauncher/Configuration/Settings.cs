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

        [Setting("Launcher", true)]
        public static bool ShowUpdateNag { get; set; }

        [Setting("Launcher", false)]
        public static bool ShowHostsWarning { get; set; }

        [Setting("Launcher", true)]
        public static bool ShowCloudWarning { get; set; }

        [Setting("Launcher", 1000)]
        public static int SessionBufferSize { get; set; }

        [Setting("Launcher", 64 * 1000)]
        public static int SessionSizeLimit { get; set; }

        [Setting("Launcher")]
        public static Dictionary<string, string> EndpointFallbacks { get; set; } = new Dictionary<string, string>()
        {
            { typeof(ContentEndpoint).Name, "https://cdn.shiftinv.cc/wiiuusbhelper/cdn/" }
        };

        [Setting("Launcher")]
        public static Dictionary<string, string> TitleKeys { get; set; } = new Dictionary<string, string>();

        [Setting("Injector", false)]
        public static bool DisableOptionalPatches { get; set; }

        [Setting("Injector", 3)]
        public static int MaxRetries { get; set; }

        [Setting("Injector", 0)]
        public static int DelayBetweenRetries { get; set; }

        public static void Save()
        {
            JObject conf = new JObject();
            foreach (var prop in typeof(Settings).GetProperties())
            {
                Setting setting = prop.GetCustomAttributes().OfType<Setting>().FirstOrDefault();
                if (setting == null)
                {
                    continue;
                }
                if (conf[setting.Section] == null)
                {
                    conf[setting.Section] = new JObject();
                }
                var obj = conf[setting.Section];
                var value = prop.GetValue(null);
                if (value != null)
                {
                    obj[prop.Name] = JToken.FromObject(value);
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
            foreach (var prop in typeof(Settings).GetProperties())
            {
                Setting setting = prop.GetCustomAttributes().OfType<Setting>().FirstOrDefault();
                if (setting == null)
                {
                    continue;
                }
                var token = conf.SelectToken(string.Join(".", setting.Section, prop.Name));
                var value = token == null ? setting.Default : token.ToObject(prop.PropertyType);
                if (value != null)
                {
                    prop.SetValue(null, value);
                }
            }
        }
    }
}
