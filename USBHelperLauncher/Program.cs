using Fiddler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using USBHelperInjector.IPC;
using USBHelperLauncher.Configuration;
using USBHelperLauncher.Emulator;
using USBHelperLauncher.Utils;

namespace USBHelperLauncher
{
    class Program
    {
        private static bool killed = false;
        private static bool showConsole = false;
        private static NotifyIcon trayIcon;
        private static RSAParameters rsaParams;

        public static Guid Session { get; } = Guid.NewGuid();
        public static Logger Logger { get; } = new Logger();
        public static Dispatcher Dispatcher { get; } = Dispatcher.CurrentDispatcher;
        public static Database Database { get; } = new Database();
        public static LocaleProvider Locale { get; } = new LocaleProvider();
        public static Net.Proxy Proxy { get; } = new Net.Proxy(8877);
        public static Hosts Hosts { get; set; }
        public static Process HelperProcess { get; private set; }
        public static string HelperVersion { get; private set; }
        public static DateTime SessionStart { get; private set; }
        public static string PublicKey { get; private set; }
        public static bool OverridePublicKey { get; private set; } = true;
        public static bool WineCompat { get; private set; } = false;

        [STAThread]
        static void Main(string[] args)
        {
            Settings.Load();
            Settings.Save();
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);

            for (int i = 0; i < args.Length; i++)
            {
                var group = Regex.Match(args[i], "[-]{1,2}(.*)").Groups[1];
                if (group.Success)
                {
                    switch (group.Value)
                    {
                        case "nokey":
                            OverridePublicKey = false;
                            break;
                        case "showconsole":
                            showConsole = true;
                            break;
                        case "portable":
                            Settings.Portable = true;
                            Settings.Save();
                            break;
                        case "wine":
                            WineCompat = true;
                            break;
                    }
                }
            }

            Logger.WriteLine("Made by FailedShack");
            SetConsoleVisibility(showConsole);
            Application.EnableVisualStyles();

            if (!WineCompat && WineUtil.IsRunningInWine())
            {
                var result = MessageBox.Show(
                    "Detected Wine environment.\nWould you like to enable settings/patches to improve Wine compatibility?\n(To enable this by default, run USBHelperLauncher with the \"--wine\" flag)",
                    "Question",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                WineCompat = result == DialogResult.Yes;
            }
            if (WineCompat)
            {
                Logger.WriteLine("Wine compatibility settings enabled");
                Settings.ForceHttp = true;
                Settings.IPCType = IPCType.TCP;
                Settings.Save();
            }

            if (Settings.ShowUpdateNag)
            {
                Task.Run(async () =>
                {
                    JObject release;
                    try
                    {
                        release = await GithubUtil.GetRelease("FailedShack", "USBHelperLauncher", "latest");
                    }
                    catch
                    {
                        return;
                    }
                    string newVersion = (string)release["tag_name"];
                    string version = GetVersion();
                    if (newVersion.CompareTo(version) > 0)
                    {
                        var updateNag = new CheckboxDialog("New version found: " + newVersion + "\nCurrent version: " + version + "\nDo you want to open the download site?", "Do not show this again.", "Update Checker", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        DialogResult result = updateNag.ShowDialog();
                        if (result == DialogResult.Yes)
                        {
                            Process.Start((string)release["html_url"]);
                        }
                        Settings.ShowUpdateNag = !updateNag.Checked;
                        Settings.Save();
                    }
                }).Wait();
            }

            if (Settings.ShowTranslateNag && Locale.ChosenLocale != LocaleProvider.DefaultLocale)
            {
                var localeInfo = Locale.KnownLocales[Locale.ChosenLocale];
                var translateNag = MessageBox.Show(
                    string.Format("We are currently looking for volunteers to translate Wii U USB Helper to other languages. " +
                    "You may be able to contribute by submitting translations for {0} on Crowdin.\nWould you like to open the site?", localeInfo.Name),
                    "Appeal to Translate", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (translateNag == DialogResult.Yes)
                {
                    Process.Start("https://crowdin.com/project/wii-u-usb-helper");
                }
                Settings.ShowTranslateNag = false;
                Settings.Save();
            }

            try
            {
                MOTD.DisplayIfNeeded(Locale.ChosenLocale);
            }
            catch (WebException e)
            {
                Logger.WriteLine("Could not load message of the day: {0}", e.Message);
            }

            var certs = new DirectoryInfo("certs");
            if (certs.Exists)
            {
                foreach (var file in certs.EnumerateFiles().Where(x => x.Length > 0))
                {
                    try
                    {
                        Proxy.CertificateStore.Import(file.FullName);
                    }
                    catch (CryptographicException)
                    {
                        MessageBox.Show(string.Format("{0} is not a valid certificate file.", file.Name, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error));
                        Environment.Exit(-1);
                    }
                }
            }

            string hostsFile = GetHostsFile();
            if (File.Exists(hostsFile))
            {
                try
                {
                    Hosts = Hosts.Load(hostsFile);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Could not load hosts file: " + e.Message, "Malformed hosts file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Hosts = new Hosts();
                }

                var conflicting = Proxy.GetConflictingHosts();
                if (!Settings.HostsExpert && conflicting.Count > 0)
                {
                    var hostsConflictWarning = new CheckboxDialog(
                        "The following hostnames specified in the hosts file are normally handled by USBHelperLauncher:\n\n" + string.Join("\n", conflicting) +
                        "\n\nIf you override them the program may not function properly." +
                        "\nDo you want to exclude them?", "Do not show this again.", "Conflicting hosts", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    DialogResult result = hostsConflictWarning.ShowDialog();
                    if (result == DialogResult.Yes)
                    {
                        Hosts.hosts = Hosts.hosts.Where(x => !conflicting.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
                    }
                    Settings.HostsExpert = hostsConflictWarning.Checked;
                    Settings.Save();
                }
            }
            else
            {
                Hosts = new Hosts();
                if (Settings.ShowHostsWarning)
                {
                    var hostsWarning = new CheckboxDialog(
                        "It appears you don't currently have a hosts redirector file. This file may be required to route obsolete hostnames to their correct destination.\n" +
                        "If you intended to use this feature, make sure a file named 'hosts.json' is located in the same directory as this executable.\n" +
                        "You may also use the built-in editor located in the Advanced section in the tray icon's context menu.", "Do not show this again.", "Hosts file missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    hostsWarning.ShowDialog();
                    Settings.ShowHostsWarning = !hostsWarning.Checked;
                    Settings.Save();
                }
            }

            try
            {
                Database.LoadFromDir(Path.Combine(GetLauncherPath(), "data"));
            }
            catch (FileNotFoundException e)
            {
                MessageBox.Show(e.Message + "\nMake sure this file is under the data directory.", "Initialization error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }

            if (!File.Exists("WiiU_USB_Helper.exe"))
            {
                MessageBox.Show(
                    File.Exists("ver")
                        ? $"Could not find Wii U USB Helper, your Antivirus software probably deleted it. Try adding the install directory ({GetLauncherPath()}) to your Antivirus' exclusions or disable your Antivirus, then reinstall USB Helper."
                        : "Could not find Wii U USB Helper, please make sure you unpacked the launcher's files (e.g. USBHelperLauncher.exe) and Wii U USB Helper's files (e.g. WiiU_USB_Helper.exe) into the same directory.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error
                );
                Environment.Exit(-1);
            }
            HelperVersion = File.ReadAllLines("ver")[0];

            // Ensure that the cached title key JSON files are valid
            string[] toCheck = { "3FFFD23A80F800ABFCC436A5EC8F7F0B94C728A4", "9C6DD14B8E3530B701BC4F1B77345DADB0C32020" };
            foreach (string file in toCheck)
            {
                string path = Path.Combine(GetInstallPath(), file);
                if (File.Exists(path))
                {
                    try
                    {
                        JToken.Parse(File.ReadAllText(path));
                    }
                    catch (JsonReaderException)
                    {
                        File.Delete(path);
                        Logger.WriteLine(string.Format("Removed bad cache file: {0}", file));
                    }
                }
            }

            // Make sure that FiddlerCore's key container can be accessed
            string keyContainer = FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.bc.KeyContainerName", "FiddlerBCKey");
            if (WinUtil.CSP.TryAcquire(keyContainer) == WinUtil.CSP.NTE_BAD_KEY_STATE)
            {
                WinUtil.CSP.Delete(keyContainer);
                Logger.WriteLine("Removed broken key container: {0}", keyContainer);
            }

            CertMaker.oCertProvider = new BCCertMaker.BCCertMaker(); // Don't try to load CertMaker.dll
            if (!Settings.ForceHttp && !CertMaker.rootCertExists() && !CertMaker.createRootCert())
            {
                MessageBox.Show("Creation of the interception certificate failed.", "Unable to generate certificate.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }

            string executable = Path.Combine(GetLauncherPath(), "WiiU_USB_Helper.exe");

            var running = Process.GetProcessesByName("WiiU_USB_Helper_")
                .FirstOrDefault(p => p.GetMainModuleFileName().StartsWith(GetLauncherPath(), StringComparison.OrdinalIgnoreCase));

            if (running != default(Process))
            {
                DialogResult result = MessageBox.Show("An instance of Wii U USB Helper is already running.\nWould you like to close it?", "Already running", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (result == DialogResult.No)
                {
                    Environment.Exit(0);
                }
                running.Kill();
            }

            // The target .NET version (4.5) only uses TLS 1.0 and 1.1 by default
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            Proxy.Start();

            // Update translations
            var dialog = new ProgressDialog();
            var worker = dialog.GetWorker();
            dialog.SetHeader("Updating translations...");
            new Thread(() => dialog.ShowDialog()).Start();
            Task.Run(async () =>
            {
                try
                {
                    if (await Locale.UpdateIfNeeded(dialog))
                    {
                        Logger.WriteLine("Updated translations: {0}", Settings.TranslationsBuild);
                    }
                    else
                    {
                        Logger.WriteLine("Translations were up to date.");
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLine("Could not update translations: {0}", e.Message);
                }
            }).Wait();

            IPCUtil.CreateService(
                Settings.IPCType,
                typeof(LauncherService),
                typeof(ILauncherService),
                out var serviceUri
            );
            Logger.WriteLine($"WCF host uri: {serviceUri}");

            // Patching
            dialog.Invoke(new Action(() =>
            {
                dialog.SetStyle(ProgressBarStyle.Marquee);
                dialog.GetProgressBar().MarqueeAnimationSpeed = 30;
                dialog.SetHeader("Injecting...");
            }));
            var injector = new ModuleInitInjector(executable);
            executable = Path.Combine(GetLauncherPath(), "WiiU_USB_Helper_.exe");
            if (injector.RequiresInject(executable))
            {
                injector.Inject(executable);
                Logger.WriteLine("Injected module initializer.");
            }
            else
            {
                Logger.WriteLine("Module initializer already injected.");
            }
            dialog.Invoke(new Action(() => dialog.Close()));

            if (OverridePublicKey)
            {
                // Generate an RSA key pair for our donation keys
                using (var rsa = new RSACryptoServiceProvider(2048))
                {
                    rsaParams = rsa.ExportParameters(true);
                    PublicKey = rsa.ToXmlString(false);
                }
            }

            // Time to launch Wii U USB Helper
            SessionStart = DateTime.UtcNow;
            var startInfo = new ProcessStartInfo()
            {
                FileName = executable,
                Arguments = string.Join(" ", HelperVersion, Settings.IPCType, serviceUri),
                UseShellExecute = false,
                RedirectStandardError = true,
                StandardErrorEncoding = Encoding.Default
            };
            var process = new Process() { StartInfo = startInfo };
            process.EnableRaisingEvents = true;
            process.Exited += async (sender, e) =>
            // Not raised in the main thread, must invoke there to interact with UI
            await Dispatcher.InvokeAsync(async () =>
            {
                if (killed) return;
                if (process.ExitCode != 0)
                {
                    Logger.WriteLine("Wii U USB Helper returned non-zero exit code 0x{0:x}:\n{1}", process.ExitCode, process.StandardError.ReadToEnd().Trim()); ;
                    var result = MessageBox.Show(string.Format("Uh-oh. Wii U USB Helper has crashed unexpectedly.\nDo you want to generate a debug log?\n\nError code: 0x{0:x}", process.ExitCode), "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                    if (result == DialogResult.Yes)
                    {
                        await GenerateDebugLog();
                    }
                }
                Cleanup();
                Application.Exit();
            });
            process.Start();
            HelperProcess = process;

            if (Settings.DisableOptionalPatches)
            {
                Logger.WriteLine("Optional patches have been disabled.");
            }

            if (Settings.ForceHttp)
            {
                Logger.WriteLine("Requests will be proxied over plain HTTP.");
            }

            ContextMenu trayMenu = new ContextMenu();
            MenuItem dlEmulator = new MenuItem("Download Emulator");
            foreach (EmulatorConfiguration.Emulator emulator in Enum.GetValues(typeof(EmulatorConfiguration.Emulator)))
            {
                EmulatorConfiguration config = EmulatorConfiguration.GetConfiguration(emulator);
                dlEmulator.MenuItems.Add(config.GetName(), (sender, e) => OnDownloadEmulator(config));
            }
            MenuItem language = new MenuItem("Language") { RadioCheck = true };
            foreach (var lang in Locale.AvailableLocales)
            {
                language.MenuItems.Add(lang.Value.Native, (sender, e) =>
                {
                    Settings.Locale = lang.Key;
                    Settings.Save();
                    MessageBox.Show("Your language choice has been saved.\nPlease restart USBHelperLauncher for changes to take effect.", "Restart required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }).Checked = lang.Key == Locale.ChosenLocale;
            }
            if (language.MenuItems.Count == 0)
            {
                language.MenuItems.Add("No translations found").Enabled = false;
            }
            MenuItem advanced = new MenuItem("Advanced");
            advanced.MenuItems.Add("Toggle Console", OnVisibilityChange);
            advanced.MenuItems.Add("Clear Install", OnClearInstall);
            advanced.MenuItems.Add("Generate Donation Key", OnGenerateKey).Enabled = OverridePublicKey;
            advanced.MenuItems.Add("Hosts Editor", OnOpenHostsEditor);
            advanced.MenuItems.Add("Export Sessions", OnExportSessions);
            trayMenu.MenuItems.Add("Exit", OnExit);
            trayMenu.MenuItems.Add("Check for Updates", OnUpdateCheck);
            trayMenu.MenuItems.Add("Report Issue", async (sender, e) => await GenerateDebugLog());
            trayMenu.MenuItems.Add(dlEmulator);
            trayMenu.MenuItems.Add(language);
            trayMenu.MenuItems.Add(advanced);
            trayIcon = new NotifyIcon
            {
                Text = "Wii U USB Helper Launcher",
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                ContextMenu = trayMenu,
                Visible = true
            };
            Application.Run();
        }

        private static void OnExit(object sender, EventArgs e)
        {
            Cleanup();
            Environment.Exit(0);
        }

        private async static void OnUpdateCheck(object sender, EventArgs e)
        {
            JObject release;
            try
            {
                release = await GithubUtil.GetRelease("FailedShack", "USBHelperLauncher", "latest");
            }
            catch (WebException ex)
            {
                MessageBox.Show("Could not check for updates.\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string newVersion = (string) release["tag_name"];
            string version = GetVersion();
            if (newVersion.CompareTo(version) > 0)
            {
                DialogResult result = MessageBox.Show("New version found: " + newVersion + "\nCurrent version: " + version + "\nDo you want to open the download site?", "Update Checker", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (result == DialogResult.Yes)
                {
                    Process.Start((string) release["html_url"]);
                }
            }
            else
            {
                MessageBox.Show("No update found.", "Update Checker", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static void OnVisibilityChange(object sender, EventArgs e)
        {
            SetConsoleVisibility(!showConsole);
        }

        private static void OnClearInstall(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to clear your current Wii U USB Helper install data?\nThis action cannot be undone.", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                Cleanup();
                ProgressDialog dialog = new ProgressDialog();
                ProgressBar progressBar = dialog.GetProgressBar();
                BackgroundWorker worker = dialog.GetWorker();
                worker.DoWork += delegate (object obj, DoWorkEventArgs args)
                {
                    string[] toRemove = Settings.Portable ? new string[] { Path.Combine(GetLauncherPath(), "userdata") } : new string[] { GetInstallPath(), GetInstallConfPath() };
                    foreach (string dir in toRemove)
                    {
                        if (Directory.Exists(dir))
                        {
                            DirectoryInfo dirInfo = new DirectoryInfo(dir);
                            var files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);
                            dialog.BeginInvoke(new Action(() => dialog.Reset(files.Length)));

                            foreach (FileInfo file in files)
                            {
                                dialog.BeginInvoke(new Action(() => dialog.SetHeader("Removing: " + file.Name)));
                                file.Delete();
                                dialog.BeginInvoke(new Action(() => progressBar.PerformStep()));
                            }

                            var subDirs = dirInfo.GetDirectories();
                            dialog.BeginInvoke(new Action(() => dialog.Reset(subDirs.Length)));

                            foreach (DirectoryInfo subDir in subDirs)
                            {
                                dialog.BeginInvoke(new Action(() => dialog.SetHeader("Removing: " + subDir.Name)));
                                subDir.Delete(true);
                                dialog.BeginInvoke(new Action(() => progressBar.PerformStep()));
                            }

                            Directory.Delete(dir);
                        }
                    }
                    dialog.BeginInvoke(new Action(() => dialog.Close()));
                };
                worker.RunWorkerCompleted += delegate (object obj, RunWorkerCompletedEventArgs args)
                {
                    Application.Exit();
                };
                new Thread(() =>
                {
                    Application.Run(dialog);
                }).Start();
                worker.RunWorkerAsync();
            }
        }

        private static void OnExportSessions(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "HTTPArchive (*.har)|*.har",
                DefaultExt = "har",
                AddExtension = true
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Logger.WriteLine("Exporting Sessions...");

                Session[] sessions = Proxy.GetSessions();
                FiddlerApplication.oTranscoders.ImportTranscoders(Assembly.Load("BasicFormatsForCore"));

                var options = new Dictionary<string, object>
                {
                    { "Filename", dialog.FileName },
                    { "MaxTextBodyLength", 10*1024*1024 },
                    { "MaxBinaryBodyLength", 10*1024*1024 }
                };
                FiddlerApplication.DoExport("HTTPArchive v1.2", sessions, options, null);

                MessageBox.Show("Session export successful.", "Session export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static void OnGenerateKey(object sender, EventArgs e)
        {
            Clipboard.SetText(GenerateDonationKey());
            MessageBox.Show("Donation key generated and stored in your clipboard!", "Donation key", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void OnOpenHostsEditor(object sender, EventArgs e)
        {
            var window = Application.OpenForms.OfType<HostsDialog>().FirstOrDefault();
            if (window != null)
            {
                window.WindowState = FormWindowState.Normal;
                window.Activate();
                return;
            }
            new HostsDialog().ShowDialog();
        }

        private static void OnDownloadEmulator(EmulatorConfiguration config)
        {
            string emulatorPath = Path.Combine("emulators", config.GetName() + ".zip");
            if (File.Exists(emulatorPath))
            {
                DialogResult result = MessageBox.Show("This emulator has already been downloaded. Do you want to replace it?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.No)
                {
                    return;
                }
                File.Delete(emulatorPath);
            }
            new EmulatorConfigurationDialog(config).Show();
        }

        public static string GetHostsFile()
        {
            return Path.Combine(GetLauncherPath(), "hosts.json");
        }

        public static string GetLauncherPath()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        private static string GetInstallPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "USB_HELPER");
        }

        private static string GetInstallConfPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Hikari06");
        }

        private static void SetConsoleVisibility(bool visible)
        {
            showConsole = visible;
            ShowWindow(GetConsoleWindow(), visible ? SW_SHOW : SW_HIDE);
        }

        private static void Cleanup()
        {
            if (!HelperProcess.HasExited)
            {
                HelperProcess.CloseMainWindow();
                if (!HelperProcess.WaitForExit(500))
                {
                    killed = true;
                    HelperProcess.Kill();
                }
            }
            if (Proxy != null)
            {
                Proxy.Dispose();
            }
            trayIcon.Visible = false;
            trayIcon.Dispose();
            Logger.Dispose();
        }

        public static string GenerateDonationKey()
        {
            byte[] key = new byte[272];
            byte[] buffer = new byte[16];
            Random random = new Random();
            random.NextBytes(buffer);
            byte[] signature;
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.ImportParameters(rsaParams);
                signature = rsa.SignData(buffer, CryptoConfig.MapNameToOID("SHA1"));
            }
            Buffer.BlockCopy(buffer, 0, key, 0, 16);
            Buffer.BlockCopy(signature, 0, key, 16, 256);
            return Convert.ToBase64String(key);
        }

        public static async Task GenerateDebugLog()
        {
            DebugMessage debug = new DebugMessage(Logger.GetLog(), Proxy.GetLog());
            async Task toFile()
            {
                var dialog = new SaveFileDialog()
                {
                    Filter = "Log Files (*.log)|*.log",
                    FileName = string.Format("usbhelperlauncher_{0:yyyy-MM-dd_HH-mm-ss}.log", DateTime.Now)
                };
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(dialog.FileName, await debug.Build());
                }
            }
            if (Control.ModifierKeys == Keys.Shift)
            {
                await toFile();
                return;
            }

            try
            {
                var url = await debug.PublishAsync(timeout: TimeSpan.FromSeconds(5));
                Clipboard.SetText(url);
                MessageBox.Show("Debug message created and published, the link has been stored in your clipboard.\nProvide this link when reporting an issue.", "Debug message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is JsonReaderException || ex is TaskCanceledException)
            {
                Logger.WriteLine("Could not submit log to Hastebin: {0}", ex);
                await toFile();
            }
        }

        public static string GetVersion()
        {
            string[] rawVersion = Application.ProductVersion.Split('.');
            string version = rawVersion[0] + "." + rawVersion[1];
            int revision = int.Parse(rawVersion[3]);
            if (revision > 0)
            {
                version += (char)(97 + revision);
            }
            return version;
        }

        // Displays a form as a child of Wii U USB Helper
        public static void ShowChildDialog(Form dialog)
        {
            WinUtil.SetWindowLong(dialog.Handle, -8 /*GWL_HWNDPARENT*/, HelperProcess.MainWindowHandle);
            dialog.ShowDialog();
        }

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(int sig);
        static EventHandler _handler;

        private static bool Handler(int sig)
        {
            Cleanup();
            return false;
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
    }
}
