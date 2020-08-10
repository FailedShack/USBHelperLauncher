using System;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace USBHelperInjector.IPC
{
    public class IPCUtil
    {
        public static ServiceHost CreateService(IPCType ipcType, Type serviceType, Type contractType, out Uri uri)
        {
            switch (ipcType)
            {
                case IPCType.NamedPipe:
                    return CreateNamedPipeService(serviceType, contractType, out uri);
                case IPCType.TCP:
                    return CreateTcpService(serviceType, contractType, out uri);
                default:
                    throw new InvalidEnumArgumentException(nameof(ipcType), (int)ipcType, ipcType.GetType());
            }
        }

        public static ServiceHost CreateNamedPipeService(Type serviceType, Type contractType, out Uri uri)
        {
            var guid = Guid.NewGuid().ToString("D");
            var host = new ServiceHost(serviceType, new Uri($"net.pipe://localhost/{guid}"));
            host.AddServiceEndpoint(contractType, new NetNamedPipeBinding(""), "");
            host.Open();
            uri = host.ChannelDispatchers.First().Listener.Uri;
            return host;
        }

        public static ServiceHost CreateTcpService(Type serviceType, Type contractType, out Uri uri)
        {
            var localUri = new Uri("net.tcp://127.0.0.1");
            var host = new ServiceHost(serviceType, localUri);
            var binding = new NetTcpBinding("")
            {
                Security =
                {
                    Mode = SecurityMode.None
                }
            };
            var endpoint = host.AddServiceEndpoint(contractType, binding, "", localUri);
            endpoint.ListenUriMode = ListenUriMode.Unique;
            host.Open();
            uri = host.ChannelDispatchers.First().Listener.Uri;
            return host;
        }


        public static TContract CreateChannel<TContract>(IPCType ipcType, string address)
        {
            Binding binding;
            switch (ipcType)
            {
                case IPCType.NamedPipe:
                    binding = new NetNamedPipeBinding("");
                    break;
                case IPCType.TCP:
                    binding = new NetTcpBinding("")
                    {
                        Security =
                        {
                            Mode = SecurityMode.None
                        }
                    };
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(ipcType), (int)ipcType, ipcType.GetType());
            }

            var factory = new ChannelFactory<TContract>(binding, address);
            return factory.CreateChannel();
        }
    }
}
