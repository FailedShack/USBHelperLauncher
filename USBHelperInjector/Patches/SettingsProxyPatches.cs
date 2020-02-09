﻿using HarmonyLib;
using System.Net;
using System.Reflection;

namespace USBHelperInjector.Patches
{
    [HarmonyPatch]
    internal class SettingsProxyGet
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.DeclaredProperty(ReflectionHelper.NusGrabberForm.Type, "Proxy").GetGetMethod(true);
        }

        static bool Prefix(ref WebProxy __result)
        {
            if (Overrides.Proxy != null)
            {
                __result = Overrides.Proxy;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch]
    internal class SettingsProxySet
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.DeclaredProperty(ReflectionHelper.NusGrabberForm.Type, "Proxy").GetSetMethod(true);
        }

        static bool Prefix(ref WebProxy value)
        {
            Overrides.RaiseProxyChangeEvent(value);
            return true;
        }
    }
}
