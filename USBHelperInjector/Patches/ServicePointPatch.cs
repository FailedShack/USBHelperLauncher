using Harmony;
using System;
using System.Linq;
using System.Net;
using System.Reflection;

namespace USBHelperInjector.Patches
{
    // Forces a Proxy for all web requests
    [HarmonyPatch]
    internal class ServicePointPatch
    {
        // This is intended to get FindServicePoint(Uri, IWebProxy, ProxyChain, HttpAbortDelegate, Int32)
        // without having to somehow reference all parameter types (some of which are not visible without reflection).
        static MethodBase TargetMethod()
        {
            return (from method in typeof(ServicePointManager).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                    where method.Name == "FindServicePoint"
                    && method.GetParameters().Count() == 5
                    select method).FirstOrDefault();
        }

        static bool Prefix(Uri address, ref IWebProxy proxy)
        {
            if ((address.Scheme == "http" || address.Scheme == "https") && Overrides.Proxy != null)
            {
                proxy = Overrides.Proxy;
            }
            return true;
        }
    }
}
