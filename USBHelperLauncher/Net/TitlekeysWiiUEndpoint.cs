using Fiddler;
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
