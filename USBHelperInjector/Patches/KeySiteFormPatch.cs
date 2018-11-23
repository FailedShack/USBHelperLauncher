using Harmony;
using Harmony.ILCopying;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    class KeySiteFormPatch
    {
        private static readonly string[] sites = { "wiiu", "3ds", "wii" };

        private static List<FieldInfo> textBoxes;

        static MethodBase TargetMethod()
        {
            var methods = (from type in ReflectionHelper.Types
                           from prop in type.GetProperties()
                           where prop.Name == "FileLocationWiiU"
                           from method in prop.DeclaringType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                           select method).GetEnumerator();

            while (methods.MoveNext())
            {
                var instructions = MethodBodyReader.GetInstructions(null, methods.Current);
                if (instructions.Any(x => x.opcode == OpCodes.Call && ((MethodInfo)x.operand).Name == "set_FileLocation3DS"))
                {
                    textBoxes = (from instruction in instructions
                                 where instruction.opcode == OpCodes.Ldfld
                                 let field = (FieldInfo)instruction.operand
                                 where field.FieldType.Name == "RadTextBox"
                                 select field).ToList();

                    if (textBoxes.Count > 0)
                    {
                        break;
                    }
                }
            }

            return methods.Current;
        }

        static bool Prefix(object __instance)
        {
            var config = File.ReadAllText("conf.json");
            var json = JObject.Parse(config);
            var urls = new JObject();
            var textProperty = textBoxes[0].FieldType.GetProperty("Text");
            var client = new HttpClient();
            for (int i = 0; i < textBoxes.Count; i++)
            {
                var textBox = textBoxes[i].GetValue(__instance);
                var text = (string)textProperty.GetValue(textBox);
                try
                {
                    var baseUri = new UriBuilder(text).Uri;
                    var uri = new Uri(baseUri, "json");
                    var resp = client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri)).Result;
                    resp.EnsureSuccessStatusCode();
                }
                catch
                {
                    // Tell the user the title key site is invalid.
                    textProperty.SetValue(textBox, string.Empty);
                    continue;
                }
                // Take the title key site as valid.
                textProperty.SetValue(textBox, string.Format("{0}.titlekeys", sites[i]));
                urls.Add(new JProperty(sites[i], text));
            }

            // Save to config
            json["Launcher"]["TitleKeys"] = urls;
            File.WriteAllText("conf.json", json.ToString());
            return true;
        }
    }
}
