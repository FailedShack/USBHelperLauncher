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
using USBHelperInjector.Media;

namespace USBHelperInjector.Patches
{
    [Attributes.Optional]
    [HarmonyPatch]
    class MoviePlaybackPatch
    {
        const int GWL_HWNDPARENT = -8;
        static readonly Process PROCESS = Process.GetCurrentProcess();
        static FF PLAYER;

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        static bool Prepare()
        {
            NusGrabberFormPatch.FormClosing += (sender, e) => PLAYER?.Stop();
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
            var drawText = new[]
            {
                "enable='between(t,0,5)'",
                "text='PRESS ESC TO EXIT'",
                "fontfile=" + InjectorService.DefaultFont,
                "fontcolor=white",
                "fontsize=48",
                "box=1",
                "boxcolor=black@0.4",
                "boxborderw=16",
                "x=(w-tw)/2",
                "y=h-(2*lh)"
            };
            PLAYER?.Stop();
            PLAYER = FF.Play(string.Format(
                    "{0} -noborder -autoexit -reconnect 1 -http_proxy {1} -headers \"{2}\" -vf \"drawtext={3}\"",
                    uri, Overrides.Proxy.Address, headers, string.Join(":", drawText)));
            while (PLAYER.Process.MainWindowHandle == IntPtr.Zero)
            {
                Thread.Sleep(20);
                PLAYER.Process.Refresh();
            }

            // Set the owner of the player to the main window
            SetWindowLong(PLAYER.Process.MainWindowHandle, GWL_HWNDPARENT, PROCESS.MainWindowHandle);
            return false;
        }
    }
}
