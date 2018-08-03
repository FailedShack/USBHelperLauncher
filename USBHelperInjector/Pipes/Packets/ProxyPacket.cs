using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace USBHelperInjector.Pipes.Packets
{
    public class ProxyPacket : ActionPacket
    {
        public WebProxy Proxy { get; set; }

        public override void Deserialize(XDocument doc)
        {
            var packet = doc.Element("Packet");
            Proxy = new WebProxy(packet.Element("Proxy").Value);
        }

        public override void Execute()
        {
            Overrides.Proxy = Proxy;
        }

        public override XDocument Serialize()
        {
            XDocument doc = new XDocument();
            XElement packet = new XElement("Packet");
            packet.SetAttributeValue("Type", GetType().Name);
            packet.Add(new XElement("Proxy", Proxy.Address.ToString()));
            doc.Add(packet);
            return doc;
        }
    }
}
