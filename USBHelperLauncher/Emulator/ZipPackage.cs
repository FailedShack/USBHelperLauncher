using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USBHelperLauncher.Emulator
{
    class ZipPackage : Package
    {
        public ZipPackage(Uri uri, string name, string version) : base(uri, name, version) { }

        public ZipPackage(Uri uri, string name, string version, string installPath) : base(uri, name, version, installPath) { }

        public async override Task<DirectoryInfo> Unpack()
        {
            if (packageFile == null || !packageFile.Exists)
            {
                throw new InvalidOperationException();
            }
            string filePath = packageFile.FullName;
            string path = Path.GetDirectoryName(filePath);
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(path, Path.GetFileNameWithoutExtension(filePath)));
            dir.Create();
            await Task.Run(() => ZipFile.ExtractToDirectory(filePath, dir.FullName));
            RaisePostUnpack(dir);
            return dir;
        }
    }
}
