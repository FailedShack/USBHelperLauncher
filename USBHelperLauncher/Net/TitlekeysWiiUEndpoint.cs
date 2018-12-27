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
            if (Settings.TitleKeys.ContainsKey("wiiu"))
            {
                Proxy.RedirectRequest(oS, this, Settings.TitleKeys["wiiu"]);
            }
        }
    }
}
