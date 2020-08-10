using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Threading.Tasks;

namespace USBHelperLauncher.Utils
{
    class SourceforgeUtil
    {
        public static async Task<JToken> GetLatestRelease(string project, string platform)
        {
            string json;
            using (WebClient client = new WebClient())
            {
                ServicePointManager.Expect100Continue = true;
                client.Headers.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2;)");
                try
                {
                    json = await client.DownloadStringTaskAsync(String.Format("https://sourceforge.net/projects/{0}/best_release.json", project));
                }
                catch (WebException)
                {
                    throw;
                }
            }
            JObject obj = JObject.Parse(json);
            JToken platforms = obj["platform_releases"];
            foreach (JProperty asset in platforms.Children())
            {
                if (asset.Name == platform)
                {
                    return platforms[platform];
                }
            }
            return null;
        }
    }
}
