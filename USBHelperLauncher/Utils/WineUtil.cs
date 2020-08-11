using System.Runtime.InteropServices;

namespace USBHelperLauncher.Utils
{
    static class WineUtil
    {
        // see: https://wiki.winehq.org/Developer_FAQ#How_can_I_detect_Wine.3F
        [DllImport("ntdll.dll")]
        [return: MarshalAs(UnmanagedType.LPStr)]
        private static extern string wine_get_version();

        public static string TryGetVersion()
        {
            try
            {
                return wine_get_version();
            }
            catch
            {
                return null;
            }
        }

        public static bool IsRunningInWine() => TryGetVersion() != null;
    }
}
