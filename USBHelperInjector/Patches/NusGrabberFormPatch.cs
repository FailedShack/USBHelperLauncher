using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    class NusGrabberFormPatch
    {
        public static event EventHandler Shown;
        public static event FormClosingEventHandler FormClosing;

        static MethodBase TargetMethod()
        {
            return ReflectionHelper.NusGrabberForm.Constructor;
        }

        static void Postfix(Form __instance)
        {
            var fields = ReflectionHelper.NusGrabberForm.Type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            var tabFields = fields.Where(field => field.FieldType.Name == "ToolWindow").ToList();
            var closeMethod = tabFields[0].FieldType.GetMethod("Close");

            foreach (var field in tabFields)
            {
                var tab = (Control)field.GetValue(__instance);
                if (InjectorService.DisableTabs.Contains(tab.Name))
                    closeMethod.Invoke(tab, Type.EmptyTypes);
            }

            // Fixes window missing from taskbar when UseShellExecute = false
            __instance.Load += (sender, e) =>
            {
                __instance.ShowInTaskbar = false;
                __instance.ShowInTaskbar = true;
            };
            __instance.Shown += (sender, e) => Shown?.Invoke(sender, e);
            __instance.FormClosing += (sender, e) => FormClosing?.Invoke(sender, e);
        }
    }
}
