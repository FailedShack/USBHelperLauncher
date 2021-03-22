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
        void SetPlayMusic(bool playMusic);

        [OperationContract]
        void SendInjectorSettings(Uri uri);
    }
}
