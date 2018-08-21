using System.Xml.Linq;

namespace USBHelperInjector.Pipes.Packets
{
    public class OptionalPatchesPacket : ActionPacket
    {
        public bool DisableOptionalPatches { get; set; }

        public override void Deserialize(XDocument doc)
        {
            var packet = doc.Element("Packet");
            DisableOptionalPatches = bool.Parse(packet.Element("DisableOptionalPatches").Value);
        }

        public override void Execute()
        {
            Injector.ApplyPatches(DisableOptionalPatches);
        }

        public override XDocument Serialize()
        {
            XDocument doc = new XDocument();
            XElement packet = new XElement("Packet");
            packet.SetAttributeValue("Type", GetType().Name);
            packet.Add(new XElement("DisableOptionalPatches", DisableOptionalPatches));
            doc.Add(packet);
            return doc;
        }
    }
}
