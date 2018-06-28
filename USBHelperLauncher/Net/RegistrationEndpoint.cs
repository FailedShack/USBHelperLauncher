using Fiddler;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace USBHelperLauncher.Net
{
    class RegistrationEndpoint : Endpoint
    {
        public RegistrationEndpoint() : base("registration.wiiuusbhelper.com") { }

        [Request("/getContributors.php")]
        public void GetContributors(Session oS)
        {
            oS.utilCreateResponseAndBypassServer();
            oS.utilSetResponseBody("Wii U USB Helper Launcher made by\n!FailedShack\n© 2018");
            Proxy.LogRequest(oS, this, "Sent credits.");
        }

        [Request("/verifyDonationKey.php")]
        public void GetVerificationResponse(Session oS)
        {
            var data = GetRequestData(oS);
            oS.utilCreateResponseAndBypassServer();
            string key = data.Get("key");
            JObject resp = new JObject(
                new JProperty("Accepted", true),
                new JProperty("Message", "This key can be used."),
                new JProperty("DonationDate", DateTime.Now.ToString("yyyy-MM-dd")),
                new JProperty("Email", WindowsIdentity.GetCurrent().Name.Split('\\').Last() + "@localhost"),
                new JProperty("DonatorKey", key));
            oS.utilSetResponseBody(resp.ToString());
            Proxy.LogRequest(oS, this, "Sent key validation.");
        }
    }
}
