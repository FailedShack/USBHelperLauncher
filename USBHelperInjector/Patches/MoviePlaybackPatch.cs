using HarmonyLib;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace USBHelperInjector.Patches
{
    [Attributes.Optional]
    [HarmonyPatch]
    class MoviePlaybackPatch
    {
        const int GWL_HWNDPARENT = -8;
        static readonly Process PROCESS = Process.GetCurrentProcess();
        static readonly string FFMPEG_URL = "https://ffmpeg.zeranoe.com/builds/win32/static/ffmpeg-latest-win32-static.zip";
        static readonly object FFPLAY_CHECK = new object();
        static Process player;

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        static bool Prepare()
        {
            NusGrabberFormPatch.FormClosing += (sender, e) => Stop();
            return true;
        }

        static MethodBase TargetMethod()
        {
            return AccessTools.Constructor(
                (from type in ReflectionHelper.Types
                 from field in type.GetFields(AccessTools.all)
                 where field.FieldType.Name == "AxWindowsMediaPlayer"
                 select type).FirstOrDefault(), new[] { typeof(string) });
        }

        static bool Prefix(Form __instance, string __0)
        {
            __instance.Load += (sender, e) => __instance.Close();
            Uri uri; // URL after redirects
            var request = WebRequest.Create(__0);
            var headers = new StringBuilder();
            using (var response = request.GetResponse())
            {
                uri = response.ResponseUri;
            }
            foreach (var header in request.Headers.AllKeys)
            {
                headers.AppendFormat("{0}: {1}\r\n", header, request.Headers[header]);
            }
            Stop();
            lock (FFPLAY_CHECK)
            {
                if (!File.Exists("ffplay.exe"))
                {
                    var temp = Path.GetTempFileName();
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(FFMPEG_URL, temp);
                    }
                    using (var archive = ZipFile.OpenRead(temp))
                    {
                        var entry = archive.Entries.Where(file => file.Name == "ffplay.exe").FirstOrDefault();
                        entry.ExtractToFile(entry.Name, true);
                    }
                    File.Delete(temp);
                }
            }
            var drawText = new[]
            {
                "enable='between(t,0,5)'",
                "text='PRESS ESC TO EXIT'",
                "fontfile=/Windows/Fonts/segoeui.ttf",
                "fontcolor=white",
                "fontsize=48",
                "box=1",
                "boxcolor=black@0.4",
                "boxborderw=16",
                "x=(w-tw)/2",
                "y=h-(2*lh)"
            };
            var startInfo = new ProcessStartInfo()
            {
                FileName = "ffplay.exe",
                Arguments = string.Format(
                    "{0} -noborder -autoexit -reconnect 1 -http_proxy {1} -headers \"{2}\" -vf \"drawtext={3}\"",
                    uri, Overrides.Proxy.Address, headers, string.Join(":", drawText)),
                UseShellExecute = false,
                CreateNoWindow = true
            };
            player = new Process() { StartInfo = startInfo };
            player.Start();
            while (player.MainWindowHandle == IntPtr.Zero)
            {
                Thread.Sleep(20);
                player.Refresh();
            }

            // Set the owner of the player to the main window
            SetWindowLong(player.MainWindowHandle, GWL_HWNDPARENT, PROCESS.MainWindowHandle);
            return false;
        }

        static void Stop()
        {
            if (player != null && !player.HasExited)
            {
                player.Kill();
                player.WaitForExit();
            }
        }
    }
}
