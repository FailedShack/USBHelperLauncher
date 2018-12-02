using System.ServiceModel;

namespace USBHelperInjector.Contracts
{
    [ServiceContract]
    public interface IInjectorService
    {
        [OperationContract]
        void TrustCertificateAuthority(byte[] rawCertData);

        [OperationContract]
        void SetDonationKey(string donationKey);

        [OperationContract]
        void SetDownloaderMaxRetries(int maxRetries);

        [OperationContract]
        void SetDownloaderRetryDelay(int delay);

        [OperationContract]
        void ApplyPatches(bool disableOptional);

        [OperationContract]
        void SetProxy(string address);
    }
}
