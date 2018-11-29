using Fiddler;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using USBHelperLauncher.Configuration;

namespace USBHelperLauncher.Net
{
    class TitlekeysWiiUEndpoint : Endpoint
    {
        public TitlekeysWiiUEndpoint() : base("wiiu.titlekeys.gq") { }

        [Request("/*")]
        public void Get(Session oS)
        {
            if (Settings.TitleKeys == null || !Settings.TitleKeys.ContainsKey("wiiu"))
            {
                return;
            }
            oS.utilCreateResponseAndBypassServer();
            oS.oResponse.headers.SetStatus(307, "Redirect");
            var path = Regex.Replace(oS.PathAndQuery, @"^\/*", "");
            var baseUri = new UriBuilder(Settings.TitleKeys["wiiu"]).Uri;
            var url = new Uri(baseUri, path).ToString();
            oS.oResponse["Location"] = url;
            Proxy.LogRequest(oS, this, "Redirecting to " + url);
        }
    }
}
