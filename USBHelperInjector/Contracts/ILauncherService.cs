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
        string SetCustomProxy(string address, string username, string password);

        [OperationContract]
        void SendInjectorSettings();
    }
}
