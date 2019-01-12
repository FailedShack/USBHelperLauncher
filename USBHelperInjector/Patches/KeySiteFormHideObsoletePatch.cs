using Harmony;
using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    class KeySiteFormHideObsoletePatch
    {
        static MethodBase TargetMethod()
        {
            return (from method in ReflectionHelper.FrmAskTicket.Type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    where method.GetParameters().Count() == 0
                    select method).FirstOrDefault();
        }

        static void Postfix(object __instance)
        {
            Control wiiUTextBox = (Control) ReflectionHelper.FrmAskTicket.TextBoxes[0].GetValue(__instance);
            Type radButtonType = wiiUTextBox.GetType().Assembly.GetType("Telerik.WinControls.UI.RadButton");
            Type radLabelType = wiiUTextBox.GetType().Assembly.GetType("Telerik.WinControls.UI.RadLabel");

            Control groupBox = wiiUTextBox.Parent;
            foreach (Control c in groupBox.Controls)
            {
                if (c.Location.Y > wiiUTextBox.Location.Y && c.GetType() != radButtonType)
                {
                    c.Visible = false;
                }
            }

            Control largestLabel = groupBox.Controls.OfType<Control>()
                .Where(c => c.GetType() == radLabelType)
                .OrderBy(c => c.Text.Length).Last();
            if (largestLabel.Text.Length > 30) // make sure to modify the correct label, it may not exist on older versions
            {
                largestLabel.Visible = false;
            }
        }
    }
}
