using HarmonyLib;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace USBHelperInjector.Patches
{
    // Raises exceptions for failed Tasks that have not been awaited for
    [HarmonyPatch]
    class TaskUnhandledExceptionPatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.FirstMethod(typeof(Task), method => method.Name == "ExecuteWithThreadLocal");
        }

        static void Postfix(Task __instance)
        {
            if (__instance.IsFaulted)
            {
                var ex = __instance.Exception;
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
            }
        }
    }
}
