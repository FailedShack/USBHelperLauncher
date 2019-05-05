using Harmony;
using System.Collections;
using System.Linq;
using System.Reflection;
using USBHelperInjector.Patches.Attributes;

namespace USBHelperInjector.Patches
{
    [Optional]
    [HarmonyPatch]
    internal class SpeedChartPatch
    {
        static MethodBase TargetMethod()
        {
            return (from method in ReflectionHelper.NusGrabberForm.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    where method.ReturnType == typeof(void) && method.GetParameters().Length == 0
                    && method.GetMethodBody().LocalVariables.Count == 1 && method.GetMethodBody().LocalVariables[0].LocalType == typeof(System.TimeSpan)
                    select method).FirstOrDefault();
        }

        static void Postfix(object __instance)
        {
            FieldInfo speedChartField = (from field in __instance.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                                         where field.FieldType == Assembly.Load("LiveCharts.WinForms").GetType("LiveCharts.WinForms.CartesianChart")
                                         select field).FirstOrDefault();

            var speedChart = speedChartField.GetValue(__instance);

            // enable animations for grid
            IList axisXCollection = (IList)AccessTools.Property(speedChart.GetType(), "AxisX").GetValue(speedChart);
            var axis = axisXCollection[axisXCollection.Count - 1];
            AccessTools.Property(axis.GetType(), "DisableAnimations").SetValue(axis, false);

            // set step size
            var axisSeparator = AccessTools.Property(axis.GetType(), "Separator").GetValue(axis);
            var stepValue = AccessTools.Property(axisSeparator.GetType(), "Step").GetValue(axisSeparator);
            AccessTools.Property(axis.GetType(), "Unit").SetValue(axis, stepValue);
        }
    }
}
