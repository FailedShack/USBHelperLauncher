using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace USBHelperInjector.Pipes.Packets
{
    public abstract class ActionPacket
    {
        private static readonly Type[] packets = new Type[]
        {
            typeof(DonationKeyPacket),
            typeof(CertificateAuthorityPacket),
            typeof(ProxyPacket),
            typeof(TerminationPacket),
            typeof(DownloaderSettingsPacket),
            typeof(OptionalPatchesPacket)
        };

        public abstract void Execute();

        public abstract XDocument Serialize();

        public abstract void Deserialize(XDocument doc);

        public static Type ByName(string name)
        {
            return (from packet in packets where packet.Name == name select packet).FirstOrDefault();
        }
    }
}
