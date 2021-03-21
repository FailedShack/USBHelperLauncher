using System.IO;
using System.Linq;
using HarmonyLib;
using System.Reflection;
using USBHelperInjector.Patches.Attributes;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    [Optional]
    class UnpackDirectoryPatch
    {
        static bool Prepare()
        {
            return InjectorService.SplitUnpackDirectories;
        }

        static MethodBase TargetMethod()
        {
            // v0.6.1.655: GClass30.method_16
            return (from method in ReflectionHelper.TitleTypes.Game.BaseType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    where method.ReturnType == typeof(bool)
                        && method.GetMethodBody().LocalVariables.Any(v => v.LocalType == typeof(DriveInfo))
                    select method).FirstOrDefault();
        }

        static void Prefix(object __instance, ref string __0)
        {
            // only modify paths to the configured unpack directory, not for builtin emulators
            if (__0 != ReflectionHelper.Settings.GetValue<string>("ExtractFolder"))
            {
                return;
            }
            var unpackDir = new[] { ReflectionHelper.TitleTypes.Update, ReflectionHelper.TitleTypes.Dlc }.Contains(__instance.GetType())
                ? "Updates and DLC"
                : "Base Games";
            __0 = Path.Combine(__0, unpackDir);
        }
    }
}
