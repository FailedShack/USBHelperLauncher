using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using USBHelperLauncher.Configuration;

namespace USBHelperLauncher
{
    class MOTD
    {
        public static bool DisplayIfNeeded(string locale)
        {
            try
            {
                using (var client = new WebClient())
                {
                    if (Settings.LastMessage != null)
                    {
                        client.Headers[HttpRequestHeader.IfNoneMatch] = string.Format("\"{0}\"", Settings.LastMessage);
                    }
                    var uri = new Uri(string.Format("https://dl.nul.sh/USBHelperLauncher/motd/{0}", locale));
                    var motd = client.DownloadString(uri).Split('|');
                    if (motd.Length >= 2)
                    {
                        Uri.TryCreate(motd.ElementAtOrDefault(2), UriKind.Absolute, out Uri target);
                        var result = MessageBox.Show(motd[0], motd[1], target == null ? MessageBoxButtons.OK : MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (result == DialogResult.Yes)
                        {
                            Process.Start(target.ToString());
                        }
                        Settings.LastMessage = client.ResponseHeaders[HttpResponseHeader.ETag].Trim('"');
                        Settings.Save();
                    }
                }
            }
            catch (WebException e) when (((HttpWebResponse)e.Response)?.StatusCode == HttpStatusCode.NotModified)
            {
                return false;
            }
            return true;
        }
    }
}
