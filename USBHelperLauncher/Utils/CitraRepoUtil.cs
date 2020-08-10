using System;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using USBHelperLauncher.Emulator;

namespace USBHelperLauncher.Utils
{
    class CitraRepoUtil
    {
        private const string Url = "https://repo.citra-emu.org/";
        private const string Path = "{0}/{1}{2}";
        private const string Package = "org.citra.{0}.{1}";

        public static async Task<Package> GetPackageAsync(string branch, string platform)
        {
            string xml;
            using (WebClient client = new WebClient())
            {
                ServicePointManager.Expect100Continue = true;
                client.Headers.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2;)");
                try
                {
                    xml = await client.DownloadStringTaskAsync(Url + "Updates.xml");
                }
                catch (WebException)
                {
                    throw;
                }
            }
            XDocument doc = XDocument.Parse(xml);
            XElement updates = doc.Element("Updates");
            String packageName = String.Format(Package, branch, platform);
            foreach (XElement XPackage in updates.Elements("PackageUpdate"))
            {
                if (XPackage.Element("Name").Value == packageName)
                {
                    string version = XPackage.Element("Version").Value;
                    string name = XPackage.Element("DisplayName").Value;
                    string fileName = XPackage.Element("DownloadableArchives").Value;
                    Uri uri = new Uri(Url + String.Format(Path, packageName, version, fileName));
                    Package package = new SevenZipPackage(uri, name, version);
                    package.SetMeta("Description", XPackage.Element("Description").Value);
                    XElement updateFile = XPackage.Element("UpdateFile");
                    long uncompressed = long.Parse(updateFile.Attribute("UncompressedSize").Value);
                    long compressed = long.Parse(updateFile.Attribute("CompressedSize").Value);
                    package.SetMeta("UncompressedSize", IOUtil.GetBytesReadable(uncompressed));
                    package.SetMeta("CompressedSize", IOUtil.GetBytesReadable(compressed));
                    return package;
                }
            }
            return null;
        }
    }
}
