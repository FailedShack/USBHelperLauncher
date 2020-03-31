using Fiddler;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Mime;
using System.Text;
using USBHelperLauncher.Configuration;
using USBHelperLauncher.Utils;

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
            Uri uri = new Uri(Encoding.UTF8.GetString(bytes));

            string content;
            using (var resp = Program.Proxy.Get(uri).GetResponse())
            {
                var contentType = new ContentType(resp.Headers["Content-Type"]);
                var encoding = Encoding.GetEncoding(contentType.CharSet ?? "UTF-8");
                try
                {
                    using (var reader = new StreamReader(resp.GetResponseStream(), encoding))
                    {
                        content = reader.ReadToEnd();
                    }
                }
                catch (WebException e)
                {
                    Proxy.LogRequest(oS, this, "Unable to download data from " + uri.ToString() + ": " + e);
                    oS.utilCreateResponseAndBypassServer();
                    if (e.Status == WebExceptionStatus.ProtocolError)
                    {
                        var response = (HttpWebResponse)e.Response;
                        oS.oResponse.headers.SetStatus((int)response.StatusCode, response.StatusDescription);
                    }
                    else
                    {
                        oS.oResponse.headers.SetStatus(500, "Internal Server Error: " + e.Status);
                    }
                    return;
                }
            }

            MemoryStream stream = new MemoryStream();
            using (ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("content");
                using (var streamWriter = new StreamWriter(entry.Open()))
                {
                    streamWriter.Write(content);
                }
            }

            byte[] dataBytes = stream.ToArray();
            oS.utilCreateResponseAndBypassServer();
            oS.oResponse["Content-Length"] = dataBytes.Length.ToString();
            oS.responseBodyBytes = dataBytes;
            Proxy.LogRequest(oS, this, "Created zip from " + uri.ToString());
        }

        [Request("/requestZipHash.php")]
        [Request("/hash_zip/*")]
        public void GetZipHash(Session oS)
        {
            string data = Path.GetFileName(oS.PathAndQuery);
            if (data == "requestZipHash.php")
            {
                data = GetRequestData(oS).Get("url");
            }

            byte[] bytes = Convert.FromBase64String(data);
            string url = Encoding.UTF8.GetString(bytes);

            oS.utilCreateResponseAndBypassServer();
            Proxy.LogRequest(oS, this, "Sent empty zip hash for " + url);
        }
    }
}
