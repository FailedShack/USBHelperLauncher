using Harmony;
using System;
using System.IO;
using System.Reflection;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch(typeof(Environment))]
    [HarmonyPatch("GetFolderPath")]
    [HarmonyPatch(new Type[] { typeof(Environment.SpecialFolder) })]
    class EnvironmentFolderPatch
    {
        static bool Prefix(ref string __result)
        {
            if (!InjectorService.Portable) return true;
            __result = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "userdata");
            Directory.CreateDirectory(__result);
            return false;
        }
    }

    [HarmonyPatch]
    class ClientConfigPathsPatch
    {
        static MethodBase TargetMethod()
        {
            var type = Assembly.GetAssembly(typeof(System.Configuration.Configuration)).GetType("System.Configuration.ClientConfigPaths");
            return type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(string), typeof(bool) }, null);
        }

        static bool Prefix(ref string ____localConfigDirectory, ref string ____localConfigFilename)
        {
            if (!InjectorService.Portable) return true;
            ____localConfigDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "userdata");
            ____localConfigFilename = Path.Combine(____localConfigDirectory, "user.config");
            return false;
        }
    }
}
