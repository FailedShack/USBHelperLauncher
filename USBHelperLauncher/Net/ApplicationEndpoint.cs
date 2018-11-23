using Fiddler;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace USBHelperLauncher.Net
{
    class ApplicationEndpoint : ContentEndpoint
    {
        public ApplicationEndpoint() : base("application.wiiuusbhelper.com") { }

        [Request("/proxy.php?*")]
        public void GetProxy(Session oS)
        {
            var data = GetRequestData(oS);
            oS.utilCreateResponseAndBypassServer();
            oS.oResponse.headers.SetStatus(307, "Redirect");
            byte[] bytes = Convert.FromBase64String(data.Get("url"));
            string url = Encoding.UTF8.GetString(bytes);
            oS.oResponse["Location"] = url;
            Proxy.LogRequest(oS, this, "Redirecting to " + url);
        }

        [Request("/zipProxy.php?*")]
        public void GetZipProxy(Session oS)
        {
            var data = GetRequestData(oS);
            byte[] bytes = Convert.FromBase64String(data.Get("url"));
            string url = Encoding.UTF8.GetString(bytes);
            string content;
            using (WebClient client = new WebClient())
            {
                client.Proxy = Program.GetProxy().GetWebProxy();
                content = client.DownloadString(url);
            }
            MemoryStream stream = new MemoryStream();
            using (ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("content");
                using (var entryStream = entry.Open())
                using (var streamWriter = new StreamWriter(entryStream))
                {
                    streamWriter.Write(content);
                }
            }
            byte[] dataBytes = stream.ToArray();
            oS.utilCreateResponseAndBypassServer();
            oS.oResponse["Content-Length"] = dataBytes.Length.ToString();
            oS.responseBodyBytes = dataBytes;
            Proxy.LogRequest(oS, this, "Created zip from " + url);
        }
    }
}
