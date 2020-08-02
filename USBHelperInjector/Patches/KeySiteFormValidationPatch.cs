using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using USBHelperInjector.Patches.Attributes;

namespace USBHelperInjector.Patches
{
    [VersionSpecific("")] // hack: Exclude from automatic patching
    [HarmonyPatch]
    class KeySiteFormValidationPatch
    {
        private static readonly string[] sites = { "wiiu", "3ds" };
        private static string lastError; // Read using reflection

        static MethodBase TargetMethod()
        {
            return ReflectionHelper.FrmAskTicket.OkButtonHandler;
        }

        static bool Prefix(object __instance)
        {
            var textBoxes = ReflectionHelper.FrmAskTicket.TextBoxes.Select(x => (Control)x.GetValue(__instance)).ToArray();
            var textBoxWiiU = textBoxes[0];

            if (Regex.IsMatch(textBoxWiiU.Text.Trim(), "^[0-9a-fA-F]{32}$"))
            {
                DialogResult result = MessageBox.Show(
                    "patch.keysitevalidation.titlekey".Localize(),
                    "common:warning".Localize(),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2
                );
                if (result == DialogResult.No)
                {
                    textBoxWiiU.Text = string.Empty;
                    return false;
                }
            }

            var text = textBoxWiiU.Text.Trim();
            if (TryValidateKeySite(text, out lastError))
            {
                // Take the title key site as valid if the request succeeded.
                InjectorService.LauncherService.SetKeySite(sites[0], text);
                textBoxWiiU.Text = string.Format("{0}.titlekeys", sites[0]);

                // Always give a valid 3DS titlekey url if the Wii U url was valid.
                textBoxes[1].Text = string.Format("{0}.titlekeys", sites[1]);
            }
            else
            {
                // Tell the user the title key site is invalid.
                textBoxWiiU.Text = string.Empty;
            }

            Overrides.ForceKeySiteForm = false;
            return true;
        }

        static bool TryValidateKeySite(string text, out string lastError)
        {
            Uri baseUri;
            try
            {
                baseUri = new UriBuilder(text).Uri;
            }
            catch (UriFormatException e)
            {
                lastError = string.Format("patch.keysitevalidation.invaliduri".Localize(), e.Message);
                return false;
            }

            var uri = new Uri(baseUri, "json");
            using (var client = new HttpClient())
            {
                try
                {
                    var resp = client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).Result;
                    if (resp.IsSuccessStatusCode)
                    {
                        // Ensure that we actually got a JSON response
                        var content = resp.Content;
                        if (content.Headers.ContentType.MediaType != "application/json")
                        {
                            try
                            {
                                JToken.Parse(content.ReadAsStringAsync().Result);
                            }
                            catch (JsonReaderException e)
                            {
                                lastError = string.Format("patch.keysitevalidation.invalidjson".Localize(), e.Message);
                                return false;
                            }
                        }
                    }
                    else
                    {
                        lastError = GetCustomHttpErrorMessage((int)resp.StatusCode, resp.ReasonPhrase);
                        return false;
                    }
                }
                catch (HttpRequestException e)
                {
                    lastError = string.Format("patch.keysitevalidation.networkerror".Localize(), baseUri, e.Message);
                    return false;
                }
            }

            lastError = null;
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
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(KeySiteFormValidationPatch), "lastError"))
            };

            codes.InsertRange(showIndex, toInsert);

            return codes;
        }

        internal static string GetCustomHttpErrorMessage(int statusCode, string statusDescription)
        {
            return string.Format("patch.keysitevalidation.httperror".Localize(), statusCode, statusDescription);
        }
    }
}
