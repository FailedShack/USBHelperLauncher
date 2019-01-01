using Fiddler;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using USBHelperLauncher.Emulator;
using USBHelperLauncher.Utils;
using System.Text.RegularExpressions;
using System.Linq;
using USBHelperLauncher.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.ServiceModel;
using USBHelperInjector.Contracts;

namespace USBHelperLauncher
{
    class Program
    {
        private static readonly Guid sessionGuid = Guid.NewGuid();
        private static readonly Logger logger = new Logger();
        private static readonly Database database = new Database();

        private static DateTime sessionStart;
        private static Process process;
        private static string helperVersion;
        private static Thread backgroundThread;
        private static bool showConsole = false;
        private static NotifyIcon trayIcon;
        private static Net.Proxy proxy;
        private static RSAParameters rsaParams;

        public static Hosts Hosts { get; set; }
        public static bool PatchPublicKey { get; private set; } = true;

        [STAThread]
        static void Main(string[] args)
        {
            Settings.Load();
            Settings.Save();
            proxy = new Net.Proxy(8877);
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);

            for (int i = 0; i < args.Length; i++)
            {
                var group = Regex.Match(args[i], "[-]{1,2}(.*)").Groups[1];
                if (group.Success)
                {
                    switch (group.Value)
                    {
                        case "nopatch":
                            PatchPublicKey = false;
                            break;
                        case "showconsole":
                            showConsole = true;
                            break;
                    }
                }
            }

            logger.WriteLine("Made by FailedShack");
            SetConsoleVisibility(showConsole);
            Application.EnableVisualStyles();

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
                });
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
                database.LoadFromDir(Path.Combine(GetLauncherPath(), "data"));
            }
            catch (FileNotFoundException e)
            {
                MessageBox.Show(e.Message + "\nMake sure this file is under the data directory.", "Initialization error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }

            if (!File.Exists("ver") || !File.Exists("WiiU_USB_Helper.exe"))
            {
                MessageBox.Show("Could not find Wii U USB Helper, please make sure this executable is in the correct folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
            helperVersion = File.ReadAllLines("ver")[0];
            int revision = Int32.Parse(helperVersion.Substring(helperVersion.LastIndexOf('.') + 1));
            if (helperVersion.StartsWith("0.6.1"))
            {
                // Workaround to allow it to launch
                if (revision >= 653)
                {
                    string installPath = GetInstallPath();
                    string lastTitles = Path.Combine(installPath, "lasttitles");
                    if (revision > 653)
                    {
                        string installConfPath = GetInstallConfPath();
                        Directory.CreateDirectory(installConfPath);
                        File.Create(Path.Combine(installConfPath, "user.config")).Close();
                    }
                    if (!File.Exists(lastTitles))
                    {
                        Directory.CreateDirectory(installPath);
                        StringBuilder sb = new StringBuilder();
                        // Rev. 653 minimums: 3 lines, single character each
                        // Revs. 654 & 655 minimums: 25 lines, 16 chars each
                        for (int lines = 0; lines != 25; lines++)
                        {
                            sb.Append('0', 16).AppendLine();
                        }
                        File.WriteAllText(lastTitles, sb.ToString());
                    }
                }
            }

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
                        logger.WriteLine(string.Format("Removed bad cache file: {0}", file));
                    }
                }
            }

            if (!CertMaker.rootCertExists() && !CertMaker.createRootCert())
            {
                MessageBox.Show("Creation of the interception certificate failed.", "Unable to generate certificate.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }

            string executable = Path.Combine(GetLauncherPath(), "WiiU_USB_Helper.exe");

            var running = Process.GetProcessesByName("Patched").FirstOrDefault(p => p.GetMainModuleFileName().StartsWith(GetLauncherPath(), StringComparison.OrdinalIgnoreCase));

            if (running != default(Process))
            {
                DialogResult result = MessageBox.Show("An instance of Wii U USB Helper is already running.\nWould you like to close it?", "Already running", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (result == DialogResult.No)
                {
                    Environment.Exit(0);
                }
                running.Kill();
            }

            proxy.Start();
            ServiceHost host = new ServiceHost(typeof(LauncherService), new Uri("net.pipe://localhost/LauncherService"));
            host.AddServiceEndpoint(typeof(ILauncherService), new NetNamedPipeBinding(), "");
            host.Open();

            // Patching
            ProgressDialog dialog = new ProgressDialog();
            dialog.SetStyle(ProgressBarStyle.Marquee);
            dialog.GetProgressBar().MarqueeAnimationSpeed = 30;
            dialog.SetHeader("Injecting...");
            new Thread(() => {
                dialog.ShowDialog();
            }).Start();
            var injector = new ModuleInitInjector(executable);
            executable = Path.Combine(GetLauncherPath(), "Patched.exe");
            injector.Inject(executable);
            logger.WriteLine("Injected module initializer.");
            if (PatchPublicKey)
            {
                dialog.Invoke(new Action(() => dialog.SetHeader("Patching...")));
                RSAPatcher patcher = new RSAPatcher(executable);
                string xml;
                using (var rsa = new RSACryptoServiceProvider(2048))
                {
                    rsaParams = rsa.ExportParameters(true);
                    xml = rsa.ToXmlString(false);
                }
                var builder = new StringBuilder();
                var element = XElement.Parse(xml);
                var settings = new XmlWriterSettings
                {
                    OmitXmlDeclaration = true,
                    Indent = true
                };
                using (var xmlWriter = XmlWriter.Create(builder, settings))
                {
                    element.Save(xmlWriter);
                }
                patcher.SetPublicKey(builder.ToString());
                logger.WriteLine("Patched public key.");
            }
            else
            {
                logger.WriteLine("Patching has been disabled.");
            }
            dialog.Invoke(new Action(() => dialog.Close()));

            // Time to launch Wii U USB Helper
            sessionStart = DateTime.UtcNow;
            process = StartProcess(executable, helperVersion);

            if (Settings.DisableOptionalPatches)
            {
                logger.WriteLine("Optional patches have been disabled.");
            }

            ContextMenu trayMenu = new ContextMenu();
            MenuItem dlEmulator = new MenuItem("Download Emulator");
            foreach (EmulatorConfiguration.Emulator emulator in Enum.GetValues(typeof(EmulatorConfiguration.Emulator)))
            {
                EmulatorConfiguration config = EmulatorConfiguration.GetConfiguration(emulator);
                dlEmulator.MenuItems.Add(config.GetName(), (sender, e) => OnDownloadEmulator(config));
            }
            MenuItem advanced = new MenuItem("Advanced");
            advanced.MenuItems.Add("Toggle Console", OnVisibilityChange);
            advanced.MenuItems.Add("Clear Install", OnClearInstall);
            advanced.MenuItems.Add("Generate Donation Key", OnGenerateKey).Enabled = PatchPublicKey;
            advanced.MenuItems.Add("Hosts Editor", OnOpenHostsEditor);
            advanced.MenuItems.Add("Export Sessions", OnExportSessions);
            trayMenu.MenuItems.Add("Exit", OnExit);
            trayMenu.MenuItems.Add("Check for Updates", OnUpdateCheck);
            trayMenu.MenuItems.Add("Report Issue", OnDebugMessage);
            trayMenu.MenuItems.Add(dlEmulator);
            trayMenu.MenuItems.Add(advanced);
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Wii U USB Helper Launcher";
            trayIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;
            backgroundThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                while (!process.HasExited)
                {
                    try
                    {
                        Thread.Sleep(30);
                    }
                    catch (ThreadInterruptedException) { }
                }
                Cleanup();
                Application.Exit();
            });
            backgroundThread.Start();
            Application.Run();
        }

        static Process StartProcess(string path, string arguments)
        {
            Process process = new Process();
            process.StartInfo.FileName = path;
            process.StartInfo.Arguments = arguments;
            process.Start();
            return process;
        }

        public static Process GetHelperProcess()
        {
            return process;
        }

        public static string GetHelperVersion()
        {
            return helperVersion;
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
                backgroundThread.Interrupt();
                Cleanup();
                ProgressDialog dialog = new ProgressDialog();
                ProgressBar progressBar = dialog.GetProgressBar();
                BackgroundWorker worker = dialog.GetWorker();
                worker.DoWork += delegate (object obj, DoWorkEventArgs args)
                {
                    string[] toRemove = new string[] { GetInstallPath(), GetInstallConfPath() };
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

        private async static void OnDebugMessage(object sender, EventArgs e)
        {
            DebugMessage debug = new DebugMessage(logger.GetLog(), proxy.GetLog());
            Clipboard.SetText(await debug.PublishAsync());
            MessageBox.Show("Debug message created and published, the link has been stored in your clipboard.\nProvide this link when reporting an issue.", "Debug message", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void OnExportSessions(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "HTTPArchive (*.har)|*.har";
            saveFileDialog.DefaultExt = "har";
            saveFileDialog.AddExtension = true;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                logger.WriteLine("Exporting Sessions...");

                Session[] sessions = proxy.GetSessions();
                FiddlerApplication.oTranscoders.ImportTranscoders(Assembly.Load("BasicFormatsForCore"));

                var options = new Dictionary<string, object>
                {
                    { "Filename", saveFileDialog.FileName },
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
            if (!process.HasExited)
            {
                process.Kill();
            }
            if (proxy != null)
            {
                proxy.Dispose();
            }
            trayIcon.Visible = false;
            trayIcon.Dispose();
            logger.Dispose();
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
            var process = GetHelperProcess();
            WinUtil.SetWindowLong(dialog.Handle, -8 /*GWL_HWNDPARENT*/, process.MainWindowHandle);
            dialog.ShowDialog();
        }

        public static Logger GetLogger()
        {
            return logger;
        }

        public static Database GetDatabase()
        {
            return database;
        }

        public static Net.Proxy GetProxy()
        {
            return proxy;
        }

        public static DateTime GetSessionStart()
        {
            return sessionStart;
        }

        public static Guid GetSessionGuid()
        {
            return sessionGuid;
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
