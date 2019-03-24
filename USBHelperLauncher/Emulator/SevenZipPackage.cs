using SharpCompress.Archives.SevenZip;
using SharpCompress.Readers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace USBHelperLauncher.Emulator
{
    class SevenZipPackage : Package
    {
        public SevenZipPackage(Uri uri, string name, string version) : base(uri, name, version) { }

        public SevenZipPackage(Uri uri, string name, string version, string installPath) : base(uri, name, version, installPath) { }

        public async override Task<DirectoryInfo> DoUnpack()
        {
            if (packageFile == null || !packageFile.Exists)
            {
                throw new InvalidOperationException();
            }
            string filePath = packageFile.FullName;
            string path = Path.GetDirectoryName(filePath);
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(path, Path.GetFileNameWithoutExtension(filePath)));
            dir.Create();
            await Task.Run(() =>
            {
                using (Stream stream = File.OpenRead(filePath))
                using (var archive = SevenZipArchive.Open(stream))
                {
                    var reader = archive.ExtractAllEntries();
                    while (reader.MoveToNextEntry())
                    {
                        if (!reader.Entry.IsDirectory)
                        {
                            reader.WriteEntryToDirectory(dir.FullName, new ExtractionOptions()
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                        }
                    }
                }
            });
            return dir;
        }
    }
}
