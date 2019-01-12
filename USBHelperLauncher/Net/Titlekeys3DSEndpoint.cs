using Fiddler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USBHelperLauncher.Net
{
    class Titlekeys3DSEndpoint : Endpoint
    {
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
            oS.utilCreateResponseAndBypassServer();
            oS.oResponse["Content-Type"] = "application/json";

            try
            {
                JArray jsonArray = new JArray();
                string[] files = { "games3ds.json", "updates3ds.json", "dlcs3ds.json" };
                foreach (string filename in files)
                {
                    string path = Path.Combine(Program.GetLauncherPath(), "data", filename);
                    JArray contents = JArray.Parse(File.ReadAllText(path));
                    foreach (JObject title in contents)
                    {
                        JObject keyObject = new JObject();
                        keyObject["titleID"] = title["TitleId"].ToString().ToLower();
                        keyObject["name"] = title["Name"];
                        keyObject["region"] = title["Region"];
                        keyObject["titleKey"] = "";
                        keyObject["encTitleKey"] = "";
                        jsonArray.Add(keyObject);
                    }
                }

                string str = jsonArray.ToString();

                oS.utilSetResponseBody(str);
                Proxy.LogRequest(oS, this, "Sent custom response for request to /json");
            }
            catch
            {
                oS.utilSetResponseBody("[]");
                Proxy.LogRequest(oS, this, "Stubbed request to /json");
            }
        }
    }
}
