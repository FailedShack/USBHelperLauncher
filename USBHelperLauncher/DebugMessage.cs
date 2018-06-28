using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace USBHelperLauncher
{
    class DebugMessage
    {
        private static readonly HttpClient client = new HttpClient();
        private string log;

        public DebugMessage(string log)
        {
            this.log = log;
        }

        public async Task<string> Build()
        {
            ComputerInfo info = new ComputerInfo();
            StringBuilder sb = new StringBuilder();
            Exception exception = await TryReachProxy();
            DateTime now = DateTime.UtcNow;
            sb.Append('-', 10).Append(" Wii U USB Helper Loader Debug Information ").Append('-', 10).AppendLine();
            sb.AppendLine("Debug Time: " + now + " (UTC)");
            sb.AppendLine("Session Length: " + (now - Program.GetSessionStart()).ToString(@"hh\:mm\:ss"));
            sb.AppendLine("Session GUID: " + Program.GetSessionGuid().ToString());
            sb.AppendLine("Proxy Available: " + (exception == null ? "Yes" : "No (" + exception.Message + ")"));
            sb.AppendLine("Version: " + Program.GetVersion());
            sb.AppendLine("Helper Version: " + Program.GetHelperVersion());
            sb.AppendLine(".NET Framework Version: " + Get45or451FromRegistry());
            sb.AppendLine("Operating System: " + info.OSFullName);
            sb.AppendLine("Platform: " + info.OSPlatform);
            sb.AppendLine("System Language: " + info.InstalledUICulture);
            sb.AppendLine("Total Memory: " + info.TotalPhysicalMemory);
            sb.AppendLine("Available Memory: " + info.AvailablePhysicalMemory);
            sb.Append('-', 26).Append(" Log Start ").Append('-', 26).AppendLine();
            sb.Append(log);
            return sb.ToString();
        }

        public async Task<string> PublishAsync()
        {
            StringContent content = new StringContent(await Build(), Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await client.PostAsync("https://hastebin.com/documents", content);
            JObject json = JObject.Parse(await response.Content.ReadAsStringAsync());
            return "https://hastebin.com/" + (string) json["key"];
        }

        public async Task<Exception> TryReachProxy()
        {
            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync("http://www.wiiuusbhelper.com/session");
            }
            catch (HttpRequestException e)
            {
                return e;
            }
            string respString = await response.Content.ReadAsStringAsync();
            Guid guid;
            if (Guid.TryParse(respString, out guid))
            {
                if (Program.GetSessionGuid() == guid)
                {
                    return null;
                }
            }
            return new InvalidOperationException("Invalid response: " + Regex.Replace(string.Concat(respString.Take(40)), @"\s+", " ") + "...");
        }

        private static string CheckFor45DotVersion(int releaseKey)
        {
            if (releaseKey >= 393295)
            {
                return "4.6 or later";
            }
            if ((releaseKey >= 379893))
            {
                return "4.5.2 or later";
            }
            if ((releaseKey >= 378675))
            {
                return "4.5.1 or later";
            }
            if ((releaseKey >= 378389))
            {
                return "4.5 or later";
            }
            // This line should never execute. A non-null release key should mean
            // that 4.5 or later is installed.
            return "No 4.5 or later version detected";
        }

        private static string Get45or451FromRegistry()
        {
            using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
            {
                if (ndpKey != null && ndpKey.GetValue("Release") != null)
                {
                    return CheckFor45DotVersion((int)ndpKey.GetValue("Release"));
                }
                else
                {
                    return "Version 4.5 or later is not detected.";
                }
            }
        }
    }
}
