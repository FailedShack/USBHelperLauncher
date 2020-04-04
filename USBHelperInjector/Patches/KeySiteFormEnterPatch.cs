using HarmonyLib;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using USBHelperInjector.Patches.Attributes;

namespace USBHelperInjector.Patches
{
    [Optional]
    [HarmonyPatch]
    class KeySiteFormEnterPatch
    {
        internal static MethodBase TargetMethod()
        {
            return KeySiteFormHideObsoletePatch.TargetMethod();
        }

        internal static void Postfix(Form __instance)
        {
            var propEvents = typeof(Control).GetProperty("Events", BindingFlags.NonPublic | BindingFlags.Instance);
            var eventClick = typeof(Control).GetField("EventClick", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var okButton = (from field in ReflectionHelper.FrmAskTicket.Type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                            where field.FieldType.Name == "RadButton"
                            let button = (IButtonControl) field.GetValue(__instance)
                            let lst = (EventHandlerList) propEvents.GetValue(button)
                            let handler = lst[eventClick]
                            where handler?.Method == ReflectionHelper.FrmAskTicket.OkButtonHandler
                            select button).FirstOrDefault();
            __instance.AcceptButton = okButton;
            InjectorService.Harmony.CreateClassProcessor(typeof(KeySiteFormValidationPatch)).Patch();
        }
    }
}
