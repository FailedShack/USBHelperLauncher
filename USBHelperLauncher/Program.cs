using Fiddler;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace USBHelperLauncher
{
    class Program
    {
        private static readonly Guid sessionGuid = Guid.NewGuid();
        private static readonly Logger logger = new Logger();

        private static DateTime sessionStart;
        private static Process process;
        private static string helperVersion;
        private static Thread backgroundThread;
        private static bool showConsole = true;
        private static NotifyIcon trayIcon = new NotifyIcon();
        private static Proxy proxy;

        [STAThread]
        static void Main(string[] args)
        {
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);

            logger.WriteLine("Made by FailedShack");
            SetConsoleVisibility(false);
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
            if (!CertMaker.rootCertExists() || !CertMaker.rootCertIsTrusted())
            {
                MessageBox.Show(
                    "You will now be prompted to install an SSL certificate, this is required to allow other programs to make HTTPS requests while Wii U USB Helper is open.\n" +
                    "This is part of the initial setup process.\n", "First run - Read carefully!", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                while (true)
                {
                    if (!CertMaker.createRootCert() || !CertMaker.trustRootCert())
                    {
                        DialogResult result = MessageBox.Show("The setup process cannot continue without an SSL certificate.\nAre you sure you want to cancel?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                        if (result == DialogResult.Yes)
                        {
                            Environment.Exit(0);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                string firefox = GetFirefoxExecutable();
                if (firefox != null)
                {
                    logger.WriteLine("Firefox: " + firefox);
                    DialogResult result = MessageBox.Show("You will now also be prompted to install the certificate on Firefox.\nMake sure to check 'Trust this CA to identify websites'.", "First run", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    StartProcess(firefox, "localhost");
                }
            }
            logger.WriteLine("Found trusted SSL Certificate.");

            proxy = new Proxy(CertMaker.GetRootCertificate());
            proxy.Start();

            // Time to launch Wii U USB Helper
            sessionStart = DateTime.UtcNow;
            process = StartProcess("WiiU_USB_Helper.exe", helperVersion);
            ContextMenu trayMenu = new ContextMenu();
            MenuItem advanced = new MenuItem("Advanced");
            advanced.MenuItems.Add("Toggle Console", OnVisibilityChange);
            advanced.MenuItems.Add("Clear Install", OnClearInstall);
            advanced.MenuItems.Add("Remove Certificate", OnRemoveCertificate);
            trayMenu.MenuItems.Add("Exit", OnExit);
            trayMenu.MenuItems.Add("Report Issue", OnDebugMessage);
            trayMenu.MenuItems.Add(advanced);
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
            Application.EnableVisualStyles();
            Application.Run();
        }

        static Process StartProcess(String path, String arguments)
        {
            Process process = new Process();
            process.StartInfo.FileName = path;
            process.StartInfo.Arguments = arguments;
            process.Start();
            return process;
        }

        static string GetFirefoxExecutable()
        {
            RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            RegistryKey regKey = localMachine.OpenSubKey("SOFTWARE\\Mozilla\\Mozilla Firefox");
            if (regKey == null)
            {
                return null;
            }
            string version = regKey.GetValue("CurrentVersion") as string;
            RegistryKey mainRegKey = regKey.OpenSubKey(version + "\\Main");
            if (mainRegKey == null)
            {
                return null;
            }
            return mainRegKey.GetValue("PathToExe") as string;
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
                ProgressDialog dialog = new ProgressDialog(true);
                dialog.SetHeader("Removing...");
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
                            var files = dirInfo.GetFiles();
                            dialog.BeginInvoke(new Action(() => dialog.Reset(files.Length)));

                            foreach (FileInfo file in files)
                            {
                                file.Delete();
                                worker.ReportProgress(0);
                            }

                            var subDirs = dirInfo.GetDirectories();
                            dialog.BeginInvoke(new Action(() => dialog.Reset(subDirs.Length)));

                            foreach (DirectoryInfo subDir in subDirs)
                            {
                                subDir.Delete(true);
                                worker.ReportProgress(0);
                            }

                            Directory.Delete(dir);
                        }
                    }
                    dialog.BeginInvoke(new Action(() => dialog.Close()));
                };
                new Thread(() =>
                {
                    Application.Run(dialog);
                }).Start();
                worker.RunWorkerAsync();
                Application.Exit();
            }
        }

        private static void OnRemoveCertificate(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("The SSL certificate is required to intercept HTTPS requests from Wii U USB Helper. The launcher will not work without it.\nAre you sure you want to remove it?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes && CertMaker.removeFiddlerGeneratedCerts())
            {
                Cleanup();
                Application.Exit();
            }
        }

        private async static void OnDebugMessage(object sender, EventArgs e)
        {
            DebugMessage debug = new DebugMessage(logger.GetLog());
            Clipboard.SetText(await debug.PublishAsync());
            MessageBox.Show("Debug message created and published, the link has been stored in your clipboard.\nProvide this link when reporting an issue.", "Debug message", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        public static Logger GetLogger()
        {
            return logger;
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
