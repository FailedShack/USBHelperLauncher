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
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using USBHelperInjector;
using USBHelperInjector.Contracts;
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
                    }
                }
            }

            Logger.WriteLine("Made by FailedShack");
            SetConsoleVisibility(showConsole);
            Application.EnableVisualStyles();

            // Update translations
            var dialog = new ProgressDialog();
            dialog.SetHeader("Updating translations...");
            dialog.SetStyle(ProgressBarStyle.Marquee);
            dialog.GetProgressBar().MarqueeAnimationSpeed = 30;
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
            Localization.Namespace = "launcher";
            Localization.Load(
                Path.Combine("locale", $"{Locale.ChosenLocale}.local.json"),
                Path.Combine("locale", $"{Localization.DefaultLocale}.local.json")
            );

            dialog.Invoke(new Action(() => dialog.SetHeader("progress.initializing".Localize())));

            if (Settings.ShowUpdateNag)
            {
                Task.Run(async () => await CheckUpdates(false)).Wait();
            }

            if (Settings.ShowTranslateNag && Locale.ChosenLocale != Localization.DefaultLocale)
            {
                var localeInfo = Locale.KnownLocales[Locale.ChosenLocale];
                var translateNag = MessageBox.Show(
                    string.Format("dialog.crowdin".Localize(), localeInfo.Name),
                    "dialog.crowdin.title".Localize(),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information
                );
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
                        MessageBox.Show(
                            string.Format("dialog.invalidcertificate".Localize(), file.Name),
                            "common:error".Localize(),
                            MessageBoxButtons.OK, MessageBoxIcon.Error
                        );
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
                    MessageBox.Show(
                        string.Format("dialog.malformedhosts".Localize(), e.Message),
                        "dialog.malformedhosts.title".Localize(),
                        MessageBoxButtons.OK, MessageBoxIcon.Error
                    );
                    Hosts = new Hosts();
                }

                var conflicting = Proxy.GetConflictingHosts();
                if (!Settings.HostsExpert && conflicting.Count > 0)
                {
                    var hostsConflictWarning = new CheckboxDialog(
                        string.Format("dialog.hostsconflict".Localize(), string.Join("\n", conflicting)),
                        "dontshowagain".Localize(),
                        "dialog.hostsconflict.title".Localize(),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning
                    );
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
                        "dialog.hostswarning".Localize(),
                        "dontshowagain".Localize(),
                        "dialog.hostswarning.title".Localize(),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning
                    );
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
                MessageBox.Show(
                    string.Format("dialog.databaseloadfailure".Localize(), e.Message),
                    "dialog.databaseloadfailure.title".Localize(),
                    MessageBoxButtons.OK, MessageBoxIcon.Error
                );
                Environment.Exit(-1);
            }

            if (!File.Exists("ver") || !File.Exists("WiiU_USB_Helper.exe"))
            {
                MessageBox.Show(
                    "dialog.usbhelpernotfound".Localize(),
                    "common:error".Localize(),
                    MessageBoxButtons.OK, MessageBoxIcon.Error
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
                        Logger.WriteLine("Removed bad cache file: {0}", file);
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
                MessageBox.Show(
                    "dialog.certcreationfailure".Localize(),
                    "dialog.certcreationfailure.title".Localize(),
                    MessageBoxButtons.OK, MessageBoxIcon.Error
                );
                Environment.Exit(-1);
            }

            string executable = Path.Combine(GetLauncherPath(), "WiiU_USB_Helper.exe");

            var running = Process.GetProcessesByName("Patched").FirstOrDefault(p => p.GetMainModuleFileName().StartsWith(GetLauncherPath(), StringComparison.OrdinalIgnoreCase));

            if (running != default(Process))
            {
                DialogResult result = MessageBox.Show(
                    "dialog.alreadyrunning".Localize(),
                    "dialog.alreadyrunning.title".Localize(),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation
                );
                if (result == DialogResult.No)
                {
                    Environment.Exit(0);
                }
                running.Kill();
            }

            Proxy.Start();

            ServiceHost host = new ServiceHost(typeof(LauncherService), new Uri("net.pipe://localhost/LauncherService"));
            host.AddServiceEndpoint(typeof(ILauncherService), new NetNamedPipeBinding(""), "");
            host.Open();

            // Patching
            dialog.Invoke(new Action(() =>
            {
                dialog.SetHeader("progress.injecting".Localize());
            }));
            var injector = new ModuleInitInjector(executable);
            executable = Path.Combine(GetLauncherPath(), "WiiU_USB_Helper_.exe");
            injector.Inject(executable);
            Logger.WriteLine("Injected module initializer.");
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
                Arguments = HelperVersion,
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
                    Logger.WriteLine("Wii U USB Helper returned non-zero exit code 0x{0:x}:\n{1}", process.ExitCode, process.StandardError.ReadToEnd().Trim());
                    var result = MessageBox.Show(
                        string.Format("dialog.usbhelpercrashed".Localize(), process.ExitCode),
                        "common:error".Localize(),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Error
                    );
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
            MenuItem dlEmulator = new MenuItem("menu.downloademulator".Localize());
            foreach (EmulatorConfiguration.Emulator emulator in Enum.GetValues(typeof(EmulatorConfiguration.Emulator)))
            {
                EmulatorConfiguration config = EmulatorConfiguration.GetConfiguration(emulator);
                dlEmulator.MenuItems.Add(config.GetName(), (sender, e) => OnDownloadEmulator(config));
            }
            MenuItem language = new MenuItem("menu.language".Localize()) { RadioCheck = true };
            foreach (var lang in Locale.AvailableLocales)
            {
                language.MenuItems.Add(lang.Value.Native, (sender, e) =>
                {
                    Settings.Locale = lang.Key;
                    Settings.Save();
                    MessageBox.Show(
                        "dialog.languagechanged".Localize(),
                        "dialog.languagechanged.title".Localize(),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }).Checked = lang.Key == Locale.ChosenLocale;
            }
            if (language.MenuItems.Count == 0)
            {
                language.MenuItems.Add("menu.language.empty".Localize()).Enabled = false;
            }
            MenuItem advanced = new MenuItem("menu.advanced".Localize());
            advanced.MenuItems.Add("menu.toggleconsole".Localize(), OnVisibilityChange);
            advanced.MenuItems.Add("menu.clearinstall".Localize(), OnClearInstall);
            advanced.MenuItems.Add("menu.generatekey".Localize(), OnGenerateKey).Enabled = OverridePublicKey;
            advanced.MenuItems.Add("menu.hostseditor".Localize(), OnOpenHostsEditor);
            advanced.MenuItems.Add("menu.exportsessions".Localize(), OnExportSessions);
            trayMenu.MenuItems.Add("menu.exit".Localize(), OnExit);
            trayMenu.MenuItems.Add("menu.updatecheck".Localize(), async (sender, e) => await CheckUpdates(true));
            trayMenu.MenuItems.Add("menu.reportissue".Localize(), async (sender, e) => await GenerateDebugLog());
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

        private static async Task CheckUpdates(bool userRequested)
        {
            JObject release;
            try
            {
                release = await GithubUtil.GetRelease("FailedShack", "USBHelperLauncher", "latest");
            }
            catch (WebException ex)
            {
                if (userRequested)
                {
                    MessageBox.Show(
                        string.Format("dialog.updatecheck.failed".Localize(), ex.Message),
                        "common:error".Localize(),
                        MessageBoxButtons.OK, MessageBoxIcon.Error
                    );
                }
                return;
            }
            var newVersion = (string)release["tag_name"];
            var version = GetVersion();
            if (string.Compare(newVersion, version, StringComparison.Ordinal) > 0)
            {
                DialogResult result;
                if (userRequested)
                {
                    result = MessageBox.Show(
                        string.Format("dialog.updatecheck".Localize(), newVersion, version),
                        "dialog.updatecheck.title".Localize(),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information
                    );
                }
                else
                {
                    var updateNag = new CheckboxDialog(
                        string.Format("dialog.updatecheck".Localize(), newVersion, version),
                        "dontshowagain".Localize(),
                        "dialog.updatecheck.title".Localize(),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information
                    );
                    result = updateNag.ShowDialog();
                    Settings.ShowUpdateNag = !updateNag.Checked;
                    Settings.Save();
                }
                if (result == DialogResult.Yes)
                {
                    Process.Start((string)release["html_url"]);
                }
            }
            else
            {
                if (userRequested)
                {
                    MessageBox.Show(
                        "dialog.updatecheck.latest".Localize(),
                        "dialog.updatecheck.title".Localize(),
                        MessageBoxButtons.OK, MessageBoxIcon.Information
                    );
                }
            }
        }

        private static void OnVisibilityChange(object sender, EventArgs e)
        {
            SetConsoleVisibility(!showConsole);
        }

        private static void OnClearInstall(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "dialog.clearinstall".Localize(),
                "common:warning".Localize(),
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning
            );
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
                                dialog.BeginInvoke(new Action(() => dialog.SetHeader(string.Format("progress.removing".Localize(), file.Name))));
                                file.Delete();
                                dialog.BeginInvoke(new Action(() => progressBar.PerformStep()));
                            }

                            var subDirs = dirInfo.GetDirectories();
                            dialog.BeginInvoke(new Action(() => dialog.Reset(subDirs.Length)));

                            foreach (DirectoryInfo subDir in subDirs)
                            {
                                dialog.BeginInvoke(new Action(() => dialog.SetHeader(string.Format("progress.removing".Localize(), subDir.Name))));
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

                MessageBox.Show(
                    "dialog.exportsessions".Localize(),
                    "dialog.exportsessions.title".Localize(),
                    MessageBoxButtons.OK, MessageBoxIcon.Information
                );
            }
        }

        private static void OnGenerateKey(object sender, EventArgs e)
        {
            Clipboard.SetText(GenerateDonationKey());
            MessageBox.Show(
                "dialog.generatekey".Localize(),
                "dialog.generatekey.title".Localize(),
                MessageBoxButtons.OK, MessageBoxIcon.Information
            );
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
                DialogResult result = MessageBox.Show(
                    "dialog.emulatoralreadydownloaded".Localize(),
                    "common:warning".Localize(),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning
                );
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
                MessageBox.Show(
                    "dialog.debugloguploaded".Localize(),
                    "dialog.debugloguploaded.title".Localize(),
                    MessageBoxButtons.OK, MessageBoxIcon.Information
                );
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is JsonReaderException || ex is TaskCanceledException)
            {
                Logger.WriteLine("Could not submit debug log: {0}", ex);
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
