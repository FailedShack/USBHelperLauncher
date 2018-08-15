using Harmony;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    internal class SearchPatch
    {
        // Finds the method called by the search engine to find matching strings,
        // we patch it in order to fix cases such as 'Pokemon' vs 'Pokémon'.
        static MethodBase TargetMethod()
        {
            return (from type in ReflectionHelper.NusGrabberForm.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance)
                    where type.GetFields().Length == 1 && type.GetFields()[0].FieldType == typeof(string)
                    from method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    where method.ReturnType == typeof(bool)
                    && method.GetParameters().Length == 1
                    && method.GetParameters()[0].ParameterType == typeof(string)
                    select method).FirstOrDefault();
        }

        static bool Prefix(ref bool __result, string __0, string ___0)
        {
            __result = CultureInfo.CurrentCulture.CompareInfo.IndexOf(__0, ___0, CompareOptions.IgnoreNonSpace) > -1;
            return false;
        }
    }
}
