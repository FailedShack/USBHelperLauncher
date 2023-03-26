using HarmonyLib;
using System.Security.Cryptography;

namespace USBHelperInjector.Patches
{
    // Disable amazing "feature" that prevents us from using MD5 in .NET 2.0 - 4.7.2
    // See: https://github.com/microsoft/referencesource/blob/51cf7850defa8a17d815b4700b67116e3fa283c2/mscorlib/system/security/cryptography/md5cryptoserviceprovider.cs#L30
    [HarmonyPatch(typeof(CryptoConfig))]
    [HarmonyPatch("AllowOnlyFipsAlgorithms", MethodType.Getter)]
    class AllowOnlyFipsAlgorithmsPatch
    {
        static bool Prepare()
        {
            return CryptoConfig.AllowOnlyFipsAlgorithms;
        }

        static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }
}
