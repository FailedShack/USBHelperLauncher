using System.Reflection;
using HarmonyLib;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    class NvidiaDriverCheckPatch
    {
        static MethodBase TargetMethod()
        {
            return ReflectionHelper.MainModule.GetType("NusHelper.Emulators.Cemu")
                .GetMethod("CheckNvidiaDriverVersion", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }
}
