using System.ServiceModel;

namespace USBHelperInjector.Contracts
{
    [ServiceContract]
    public interface IInjectorService
    {
        [OperationContract]
        void TrustCertificateAuthority(byte[] rawCertData);

        [OperationContract]
        void ForceKeySiteForm();

        [OperationContract]
        void SetDonationKey(string donationKey);

        [OperationContract]
        void SetPublicKey(string publicKey);

        [OperationContract]
        void SetDownloaderMaxRetries(int maxRetries);

        [OperationContract]
        void SetDownloaderRetryDelay(int delay);

        [OperationContract]
        void SetProxy(string address);

        [OperationContract]
        void SetDisableOptionalPatches(bool disableOptional);

        [OperationContract]
        void SetHelperVersion(string helperVersion);

        [OperationContract]
        void SetPortable(bool portable);

        [OperationContract]
        void SetForceHttp(bool forceHttp);

        [OperationContract]
        void SetDisableWebSearchTab(bool disableWebSearchTab);

        [OperationContract]
        void SetLocaleFile(string localeFile);
    }
}
