using HarmonyLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    class CemuUpdatePathPatch
    {
        private static readonly object MOVE_LOCK = new object();

        static MethodBase TargetMethod()
        {
            return AccessTools.Property(ReflectionHelper.MainModule.GetType("NusHelper.Emulators.Cemu"), "UpdatePath").GetGetMethod();
        }

        static void Postfix(object __instance, ref string __result)
        {
            var version = GetCemuVersion(__instance);
            if (version == null || version >= new Version(1, 15, 11))
            {
                __result = TryRewriteAndMoveDirectory(__result, "0005000e");
            }
        }

        internal static string TryRewriteAndMoveDirectory(string originalPath, string newTitleDirectory)
        {
            // rewrite path, remove "/aoc" suffix if it exists
            var components = originalPath.Split(Path.DirectorySeparatorChar).ToList();
            if (components.Last() == "aoc")
            {
                components.RemoveAt(components.Count - 1);
            }
            components[components.Count - 2] = newTitleDirectory;
            var newPath = string.Join(Path.DirectorySeparatorChar.ToString(), components);

            lock (MOVE_LOCK)
            {
                try
                {
                    if (Directory.Exists(originalPath) && !Directory.Exists(newPath))
                    {
                        Directory.CreateDirectory(Directory.GetParent(newPath).FullName);
                        Directory.Move(originalPath, newPath);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(string.Format("Unable to move directory \"{0}\" to \"{1}\":\n\n{2}", originalPath, newPath, e), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            return newPath;
        }

        internal static Version GetCemuVersion(object instance)
        {
            var executable = (string)AccessTools.Method(TargetMethod().DeclaringType, "GetExecutable").Invoke(instance, null);
            if (!File.Exists(executable))
            {
                return null;
            }
            var versionInfo = FileVersionInfo.GetVersionInfo(executable);
            return new Version(versionInfo.ProductMajorPart, versionInfo.ProductMinorPart, versionInfo.ProductBuildPart, versionInfo.ProductPrivatePart);
        }
    }

    [HarmonyPatch]
    class CemuDlcPathPatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Property(ReflectionHelper.MainModule.GetType("NusHelper.Emulators.Cemu"), "DlcPath").GetGetMethod();
        }

        static void Postfix(object __instance, ref string __result)
        {
            var version = CemuUpdatePathPatch.GetCemuVersion(__instance);
            if (version == null || version >= new Version(1, 15, 11))
            {
                __result = CemuUpdatePathPatch.TryRewriteAndMoveDirectory(__result, "0005000c");
            }
        }
    }
}
