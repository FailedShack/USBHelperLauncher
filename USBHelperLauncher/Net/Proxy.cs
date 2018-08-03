using Fiddler;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using USBHelperLauncher.Emulator;
using USBHelperLauncher.Utils;

namespace USBHelperLauncher.Net
{
    class Proxy : IDisposable
    {

        private static readonly Logger logger = Program.GetLogger();
        private static readonly Endpoint[] endpoints = new Endpoint[]
        {
            new ContentEndpoint(),
            new ApplicationEndpoint(),
            new RegistrationEndpoint(),
            new SiteEndpoint()
        };

        private ushort port;
        private TextWriter log;

        public Proxy(ushort port)
        {
            this.port = port;
            log = new StringWriter();
            FiddlerApplication.Log.OnLogString += Log_OnLogString;
            FiddlerApplication.BeforeRequest += FiddlerApplication_BeforeRequest;
            FiddlerApplication.AfterSessionComplete += FiddlerApplication_AfterSessionComplete;
        }

        public void Start()
        {
            FiddlerApplication.Prefs.SetBoolPref("fiddler.certmaker.CleanupServerCertsOnExit", true);
            FiddlerApplication.CreateProxyEndpoint(7777, true, "localhost");
            FiddlerCoreStartupSettings startupSettings =
                new FiddlerCoreStartupSettingsBuilder()
                    .ListenOnPort(port)
                    .DecryptSSL()
                    .OptimizeThreadPool()
                    .Build();
            FiddlerApplication.Startup(startupSettings);
        }

        private void FiddlerApplication_BeforeRequest(Session oS)
        {
            if (oS.HTTPMethodIs("CONNECT"))
            {
                if(oS.hostname.EndsWith("wiiuusbhelper.com"))
                {
                    oS.oFlags["X-ReplyWithTunnel"] = "Fake for HTTPS Tunnel";
                }
                else
                {
                    oS.oFlags["x-no-decrypt"] = "Passthrough for non-relevant hosts";
                }
                return;
            }

            foreach (Endpoint endpoint in endpoints)
            {
                if (endpoint.Matches(oS))
                {
                    if (!endpoint.Handle(oS))
                    {
                        LogRequest(oS, endpoint, "Unhandled request: " + oS.PathAndQuery);
                    }
                    break;
                }
            }
        }

        private void FiddlerApplication_AfterSessionComplete(Session oS)
        {
            if (oS.responseCode == 404 && oS.HostnameIs("cdn.wiiuusbhelper.com"))
            {
                string path = oS.PathAndQuery;
                string fileName = Path.GetFileName(path);
                if (path.StartsWith("/res/emulators/") && !File.Exists(Path.Combine("\\emulators", fileName)))
                {
                    string noExt = Path.GetFileNameWithoutExtension(fileName);
                    EmulatorConfiguration.Emulator emulator;
                    if (Enum.TryParse(noExt, out emulator))
                    {
                        new Thread(() =>
                        {
                            // Get rid of the exception caused by not finding the file
                            int pid = Program.GetHelperProcess().Id;
                            int lastCount = -1;
                            while (true)
                            {
                                int newCount = WinUtil.GetWindowCount(pid);
                                if (lastCount != -1 && lastCount != newCount)
                                {
                                    break;
                                }
                                lastCount = newCount;
                                Thread.Sleep(30);
                            }
                            WinUtil.CloseWindow(WinUtil.GetForegroundWindow());
                            DialogResult result = MessageBox.Show("It appears you are trying to set-up a game with " + noExt + ", but it has not been downloaded yet.\nWould you like to download it?", "Emulator missing", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                            if (result == DialogResult.Yes)
                            {
                                EmulatorConfiguration config = EmulatorConfiguration.GetConfiguration(emulator);
                                EmulatorConfigurationDialog dialog = new EmulatorConfigurationDialog(config);
                                Application.Run(dialog);
                            }
                        }).Start();
                    }
                }
                else if (path == "/res/prerequisites/java.exe")
                {
                    DialogResult result = MessageBox.Show("To download this game you need Java installed on your computer. Install now?\nCancel the download to prevent additional messages.", "Java required", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result == DialogResult.Yes)
                    {
                        Process.Start("https://www.java.com/en/download/");
                    }
                }
            }
            else if (oS.HostnameIs("application.wiiuusbhelper.com") && oS.PathAndQuery == "/res/db/data.usb")
            {
                MessageBox.Show("You're using a legacy version of Wii U USB Helper.\nSupport for this version is limited which means some features may not work correctly.\nPlease update to a more recent version for better stability.", "Legacy version detected", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        private void Log_OnLogString(object sender, LogEventArgs e)
        {
            log.WriteLine(e.LogString);
        }

        public static void LogRequest(Session oS, Endpoint endpoint, string message)
        {
            logger.WriteLine(String.Format("[{0}] {1}: {2}", oS.RequestMethod, endpoint.GetType().Name, message));
        }

        public WebProxy GetWebProxy()
        {
            return new WebProxy(string.Format("http://127.0.0.1:{0}", port));
        }

        public static string GetCertificateBase64()
        {
            return Convert.ToBase64String(CertMaker.GetRootCertificate().GetRawCertData());
        }

        public string GetLog()
        {
            return log.ToString();
        }

        public void Dispose()
        {
            FiddlerApplication.Shutdown();
        }
    }
}
