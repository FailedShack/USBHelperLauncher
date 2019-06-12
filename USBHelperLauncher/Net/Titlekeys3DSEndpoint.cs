using Fiddler;
using Newtonsoft.Json.Linq;
using System.IO;

namespace USBHelperLauncher.Net
{
    class Titlekeys3DSEndpoint : Endpoint
    {
        private static readonly string[] files = { "games3ds.json", "updates3ds.json", "dlcs3ds.json" };

        public Titlekeys3DSEndpoint() : base("3ds.titlekeys.gq") { }

        [Request("/rss")]
        public void GetRss(Session oS)
        {
            oS.utilCreateResponseAndBypassServer();
            oS.oResponse["Content-Type"] = "application/rss+xml";
            oS.utilSetResponseBody("");
            Proxy.LogRequest(oS, this, "Stubbed request to /rss");
        }

        [Request("/json")]
        public void GetJson(Session oS)
        {
            var titles = new JArray();
            foreach (string filename in files)
            {
                var path = Path.Combine(Program.GetLauncherPath(), "data", filename);
                var contents = JArray.Parse(File.ReadAllText(path));
                foreach (var title in contents)
                {
                    titles.Add(new JObject
                    {
                        ["titleID"] = title["TitleId"].ToString().ToLower(),
                        ["name"] = title["Name"],
                        ["region"] = title["Region"],
                        ["titleKey"] = "",
                        ["encTitleKey"] = ""
                    });
                }
            }

            oS.utilCreateResponseAndBypassServer();
            oS.oResponse["Content-Type"] = "application/json";
            oS.utilSetResponseBody(titles.ToString());
            Proxy.LogRequest(oS, this, "Sent custom response for request to /json");
        }
    }
}
