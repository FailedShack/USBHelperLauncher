using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace USBHelperLauncher
{
    class Database
    {
        public enum EncryptionVersion { DATA_V4, DATA_V6 };

        private readonly Dictionary<EncryptionVersion, AesData> keys = new Dictionary<EncryptionVersion, AesData>()
        {
            {
                EncryptionVersion.DATA_V4, new AesData
                (
                    new byte[]
                    {
                        94, 94, 66, 78, 67, 74, 72, 79, 194, 168, 194, 181, 111, 100, 50, 54
                    },
                    new byte[]
                    {
                        103, 94, 109, 98, 107, 108, 32, 116, 121, 94, 108, 42, 102, 42, 194, 163
                    }
                )
            },
            {
                EncryptionVersion.DATA_V6, new AesData
                (
                    new byte[]
                    {
                        103, 86, 222, 217, 210, 100, 188, 17, 49, 99, 182, 249, 182, 98, 233, 86
                    },
                    new byte[]
                    {
                        250, 18, 34, 138, 237, 50, 173, 87, 233, 64, 149, 189, 74, 122, 92, 30
                    }
                )
            }
        };

        public class AesData
        {
            private byte[] key;
            private byte[] iv;

            public AesData(byte[] key, byte[] iv)
            {
                this.key = key;
                this.iv = iv;
            }

            public byte[] GetKey()
            {
                return key;
            }

            public byte[] GetIV()
            {
                return iv;
            }
        }

        private string[] files = { "customs.json", "dlcs.json", "dlcs3ds.json", "games.json", "games3ds.json", "gamesWii.json", "injections.json", "updates.json", "updates3ds.json" };
        private byte[] zip;

        public void LoadFromDir(string path)
        {
            MemoryStream stream = new MemoryStream();
            using (ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                foreach (string file in files)
                {
                    string filePath = Path.Combine(path, file);
                    if (!File.Exists(filePath))
                    {
                        throw new FileNotFoundException("Could not load database, file not found: " + file);
                    }
                    zip.CreateEntryFromFile(filePath, "out/" + file);
                }
            }
            zip = stream.ToArray();
        }

        public MemoryStream Encrypt(EncryptionVersion version)
        {
            MemoryStream stream = new MemoryStream();
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.Mode = CipherMode.CBC;
                aes.Key = keys[version].GetKey();
                aes.IV = keys[version].GetIV();
                byte[] buffer = new byte[512];
                using (CryptoStream cryptoStream = new CryptoStream(Stream(), aes.CreateEncryptor(), CryptoStreamMode.Read))
                {
                    int count;
                    do
                    {
                        count = cryptoStream.Read(buffer, 0, 512);
                        stream.Write(buffer, 0, count);
                    }
                    while (count > 0);
                }
            }
            return stream;
        }

        public MemoryStream Stream()
        {
            return new MemoryStream(zip);
        }
    }
}
