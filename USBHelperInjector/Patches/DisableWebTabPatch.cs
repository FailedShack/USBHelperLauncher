using Harmony;
using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    class DisableWebSearchTabPatch
    {
        static MethodBase TargetMethod()
        {
            return ReflectionHelper.NusGrabberForm.GetConstructor(Type.EmptyTypes);
        }

        static void Postfix(object __instance)
        {
            if (!InjectorService.DisableWebSearchTab) return;
            var fields = ReflectionHelper.NusGrabberForm.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            var toolWebField = (from field in fields
                                where field.FieldType.Name == "ToolWindow"
                                where ((Control)field.GetValue(__instance)).Name == "toolWeb"
                                select field).FirstOrDefault();
            var webBrowserField = (from field in fields
                                   where field.FieldType.Name == "WebBrowser"
                                   select field).FirstOrDefault();

            var closeMethod = toolWebField.FieldType.GetMethod("Close");
            var webBrowser = webBrowserField.GetValue(__instance);
            var url = webBrowserField.FieldType.GetProperty("Url");
            var allowNavigation = webBrowserField.FieldType.GetProperty("AllowNavigation");

            closeMethod.Invoke(toolWebField.GetValue(__instance), Type.EmptyTypes);
            url.SetValue(webBrowser, null); // Navigate to about:blank
            allowNavigation.SetValue(webBrowser, false); // Prevent further navigation
        }
    }
}
