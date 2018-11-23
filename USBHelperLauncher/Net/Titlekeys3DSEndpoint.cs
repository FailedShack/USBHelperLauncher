using Fiddler;
using System;
using System.Collections.Generic;
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
            oS.utilSetResponseBody("[]");
            Proxy.LogRequest(oS, this, "Stubbed request to /json");
        }
    }
}
