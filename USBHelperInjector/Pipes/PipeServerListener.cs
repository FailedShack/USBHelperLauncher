using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using USBHelperInjector.Pipes.Packets;

namespace USBHelperInjector.Pipes
{
    public class PipeServerListener
    {
        private bool shutdown = false;

        public void Listen()
        {
            new Thread(() =>
            {
                while (!shutdown)
                {
                    using (var server = new NamedPipeServerStream("USBHelperLauncher", PipeDirection.InOut))
                    {
                        server.WaitForConnection();
                        DataStream ds = new DataStream(server);
                        byte[] data = ds.ReadByteArray();
                        string xml = Encoding.UTF8.GetString(data);
                        XDocument doc = XDocument.Parse(xml);
                        bool success = HandlePacket(doc);
                        server.WriteByte(Convert.ToByte(success));
                    }
                }
            }).Start();
        }

        public bool HandlePacket(XDocument doc)
        {
            Type type = ActionPacket.ByName(doc.Element("Packet").Attribute("Type").Value);
            if (type == null)
            {
                return false;
            }
            var packet = (ActionPacket)Activator.CreateInstance(type);
            try
            {
                packet.Deserialize(doc);
            }
            catch
            {
                return false;
            }
            packet.Execute();
            return true;
        }

        public void Shutdown()
        {
            shutdown = true;
        }
    }
}
