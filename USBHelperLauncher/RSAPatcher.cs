using System;
using System.IO;
using System.Linq;
using System.Text;

namespace USBHelperLauncher
{
    class RSAPatcher
    {

        // We're looking for '<RSAKeyValue>'
        private static readonly byte[] pattern = new byte[] { 60, 82, 83, 65, 75, 101, 121, 86, 97, 108, 117, 101, 62 };

        private readonly string path;
        private long? position;

        public RSAPatcher(string path)
        {
            this.path = path;
        }

        public string ReadPublicKey()
        {
            long pos = FindPosition();
            using (FileStream fs = File.OpenRead(path))
            {
                fs.Seek(pos, SeekOrigin.Begin);
                byte[] buffer = new byte[425];
                fs.Read(buffer, 0, buffer.Length);
                return Encoding.UTF8.GetString(buffer);
            }
        }

        public void SetPublicKey(string xml)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(xml);
            if (bytes.Length != 425)
            {
                throw new ArgumentException("Invalid string length.");
            }
            long pos = FindPosition();
            using (FileStream fs = File.OpenWrite(path))
            {
                fs.Seek(pos, SeekOrigin.Begin);
                fs.Write(bytes, 0, bytes.Length);
            }
        }

        private long FindPosition()
        {
            if (!position.HasValue)
            {
                using (FileStream fs = File.OpenRead(path))
                {
                    position = FindPosition(fs, pattern);
                }
            }
            return position.Value;
        }

        private long FindPosition(Stream stream, byte[] byteSequence)
        {
            if (byteSequence.Length > stream.Length)
                return -1;

            byte[] buffer = new byte[byteSequence.Length];

            using (BufferedStream bufStream = new BufferedStream(stream, byteSequence.Length))
            {
                int i;
                while ((i = bufStream.Read(buffer, 0, byteSequence.Length)) == byteSequence.Length)
                {
                    if (byteSequence.SequenceEqual(buffer))
                        return bufStream.Position - byteSequence.Length;
                    else
                        bufStream.Position -= byteSequence.Length - PadLeftSequence(buffer, byteSequence);
                }
            }

            return -1;
        }

        private int PadLeftSequence(byte[] bytes, byte[] seqBytes)
        {
            int i = 1;
            while (i < bytes.Length)
            {
                int n = bytes.Length - i;
                byte[] aux1 = new byte[n];
                byte[] aux2 = new byte[n];
                Array.Copy(bytes, i, aux1, 0, n);
                Array.Copy(seqBytes, aux2, n);
                if (aux1.SequenceEqual(aux2))
                    return i;
                i++;
            }
            return i;
        }
    }
}
