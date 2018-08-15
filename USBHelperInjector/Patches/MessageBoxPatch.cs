using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace USBHelperInjector.Patches
{
    // Allows us to replace text displayed on message boxes
    // by intercepting them before they are displayed.
    [HarmonyPatch]
    internal class MessageBoxPatch
    {
        private static Dictionary<string, string> hashes = new Dictionary<string, string>();

        public static void Replace(string hash, string replacement)
        {
            hashes.Add(hash, replacement);
        }

        static MethodBase TargetMethod()
        {
            var assembly = Assembly.Load("Telerik.WinControls.UI");
            var type = assembly.GetType("Telerik.WinControls.RadMessageBox");
            return (from method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                    where method.Name == "ShowCore"
                    select method).FirstOrDefault();
        }

        static bool Prefix(ref string text)
        {
            string hash;
            using (var md5 = MD5.Create())
            {
                byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
                hash = Convert.ToBase64String(bytes);
            }
            if (hashes.ContainsKey(hash))
            {
                text = hashes[hash];
            }
            return true;
        }
    }
}
