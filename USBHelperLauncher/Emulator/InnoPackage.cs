using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace USBHelperLauncher.Emulator
{
    class InnoPackage : Package
    {
        public InnoPackage(Uri uri, string name, string version) : base(uri, name, version) { }

        public InnoPackage(Uri uri, string name, string version, string installPath) : base(uri, name, version, installPath) { }

        public async override Task<DirectoryInfo> DoUnpack()
        {
            if (!File.Exists("innounp.exe"))
            {
                throw new FileNotFoundException("Could not find dependency 'innounp.exe'.");
            }
            if (packageFile == null || !packageFile.Exists)
            {
                throw new InvalidOperationException();
            }
            string filePath = packageFile.FullName;
            string path = Path.GetDirectoryName(filePath);
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(path, Path.GetFileNameWithoutExtension(filePath)));
            var tcs = new TaskCompletionSource<object>();
            Process process = new Process();
            process.StartInfo.FileName = Path.Combine(Program.GetLauncherPath(), "innounp.exe");
            process.StartInfo.Arguments = "-x \"" + packageFile.FullName + "\"";
            process.StartInfo.WorkingDirectory = path;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => tcs.TrySetResult(null);
            process.Start();
            await tcs.Task;
            Directory.Move(Path.Combine(path, "{app}"), dir.FullName);
            return dir;
        }
    }
}
