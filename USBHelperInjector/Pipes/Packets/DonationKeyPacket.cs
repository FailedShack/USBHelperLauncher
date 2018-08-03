using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace USBHelperInjector.Pipes.Packets
{
    public class DonationKeyPacket : ActionPacket
    {
        public string DonationKey { get; set; }

        public override void Deserialize(XDocument doc)
        {
            var packet = doc.Element("Packet");
            DonationKey = packet.Element("DonationKey").Value;
        }

        public override void Execute()
        {
            Overrides.DonationKey = DonationKey;
        }

        public override XDocument Serialize()
        {
            XDocument doc = new XDocument();
            XElement packet = new XElement("Packet");
            packet.SetAttributeValue("Type", GetType().Name);
            packet.Add(new XElement("DonationKey", DonationKey));
            doc.Add(packet);
            return doc;
        }
    }
}
