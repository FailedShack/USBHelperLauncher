using Fiddler;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
using USBHelperLauncher.Configuration;
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
            new SiteEndpoint(),
            new WiiUShopEndpoint(),
            new TitlekeysWiiUEndpoint(),
            new Titlekeys3DSEndpoint()
        };

        private bool shownCloudWarning;
        private ushort port;
        private TextWriter log;
        private Buffer<Session> sessions;

        public Proxy(ushort port)
        {
            this.port = port;
            log = new StringWriter();
            var maxBufferSize = Settings.SessionBufferSize;
            sessions = maxBufferSize < 0 ? new Buffer<Session>() : new Buffer<Session>(maxBufferSize);
            FiddlerApplication.Log.OnLogString += Log_OnLogString;
            FiddlerApplication.BeforeRequest += FiddlerApplication_BeforeRequest;
            FiddlerApplication.AfterSessionComplete += FiddlerApplication_AfterSessionComplete;
            FiddlerApplication.ResponseHeadersAvailable += FiddlerApplication_ResponseHeadersAvailable;
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
                if (oS.hostname.EndsWith("wiiuusbhelper.com"))
                {
                    oS.oFlags["X-ReplyWithTunnel"] = "Fake for HTTPS Tunnel";
                }
                else
                {
                    oS.oFlags["x-no-decrypt"] = "Passthrough for non-relevant hosts";
                }
                return;
            }

            if (Program.Hosts.GetHosts().Contains(oS.hostname))
            {
                string ip = Program.Hosts.Get(oS.hostname).ToString();
                oS.bypassGateway = true;
                oS.oFlags["x-overrideHost"] = ip;
                LogRequest(oS, "Redirected request to " + ip);
            }

            // We really need better IPC
            // Needed during first run, when the user has to input the title key sites
            if (Settings.TitleKeys == null)
            {
                Settings.Load();
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
            if (oS.responseBodyBytes.Length < Settings.SessionSizeLimit)
            {
                sessions.Add(oS);
            }
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
                                Program.ShowChildDialog(dialog);
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
            else if (oS.HostnameIs("cloud.wiiuusbhelper.com") && oS.PathAndQuery == "/saves/login.php" && Settings.ShowCloudWarning && !shownCloudWarning)
            {
                shownCloudWarning = true;
                var cloudWarning = new CheckboxDialog("The cloud save backup service is hosted by Willzor and is in no way affiliated to USBHelperLauncher. We cannot guarantee the continuity of these services and as such advise against relying on them.", "Do not show this again.", "Cloud service warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                new Thread(() => Program.ShowChildDialog(cloudWarning)).Start();
                Settings.ShowCloudWarning = !cloudWarning.Checked;
                Settings.Save();
            }
        }

        private void Log_OnLogString(object sender, LogEventArgs e)
        {
            log.WriteLine(e.LogString);
        }

        private void FiddlerApplication_ResponseHeadersAvailable(Session oS)
        {
            if (oS.oResponse.headers.Exists("Content-Length"))
            {
                if (long.TryParse(oS.oResponse["Content-Length"], out long len))
                {
                    // Don't cache responses > 50MB
                    if (len > 50 * 1024 * 1024)
                    {
                        oS.bBufferResponse = false;
                        oS["log-drop-response-body"] = "save memory";
                    }
                }
            }
        }

        public static void LogRequest(Session oS, Endpoint endpoint, string message)
        {
            logger.WriteLine(String.Format("[{0}] {1}: {2}", oS.RequestMethod, endpoint.GetType().Name, message));
        }

        public static void LogRequest(Session oS, string message)
        {
            logger.WriteLine(String.Format("[{0}] {1}: {2}", oS.RequestMethod, oS.hostname, message));
        }

        public WebProxy GetWebProxy()
        {
            return new WebProxy(string.Format("http://127.0.0.1:{0}", port));
        }

        public static string GetCertificateBase64()
        {
            return Convert.ToBase64String(CertMaker.GetRootCertificate().GetRawCertData());
        }

        public Session[] GetSessions()
        {
            return sessions.ToArray();
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
