using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;

namespace USBHelperLauncher.Emulator
{
    public abstract class Package
    {
        private const string Format = "{0} {1}";

        private Uri uri;
        private string fileName;
        private readonly string name, version, installPath;
        private Dictionary<string, string> metadata = new Dictionary<string, string>();
        protected FileInfo packageFile;

        public event PostUnpackHandler PostUnpack;
        public delegate void PostUnpackHandler(DirectoryInfo dir);

        public Package(Uri uri, string name, string version) : this(uri, name, version, "") { }

        public Package(Uri uri, string name, string version, string installPath)
        {
            this.uri = uri;
            this.name = name;
            this.version = version;
            this.installPath = installPath;
        }

        public Uri GetURI()
        {
            return uri;
        }

        public string GetName()
        {
            return name;
        }

        public async Task<string> GetFileName()
        {
            if (fileName != null)
            {
                return fileName;
            }
            fileName = Path.GetFileName(uri.LocalPath);
            HttpClient client = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Head, uri);
            var resp = await client.SendAsync(req);
            if (resp.Content.Headers.TryGetValues("Content-Disposition", out IEnumerable<string> headerValues))
            {
                var headerValue = headerValues.FirstOrDefault();
                if (headerValue != null)
                {
                    fileName = new ContentDisposition(headerValue).FileName;
                }
            }
            return fileName;
        }

        public string GetVersion()
        {
            return version;
        }

        public string GetInstallPath()
        {
            return installPath;
        }

        public void SetMeta(string key, string value)
        {
            metadata.Add(key, value);
        }

        public string GetMeta(string key)
        {
            return metadata[key];
        }

        public Dictionary<string, string> GetMeta()
        {
            return metadata;
        }

        public async Task Download(WebClient client, string path)
        {
            string fileName = await GetFileName();
            string file = Path.Combine(path, fileName);
            ServicePointManager.Expect100Continue = true;
            // Use non-browser user agent to avoid being redirected to HTML page (e.g. SourceForge)
            client.Headers.Add("User-Agent", "USBHelperLauncher");
            await client.DownloadFileTaskAsync(uri, file);
            packageFile = new FileInfo(file);
        }

        public async Task<DirectoryInfo> Unpack()
        {
            var dir = await DoUnpack();
            PostUnpack?.Invoke(dir);
            return dir;
        }

        public abstract Task<DirectoryInfo> DoUnpack();

        public override string ToString()
        {
            return String.Format(Format, name, version);
        }
    }
}
