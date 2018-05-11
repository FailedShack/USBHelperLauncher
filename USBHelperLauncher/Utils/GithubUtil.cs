using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Threading.Tasks;

namespace USBHelperLauncher.Utils
{
    class GithubUtil
    {
        public static async Task<JObject> GetRelease(string user, string repo, string release)
        {
            string json;
            using (WebClient client = new WebClient())
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                client.Headers.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2;)");
                try
                {
                    json = await client.DownloadStringTaskAsync(String.Format("https://api.github.com/repos/{0}/{1}/releases/{2}", user, repo, release));
                }
                catch (WebException)
                {
                    throw;
                }
            }
            return JObject.Parse(json);
        }
    }
}
