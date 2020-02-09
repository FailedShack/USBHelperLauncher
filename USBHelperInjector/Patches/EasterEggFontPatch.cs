using HarmonyLib;
using System;
using System.Drawing;
using System.Reflection;
using USBHelperInjector.Patches.Attributes;

namespace USBHelperInjector.Patches
{
    [Optional]
    [HarmonyPatch]
    class EasterEggFontPatch
    {
        private static string fontName;

        static EasterEggFontPatch()
        {
            if (DateTime.Now.Month == 4 && DateTime.Now.Day == 1)
            {
                // check if the font exists on the system
                var name = "Comic Sans MS";
                var font = new Font(name, 12);
                if (font.Name == name)
                {
                    fontName = name;
                }
            }
        }

        static MethodBase TargetMethod()
        {
            return typeof(FontFamily).GetMethod("CreateFontFamily", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        static void Prefix(ref object __0)
        {
            if (fontName != null)
            {
                __0 = fontName;
            }
        }
    }
}
