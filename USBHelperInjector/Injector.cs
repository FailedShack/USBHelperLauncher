using Harmony;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using USBHelperInjector.Patches;
using USBHelperInjector.Pipes;
using USBHelperInjector.Properties;

namespace USBHelperInjector
{
    public class Injector
    {
        private static X509Certificate2 CaCert { get; set; }

        private static PipeServerListener server;

        public static void Init()
        {
            MessageBoxPatch.Replace("tp9+kFO7LOSD0AZ5zUBHrA==", Resources.Disclaimer);

            server = new PipeServerListener();
            server.Listen();
        }

        public static void ApplyPatches(bool disableOptional)
        {
            var harmony = HarmonyInstance.Create("me.failedshack.usbhelperinjector");
            var assembly = Assembly.GetExecutingAssembly();
            assembly.GetTypes().Do(type =>
            {
                var parentMethodInfos = type.GetHarmonyMethods();
                if (parentMethodInfos != null && parentMethodInfos.Count() > 0)
                {
                    var info = HarmonyMethod.Merge(parentMethodInfos);
                    var processor = new PatchProcessor(harmony, type, info);
                    if (!(disableOptional && Optional.IsOptional(type)))
                    {
                        processor.Patch();
                    }
                }
            });
        }

        public static void TerminateServer()
        {
            server.Shutdown();
        }

        // Should make the given CA certificate be trusted (currently only disables HTTPs validation)
        public static void TrustCertificateAuthority(X509Certificate2 cert)
        {
            CaCert = cert;
            ServicePointManager.ServerCertificateValidationCallback += ValidateServerCertificate;
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // Disable HTTPs validation
            return true;
        }
    }
}
