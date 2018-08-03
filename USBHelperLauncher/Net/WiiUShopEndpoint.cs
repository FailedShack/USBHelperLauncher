using Fiddler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace USBHelperLauncher.Net
{
    class WiiUShopEndpoint : Endpoint
    {
        public WiiUShopEndpoint() : base("ccs.cdn.wup.shop.nintendo.net") { }

        protected WiiUShopEndpoint(string hostName) : base(hostName) { }

        [Request("/*")]
        public void Get(Session oS)
        {
            oS.utilCreateResponseAndBypassServer();
            oS.oResponse.headers.SetStatus(307, "Redirect");
            oS.oResponse["Location"] = "http://ccs.cdn.c.shop.nintendowifi.net" + oS.PathAndQuery;
            Proxy.LogRequest(oS, this, "Redirecting to http://ccs.cdn.c.shop.nintendowifi.net" + oS.PathAndQuery);
        }
    }
}
