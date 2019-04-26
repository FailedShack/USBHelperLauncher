using Fiddler;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Mime;
using System.Text;

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

            try
            {
                string content;
                using (WebClient client = new WebClient())
                {
                    client.Proxy = Program.GetProxy().GetWebProxy();
                    byte[] responseBytes = client.DownloadData(url);
                    var contentType = new ContentType(client.ResponseHeaders["Content-Type"]);
                    content = Encoding.GetEncoding(contentType.CharSet ?? "UTF-8").GetString(responseBytes);
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
                Proxy.LogRequest(oS, this, "Created zip from " + url);
            }
            catch (WebException webEx)
            {
                Proxy.LogRequest(oS, this, "Unable to create zip for " + url + ": " + webEx);

                var response = (HttpWebResponse)webEx.Response;
                oS.utilCreateResponseAndBypassServer();
                oS.oResponse.headers.SetStatus((int)response.StatusCode, response.StatusDescription);
                oS.utilSetResponseBody("");
            }
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
