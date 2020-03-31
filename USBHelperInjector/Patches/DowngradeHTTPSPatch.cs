using HarmonyLib;
using System;
using System.Net;
using System.Reflection;

namespace USBHelperInjector.Patches
{
    // Cannot patch HttpWebRequest::SubmitRequest(System.Net.ServicePoint)
    // as it appears to be subject to some special JIT optimization.
    [HarmonyPatch(typeof(ServicePoint))]
    [HarmonyPatch("SubmitRequest")]
    [HarmonyPatch(new[] { typeof(HttpWebRequest), typeof(string) })]
    static class DowngradeHTTPSPatch
    {
        static readonly FieldInfo _Uri = AccessTools.Field(typeof(HttpWebRequest), "_Uri");
        static readonly MethodInfo UpdateHeaders = AccessTools.Method(typeof(HttpWebRequest), "UpdateHeaders");

        static bool Prepare()
        {
            return InjectorService.ForceHttp;
        }

        // Called for every request and any subsequent redirects
        static bool Prefix(HttpWebRequest request)
        {
            var uri = _Uri.GetValue(request) as Uri;
            if (uri.Scheme == Uri.UriSchemeHttps)
            {
                _Uri.SetValue(request, new UriBuilder(uri) { Scheme = Uri.UriSchemeHttp, Port = -1 }.Uri);
                request.Headers["X-Forwarded-Proto"] = Uri.UriSchemeHttps;
                UpdateHeaders.Invoke(request, null); // Update Host header
            }
            else
            {
                request.Headers.Remove("X-Forwarded-Proto");
            }
            return true;
        }
    }
}
