using System.ServiceModel;
using System.Threading.Tasks;

namespace USBHelperInjector.Contracts
{
    [ServiceContract]
    public interface ILauncherService
    {
        [OperationContract]
        void SetKeySite(string site, string url);

        [OperationContract]
        void SendInjectorSettings();
    }
}
