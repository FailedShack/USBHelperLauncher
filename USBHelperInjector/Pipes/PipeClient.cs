using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using USBHelperInjector.Pipes.Packets;

namespace USBHelperInjector.Pipes
{
    public class PipeClient
    {
        public bool SendPacket(ActionPacket packet)
        {
            using (var client = new NamedPipeClientStream("USBHelperLauncher"))
            {
                client.Connect();
                DataStream ds = new DataStream(client);
                XDocument doc = packet.Serialize();
                byte[] bytes = Encoding.UTF8.GetBytes(doc.ToString());
                ds.WriteByteArray(bytes, CompressionLevel.Fastest);
                client.Flush();
                byte result = (byte) client.ReadByte();
                client.Close();
                return BitConverter.ToBoolean(new byte[] { result }, 0);
            }
        }
    }
}
