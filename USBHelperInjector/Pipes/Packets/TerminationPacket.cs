using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace USBHelperInjector.Pipes.Packets
{
    public class TerminationPacket : ActionPacket
    {
        public override void Deserialize(XDocument doc) { }

        public override void Execute()
        {
            Injector.TerminateServer();
        }

        public override XDocument Serialize()
        {
            XDocument doc = new XDocument();
            XElement packet = new XElement("Packet");
            packet.SetAttributeValue("Type", GetType().Name);
            doc.Add(packet);
            return doc;
        }
    }
}
