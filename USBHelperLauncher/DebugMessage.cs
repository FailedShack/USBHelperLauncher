using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using USBHelperLauncher.Configuration;
using USBHelperLauncher.Utils;
using static USBHelperLauncher.Utils.WinUtil;

namespace USBHelperLauncher
{
    class DebugMessage
    {
        private readonly string log, fiddlerLog;

        public DebugMessage(string log, string fiddlerLog)
        {
            this.log = log;
            this.fiddlerLog = fiddlerLog;
        }

        public async Task<string> Build()
        {
            ComputerInfo info = new ComputerInfo();
            StringBuilder sb = new StringBuilder();
            Exception exception = await TryReachProxy();
            DateTime now = DateTime.UtcNow;
            var hosts = Program.Hosts;
            var locale = Program.Locale;
            var av = new Dictionary<string, bool>();
            GetProcessAndNativeArchitecture(out var processArchitecture, out var nativeArchitecture);
            sb.Append('-', 13).Append(" USBHelperLauncher Debug Information ").Append('-', 13).AppendLine();
            sb.AppendLine("Debug Time: " + now + " (UTC)");
            sb.AppendLine("Session Length: " + (now - Program.SessionStart).ToString(@"hh\:mm\:ss"));
            sb.AppendLine("Session GUID: " + Program.Session.ToString());
            sb.AppendLine("Proxy Available: " + (exception == null ? "Yes" : "No (" + exception.Message + ")"));
            sb.AppendLine("Public Key Override: " + (Program.OverridePublicKey ? "Yes" : "No"));
            sb.AppendLine("Version: " + Program.GetVersion());
            sb.AppendLine("Helper Version: " + Program.HelperVersion);
            sb.AppendLine(".NET Framework Version: " + Get45or451FromRegistry());
            sb.AppendFormat("Operating System: {0} ({1}-bit)", info.OSFullName, Environment.Is64BitOperatingSystem ? 64 : 32).AppendLine();
            sb.AppendFormat("Native Architecture: {0}", nativeArchitecture).AppendLine();
            sb.AppendFormat("Process Architecture: {0}", processArchitecture).AppendLine();
            sb.AppendLine("Used Locale: " + locale.ChosenLocale);
            sb.AppendLine("System Language: " + CultureInfo.CurrentUICulture.Name);
            sb.AppendLine("Total Memory: " + info.TotalPhysicalMemory);
            sb.AppendLine("Available Memory: " + info.AvailablePhysicalMemory);
            sb.AppendLine("Command-line Arguments: " + string.Join(" ", Environment.GetCommandLineArgs().Skip(1)));
            if (WineUtil.IsRunningInWine()) sb.AppendLine("Wine version: " + WineUtil.TryGetVersion());
            TryCatch(() => GetAntiVirus(ref av), e => sb.AppendLine("Antivirus Software: Error (" + e.Message + ")"));
            AppendDictionary(sb, "Antivirus Software", av.ToDictionary(x => x.Key, x => x.Value ? "Enabled" : "Disabled"));
            AppendDictionary(sb, "Hosts", hosts.GetHosts().ToDictionary(x => x, x => hosts.Get(x).ToString()));
            AppendDictionary(sb, "Endpoint Fallbacks", Settings.EndpointFallbacks);
            AppendDictionary(sb, "Key Sites", Settings.TitleKeys);
            AppendDictionary(sb, "Server Certificates", Program.Proxy.CertificateStore.Cast<X509Certificate2>()
                .ToDictionary(x => x.GetNameInfo(X509NameType.SimpleName, false), x => x.Thumbprint), format: "{0} ({1})");
            sb.Append('-', 26).Append(" Log Start ").Append('-', 26).AppendLine();
            sb.Append(log);
            sb.Append('-', 22).Append(" Fiddler Log Start ").Append('-', 22).AppendLine();
            sb.Append(fiddlerLog);
            return sb.ToString();
        }

        private StringBuilder AppendDictionary(StringBuilder sb, string header, Dictionary<string, string> dict, string format = null)
        {
            if (dict.Count() > 0)
            {
                sb.Append(header).AppendLine(":");
                dict.ToList().ForEach(x => sb.AppendFormat(format ?? "{0} -> {1}", x.Key, x.Value).AppendLine());
            }
            return sb;
        }

        public async Task<string> PublishAsync(TimeSpan? timeout = null)
        {
            var content = new StringContent(await Build(), Encoding.UTF8, "application/x-www-form-urlencoded");
            using (var client = new HttpClient())
            using (var cancel = new CancellationTokenSource(timeout ?? TimeSpan.FromMilliseconds(-1)))
            {
                var response = await client.PostAsync("https://api.nul.sh/logs.php", content, cancel.Token);
                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<Exception> TryReachProxy()
        {
            using (var client = new WebClient())
            {
                client.Proxy = Program.Proxy.GetWebProxy();
                string respString;
                try
                {
                    respString = await client.DownloadStringTaskAsync("http://www.wiiuusbhelper.com/session");
                }
                catch (WebException e)
                {
                    return e;
                }
                if (Guid.TryParse(respString, out Guid session) && Program.Session == session)
                {
                    return null;
                }
                return new InvalidOperationException("Invalid response: " + Regex.Replace(string.Concat(respString.Take(40)), @"\s+", " ") + "...");

            }
        }

        private static string CheckFor45DotVersion(int releaseKey)
        {
            if (releaseKey >= 528040)
            {
                return "4.8 or later";
            }
            if (releaseKey >= 461808)
            {
                return "4.7.2";
            }
            if (releaseKey >= 461308)
            {
                return "4.7.1";
            }
            if (releaseKey >= 460798)
            {
                return "4.7";
            }
            if (releaseKey >= 394802)
            {
                return "4.6.2";
            }
            if (releaseKey >= 394254)
            {
                return "4.6.1";
            }
            if (releaseKey >= 393295)
            {
                return "4.6";
            }
            if (releaseKey >= 379893)
            {
                return "4.5.2";
            }
            if (releaseKey >= 378675)
            {
                return "4.5.1";
            }
            if (releaseKey >= 378389)
            {
                return "4.5";
            }
            // This code should never execute. A non-null release key should mean
            // that 4.5 or later is installed.
            return "No 4.5 or later version detected";
        }

        private static string Get45or451FromRegistry()
        {
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            using (RegistryKey ndpKey = baseKey.OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
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

        private static void GetAntiVirus(ref Dictionary<string, bool> antivirus)
        {
            using (var searcher = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntiVirusProduct"))
            {
                var collection = searcher.Get();
                foreach (ManagementObject obj in collection)
                {
                    var name = obj["displayName"].ToString();
                    var state = (uint)obj["productState"];
                    antivirus.Add(name, (state & 0x1000) != 0);
                }
            }
        }

        private static void GetProcessAndNativeArchitecture(out string processArchitecture, out string nativeArchitecture)
        {
            try
            {
                IsWow64Process2(Process.GetCurrentProcess().Handle, out var processMachine, out var nativeMachine);
                processArchitecture = Enum.GetName(typeof(ImageFileMachine), processMachine);
                nativeArchitecture = Enum.GetName(typeof(ImageFileMachine), nativeMachine);
            }
            catch (EntryPointNotFoundException)
            {
                // IsWow64Process2 is not available (ie. on anything older than Windows 10 version 1709)
                SystemInfo processSystemInfo = default, nativeSystemInfo = default;
                GetSystemInfo(ref processSystemInfo); GetNativeSystemInfo(ref nativeSystemInfo);
                processArchitecture = Enum.GetName(typeof(ProcessorArchitecture), processSystemInfo.wProcessorArchitecture);
                nativeArchitecture = Enum.GetName(typeof(ProcessorArchitecture), nativeSystemInfo.wProcessorArchitecture);
            }
        }

        private static void TryCatch(Action tryAction, Action<Exception> catchAction)
        {
            try
            {
                tryAction();
            }
            catch (Exception e)
            {
                catchAction(e);
            }
        }
    }
}
