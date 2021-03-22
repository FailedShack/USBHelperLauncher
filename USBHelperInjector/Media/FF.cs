using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;

namespace USBHelperInjector.Media
{
    class FF : IDisposable
    {
        static readonly string FFMPEG_URL = "http://dl.nul.sh/ffmpeg/win32/ffmpeg-release-essentials.zip";
        static readonly object FFPLAY_CHECK = new object();
        static readonly LinkedList<FF> PLAYERS = new LinkedList<FF>();

        private bool _pause;
        private bool stopped;
        private readonly bool tookFocus = false;

        public Process Process { get; private set; }
        public bool Pause
        {
            get
            {
                return _pause;
            }
            set
            {
                if (_pause && !value)
                {
                    Process.Resume();
                }
                else if (!_pause && value)
                {
                    Process.Suspend();
                }
                _pause = value;
            }
        }

        internal FF(Process process)
        {
            Process = process;
            Process.EnableRaisingEvents = true;
            Process.Exited += (sender, e) => Stop();
            var last = PLAYERS.Last?.Value;
            if (last != null && !last.Pause)
            {
                last.Pause = true;
                tookFocus = true;
            }
            PLAYERS.AddLast(this);
            Process.Start();
        }

        ~FF()
        {
            Stop();
        }

        public void Stop()
        {
            if (stopped)
            {
                return;
            }
            stopped = true;
            if (!Process.HasExited)
            {
                Process.Kill();
                Process.WaitForExit();
            }
            ReleaseFocus();
        }

        public void ReleaseFocus()
        {
            PLAYERS.RemoveLast();
            if (tookFocus)
            {
                PLAYERS.Last.Value.Pause = false;
            }
        }

        public void Dispose()
        {
            Stop();
        }

        public static FF Play(string args)
        {
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
            var startInfo = new ProcessStartInfo()
            {
                FileName = "ffplay.exe",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            return new FF(new Process() { StartInfo = startInfo });
        }
    }
}
