using HarmonyLib;
using System.Linq;
using System.Reflection;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    class DisableWebSearchTabPatch
    {
        static MethodBase TargetMethod()
        {
            return ReflectionHelper.NusGrabberForm.Constructor;
        }

        static void Postfix(object __instance)
        {
            if (!InjectorService.DisableTabs.Contains("toolWeb")) return;
            var fields = ReflectionHelper.NusGrabberForm.Type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            var webBrowserField = (from field in fields
                                   where field.FieldType.Name == "WebBrowser"
                                   select field).FirstOrDefault();

            var webBrowser = webBrowserField.GetValue(__instance);
            var url = webBrowserField.FieldType.GetProperty("Url");
            var allowNavigation = webBrowserField.FieldType.GetProperty("AllowNavigation");

            url.SetValue(webBrowser, null); // Navigate to about:blank
            allowNavigation.SetValue(webBrowser, false); // Prevent further navigation
        }
    }
}
