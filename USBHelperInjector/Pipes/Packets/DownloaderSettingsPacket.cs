using System.Xml.Linq;

namespace USBHelperInjector.Pipes.Packets
{
    public class DownloaderSettingsPacket : ActionPacket
    {
        public int MaxRetries { get; set; }
        public int DelayBetweenRetries { get; set; }

        public override void Deserialize(XDocument doc)
        {
            var packet = doc.Element("Packet");
            MaxRetries = int.Parse(packet.Element("MaxRetries").Value);
            DelayBetweenRetries = int.Parse(packet.Element("DelayBetweenRetries").Value);
        }

        public override void Execute()
        {
            Overrides.MaxRetries = MaxRetries;
            Overrides.DelayBetweenRetries = DelayBetweenRetries;
        }

        public override XDocument Serialize()
        {
            XDocument doc = new XDocument();
            XElement packet = new XElement("Packet");
            packet.SetAttributeValue("Type", GetType().Name);
            packet.Add
            (
                new XElement("MaxRetries", MaxRetries),
                new XElement("DelayBetweenRetries", DelayBetweenRetries)
            );
            doc.Add(packet);
            return doc;
        }
    }
}
