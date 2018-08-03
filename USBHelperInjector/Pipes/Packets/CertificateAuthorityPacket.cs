using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace USBHelperInjector.Pipes.Packets
{
    public class CertificateAuthorityPacket : ActionPacket
    {
        public X509Certificate2 CaCert { get; set; }

        public override void Deserialize(XDocument doc)
        {
            var packet = doc.Element("Packet");
            string cert = packet.Element("CaCert").Value;
            CaCert = new X509Certificate2(Convert.FromBase64String(cert));
        }

        public override void Execute()
        {
            Injector.TrustCertificateAuthority(CaCert);
        }

        public override XDocument Serialize()
        {
            XDocument doc = new XDocument();
            XElement packet = new XElement("Packet");
            packet.SetAttributeValue("Type", GetType().Name);
            packet.Add(new XElement("CaCert", Convert.ToBase64String(CaCert.GetRawCertData())));
            doc.Add(packet);
            return doc;
        }
    }
}
