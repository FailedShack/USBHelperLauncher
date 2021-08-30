using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace USBHelperInjector.Media
{
    class FF : IDisposable
    {
        const string FFPLAY_PATH = "extern/ffmpeg/ffplay.exe";

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
            var startInfo = new ProcessStartInfo()
            {
                FileName = FFPLAY_PATH,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            return new FF(new Process() { StartInfo = startInfo });
        }
    }
}
