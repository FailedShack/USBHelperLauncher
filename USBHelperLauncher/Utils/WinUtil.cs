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

        // source: winnt.h
        public enum ImageFileMachine
        {
            Unknown    = 0,
            I860       = 0x014D,
            I386       = 0x014C,
            R3000      = 0x0162,
            R4000      = 0x0166,
            R10000     = 0x0168,
            WCEMIPSV2  = 0x0169,
            ALPHA      = 0x0184,
            SH3        = 0x01A2,
            SH3DSP     = 0x01A3,
            SH3E       = 0x01A4,
            SH4        = 0x01A6,
            SH5        = 0x01A8,
            ARM        = 0x01C0,
            THUMB      = 0x01C2,
            ARMNT      = 0x01C4,
            AM33       = 0x01D3,
            POWERPC    = 0x01F0,
            POWERPCFP  = 0x01F1,
            IA64       = 0x0200,
            MIPS16     = 0x0266,
            ALPHA64    = 0x0284,
            MIPSFPU    = 0x0366,
            MIPSFPU16  = 0x0466,
            AXP64      = 0x0284,
            TRICORE    = 0x0520,
            INFINEON   = 0x0520,
            CEF        = 0x0CEF,
            EBC        = 0x0EBC,
            AMD64      = 0x8664,
            M32R       = 0x9041,
            AA64       = 0xAA64
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool IsWow64Process2(
            IntPtr process,
            out ImageFileMachine processMachine,
            out ImageFileMachine nativeMachine
        );

        [DllImport("kernel32.dll")]
        internal static extern void GetNativeSystemInfo(ref SystemInfo lpSystemInfo);

        [DllImport("kernel32.dll")]
        internal static extern void GetSystemInfo(ref SystemInfo lpSystemInfo);

        public enum ProcessorArchitecture : ushort
        {
            Unknown = 0xFFFF,
            I386    = 0,
            ARM     = 5,
            IA64    = 6,
            AMD64   = 9,
            ARM64   = 12
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SystemInfo
        {
            public ProcessorArchitecture wProcessorArchitecture;
            public ushort wReserved;
            public uint dwPageSize;
            public IntPtr lpMinimumApplicationAddress;
            public IntPtr lpMaximumApplicationAddress;
            public UIntPtr dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public ushort wProcessorLevel;
            public ushort wProcessorRevision;
        }
    }
}
