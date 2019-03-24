using Fiddler;

namespace USBHelperLauncher.Net
{
    class CloudEndpoint : Endpoint
    {
        public CloudEndpoint() : base("cloud.wiiuusbhelper.com") { }

        [Request("/mods/list_mods.php")]
        public void GetMods(Session oS)
        {
            oS.utilCreateResponseAndBypassServer();
            oS.oResponse["Content-Type"] = "application/json";
            oS.utilSetResponseBody("[]");
            Proxy.LogRequest(oS, this, "Stubbed request to /mods/list_mods.php");
        }
    }
}
