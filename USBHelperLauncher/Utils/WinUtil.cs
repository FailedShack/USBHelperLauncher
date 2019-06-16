using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace USBHelperLauncher.Utils
{
    internal static class WinUtil
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        private delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        public static int GetWindowCount(int processId)
        {
            IntPtr hShellWindow = GetShellWindow();
            int count = 0;
            EnumWindows(delegate (IntPtr hWnd, int lParam)
            {
                if (hWnd == hShellWindow) return true;
                if (!IsWindowVisible(hWnd)) return true;

                int length = GetWindowTextLength(hWnd);
                if (length == 0) return true;

                GetWindowThreadProcessId(hWnd, out uint windowPid);
                if (windowPid != processId) return true;

                count++;
                return true;
            }, 0);
            return count;
        }

        public static string GetWindowTitle(IntPtr hWnd)
        {
            int textLength = GetWindowTextLength(hWnd);
            StringBuilder outText = new StringBuilder(textLength + 1);
            int a = GetWindowText(hWnd, outText, outText.Capacity);
            return outText.ToString();
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Auto)]
        public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        private const UInt32 WM_CLOSE = 0x0010;

        public static void CloseWindow(IntPtr hwnd)
        {
            SendMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        [DllImport("Kernel32.dll")]
        private static extern uint QueryFullProcessImageName([In] IntPtr hProcess, [In] uint dwFlags, [Out] StringBuilder lpExeName, [In, Out] ref uint lpdwSize);

        public static string GetMainModuleFileName(this Process process, int buffer = 1024)
        {
            var fileNameBuilder = new StringBuilder(buffer);
            uint bufferLength = (uint)fileNameBuilder.Capacity + 1;
            return QueryFullProcessImageName(process.Handle, 0, fileNameBuilder, ref bufferLength) != 0 ?
                fileNameBuilder.ToString() :
                null;
        }

        public class CSP
        {
            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool CryptAcquireContext(out IntPtr phProv, string pszContainer, string pszProvider, uint dwProvType, uint dwFlags);

            const uint PROV_RSA_FULL = 1;
            const uint CRYPT_DELETEKEYSET = 16;

            public const uint NTE_BAD_KEY_STATE = 0x8009000B;

            public static uint TryAcquire(string keyContainer)
            {
                CryptAcquireContext(out _, keyContainer, null, PROV_RSA_FULL, 0);
                return (uint)Marshal.GetLastWin32Error();
            }

            public static void Delete(string keyContainer)
            {
                if (!CryptAcquireContext(out _, keyContainer, null, PROV_RSA_FULL, CRYPT_DELETEKEYSET))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }
}
