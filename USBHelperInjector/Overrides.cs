using System.Net;

namespace USBHelperInjector
{
    public class Overrides
    {
        public static event OnDonationKeyChange OnSetDonationKey;

        public static event OnProxyChange OnSetProxy;

        public delegate void OnDonationKeyChange(string donationKey);

        public delegate void OnProxyChange(WebProxy proxy);

        public static string DonationKey { get; set; }

        public static WebProxy Proxy { get; set; }

        internal static void RaiseProxyChangeEvent(WebProxy proxy)
        {
            OnSetProxy?.Invoke(proxy);
        }

        internal static void RaiseDonationKeyChangeEvent(string donationKey)
        {
            OnSetDonationKey?.Invoke(donationKey);
        }
    }
}
