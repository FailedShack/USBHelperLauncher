using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USBHelperInjector.Pipes
{
    class DataStream
    {
        private const int BUFFER_SIZE = 2048;

        private Stream stream;

        public DataStream(Stream stream)
        {
            this.stream = stream;
        }

        public byte[] ReadByteArray()
        {
            byte[] sizeBuffer = new byte[4];
            stream.Read(sizeBuffer, 0, 4);
            int size = BitConverter.ToInt32(sizeBuffer, 0);
            using (var ms = new MemoryStream())
            {
                while (size > 0)
                {
                    int length = Math.Min(BUFFER_SIZE, size);
                    byte[] buffer = new byte[length];
                    stream.Read(buffer, 0, length);
                    ms.Write(buffer, 0, length);
                    size -= length;
                }
                ms.Position = 0;
                using (var uncompressed = new MemoryStream())
                {
                    using (var zip = new GZipStream(ms, CompressionMode.Decompress))
                    {
                        zip.CopyTo(uncompressed);
                    }
                    return uncompressed.ToArray();
                }
            }
        }

        public void WriteByteArray(byte[] bytes, CompressionLevel compression)
        {
            using (var ms = new MemoryStream(bytes))
            {
                using (var compressed = new MemoryStream())
                {
                    using (var zip = new GZipStream(compressed, compression, true))
                    {
                        ms.CopyTo(zip);
                    }
                    int size = (int) compressed.Position;
                    compressed.Position = 0;
                    byte[] sizeBuffer = BitConverter.GetBytes(size);
                    stream.Write(sizeBuffer, 0, 4);
                    while (size > 0)
                    {
                        int length = Math.Min(BUFFER_SIZE, size);
                        byte[] buffer = new byte[length];
                        compressed.Read(buffer, 0, length);
                        stream.Write(buffer, 0, length);
                        size -= length;
                    }
                }
            }
        }
    }
}
