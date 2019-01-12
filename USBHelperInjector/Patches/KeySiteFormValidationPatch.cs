using Harmony;
using System;
using System.Net.Http;
using System.Reflection;
using System.Windows.Forms;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    class KeySiteFormValidationPatch
    {
        private static readonly string[] sites = { "wiiu", "3ds" };

        static MethodBase TargetMethod()
        {
            return ReflectionHelper.FrmAskTicket.OkButtonHandler;
        }

        static bool Prefix(object __instance)
        {
            var textBoxes = ReflectionHelper.FrmAskTicket.TextBoxes;
            var textBoxWiiU = (Control)textBoxes[0].GetValue(__instance);

            var client = new HttpClient();
            try
            {
                var baseUri = new UriBuilder(textBoxWiiU.Text).Uri;
                var uri = new Uri(baseUri, "json");
                var resp = client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).Result;
                resp.EnsureSuccessStatusCode();

                // Take the title key site as valid if no exception occurred.
                InjectorService.LauncherService.SetKeySite(sites[0], textBoxWiiU.Text);
                textBoxWiiU.Text = string.Format("{0}.titlekeys", sites[0]);

                // always give Wii U USB Helper a valid 3DS titlekey url if the WiiU url is valid
                var textBox3DS = (Control)textBoxes[1].GetValue(__instance);
                textBox3DS.Text = string.Format("{0}.titlekeys", sites[1]);
            }
            catch
            {
                // Tell the user the title key site is invalid.
                textBoxWiiU.Text = string.Empty;
            }

            Overrides.ForceKeySiteForm = false;
            return true;
        }
    }
}
