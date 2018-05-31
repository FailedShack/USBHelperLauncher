using Fiddler;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using USBHelperLauncher.Emulator;
using USBHelperLauncher.Utils;

namespace USBHelperLauncher
{
    class Proxy : IDisposable
    {

        private X509Certificate2 certificate;
        private readonly Logger logger = Program.GetLogger();
        private readonly Database database = Program.GetDatabase();

        public Proxy(X509Certificate2 certificate)
        {
            this.certificate = certificate;
            FiddlerApplication.BeforeRequest += FiddlerApplication_BeforeRequest;
            FiddlerApplication.AfterSessionComplete += FiddlerApplication_AfterSessionComplete;
            FiddlerApplication.OnValidateServerCertificate += FiddlerApplication_OnValidateServerCertificate;
        }

        private void FiddlerApplication_OnValidateServerCertificate(object sender, ValidateServerCertificateEventArgs e)
        {
            if (SslPolicyErrors.None == e.CertificatePolicyErrors)
            {
                return;
            }
            // This endpoint uses an invalid certificate, we need to let it slide
            // Required for most images to load properly
            if (e.Session.HostnameIs("kanzashi-wup.cdn.nintendo.net"))
            {
                e.ValidityState = CertificateValidity.ForceValid;
                logRequest(e.Session, "Force forwarded request.");
            }
        }

        public void Start()
        {
            FiddlerApplication.Prefs.SetBoolPref("fiddler.certmaker.CleanupServerCertsOnExit", true);
            FiddlerApplication.CreateProxyEndpoint(7777, true, "localhost");
            FiddlerCoreStartupSettings startupSettings =
                new FiddlerCoreStartupSettingsBuilder()
                    .ListenOnPort(8877)
                    .RegisterAsSystemProxy()
                    .DecryptSSL()
                    .OptimizeThreadPool()
                    .Build();
            FiddlerApplication.Startup(startupSettings);
        }

        private void FiddlerApplication_BeforeRequest(Session oS)
        {
            if (oS.HTTPMethodIs("CONNECT")) { oS.oFlags["X-ReplyWithTunnel"] = "Fake for HTTPS Tunnel"; return; }
            if (oS.HostnameIs("cdn.wiiuusbhelper.com"))
            {
                string path = oS.PathAndQuery;
                string fileName = Path.GetFileName(path);
                string folder = "\\data";
                if (path.StartsWith("/wiiu/info/icons/"))
                {
                    folder = "\\images\\wiiu\\icons";
                }
                else if (path.StartsWith("/3ds/icons/"))
                {
                    folder = "\\images\\3ds\\icons";
                }
                else if (path.StartsWith("/res/emulators/"))
                {
                    folder = "\\emulators";
                }
                else if (path.StartsWith("/res/prerequisites/"))
                {
                    folder = "\\redist";
                }
                else if (path.StartsWith("/res/db/"))
                {
                    Match match = Regex.Match(fileName, @"datav(\d).enc");
                    if (match.Success)
                    {
                        Database.EncryptionVersion version;
                        switch (match.Groups[1].Value)
                        {
                            case "4":
                                version = Database.EncryptionVersion.DATA_V4;
                                break;
                            case "6":
                                version = Database.EncryptionVersion.DATA_V6;
                                break;
                            default:
                                oS.responseCode = 500;
                                logRequest(oS, "Invalid encryption version requested.");
                                return;
                        }
                        logRequest(oS, "Sending database contents with " + version.ToString() + " encryption");
                        oS.utilCreateResponseAndBypassServer();
                        byte[] bytes = database.Encrypt(version).ToArray();
                        oS.oResponse["Content-Length"] = bytes.Length.ToString();
                        oS.responseBodyBytes = bytes;
                        return;
                    }
                }
                string localPath = Path.Combine(Program.GetLauncherPath() + folder, fileName);
                oS.utilCreateResponseAndBypassServer();
                if (File.Exists(localPath))
                {
                    logRequest(oS, "Sending local copy of " + fileName);
                    oS.LoadResponseFromFile(localPath);
                }
                else
                {
                    logRequest(oS, "Missing resource requested: " + oS.PathAndQuery);
                    oS.responseCode = 404;
                }
            }
            else if (oS.HostnameIs("registration.wiiuusbhelper.com"))
            {
                string path = oS.PathAndQuery;
                oS.utilCreateResponseAndBypassServer();
                logger.WriteLine(path);
                if (path == "/getContributors.php")
                {
                    oS.utilSetResponseBody("Wii U USB Helper Launcher made by\n!FailedShack\n© 2018");
                }
                logRequest(oS, "Sent credits.");
            }
            else if (oS.HostnameIs("localhost"))
            {
                oS.utilCreateResponseAndBypassServer();
                oS.oResponse["Content-Type"] = "application/x-x509-ca-cert";
                oS.utilSetResponseBody("-----BEGIN CERTIFICATE-----\n" + GetCertificateBase64() + "\n-----END CERTIFICATE----- ");
                logRequest(oS, "Sent certificate.");
            }
            else if (oS.HostnameIs("www.wiiuusbhelper.com"))
            {
                oS.utilCreateResponseAndBypassServer();
                oS.utilSetResponseBody(Program.GetSessionGuid().ToString());
                logRequest(oS, "Sent session guid.");
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
        }

        private void logRequest(Session oS, string message)
        {
            logger.WriteLine(String.Format("[{0}] {1}: {2}", oS.RequestMethod, oS.hostname, message));
        }

        private string GetCertificateBase64()
        {
            return Convert.ToBase64String(CertMaker.GetRootCertificate().GetRawCertData());
        }

        public void Dispose()
        {
            FiddlerApplication.Shutdown();
        }
    }
}
