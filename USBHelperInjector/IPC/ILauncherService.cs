using System;
using System.ServiceModel;

namespace USBHelperInjector.IPC
{
    [ServiceContract]
    public interface ILauncherService
    {
        [OperationContract]
        void SetKeySite(string site, string url);

        [OperationContract]
        void SendInjectorSettings(Uri uri);
    }
}
