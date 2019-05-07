using Harmony;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows.Forms;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    class KeySiteFormValidationPatch
    {
        private static readonly string[] sites = { "wiiu", "3ds" };

        private static string errorMessage;

        static MethodBase TargetMethod()
        {
            return ReflectionHelper.FrmAskTicket.OkButtonHandler;
        }

        static bool Prefix(object __instance)
        {
            var textBoxes = ReflectionHelper.FrmAskTicket.TextBoxes;
            var textBoxWiiU = (Control)textBoxes[0].GetValue(__instance);
            var wiiUUrl = textBoxWiiU.Text;

            var client = new HttpClient();
            try
            {
                var baseUri = new UriBuilder(textBoxWiiU.Text).Uri;
                var uri = new Uri(baseUri, "json");
                var resp = client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).Result;
                if (resp.IsSuccessStatusCode)
                {
                    // Take the title key site as valid if no exception occurred.
                    InjectorService.LauncherService.SetKeySite(sites[0], textBoxWiiU.Text);
                    textBoxWiiU.Text = string.Format("{0}.titlekeys", sites[0]);

                    // Always give Wii U USB Helper a valid 3DS titlekey url if the WiiU url is valid
                    var textBox3DS = (Control)textBoxes[1].GetValue(__instance);
                    textBox3DS.Text = string.Format("{0}.titlekeys", sites[1]);

                    errorMessage = null;
                }
                else
                {
                    // Tell the user the title key site is invalid.
                    textBoxWiiU.Text = string.Empty;
                    errorMessage = GetCustomHttpErrorMessage((int)resp.StatusCode, resp.ReasonPhrase);
                }
            }
            catch (Exception e)
            {
                // Tell the user the title key site is invalid.
                textBoxWiiU.Text = string.Empty;
                errorMessage = e.ToString();
            }

            if (errorMessage != null)
            {
                errorMessage = string.Format("An error occurred while trying to reach {0}:\n\n{1}", wiiUUrl, errorMessage);
            }

            Overrides.ForceKeySiteForm = false;
            return true;
        }

        static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            var showIndex = codes.FindIndex(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "Show");
            if (showIndex == -1)
            {
                return codes;
            }

            var toInsert = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(KeySiteFormValidationPatch), "errorMessage"))
            };

            codes.InsertRange(showIndex, toInsert);

            return codes;
        }

        internal static string GetCustomHttpErrorMessage(int statusCode, string statusDescription)
        {
            return string.Format("Remote server replied with status: ({0}) {1}", statusCode, statusDescription);
        }
    }
}
