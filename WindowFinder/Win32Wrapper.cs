using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowFinder
{
    [Flags]
    public enum TB
    {
        WM_USER = 0x0400,
        GETBUTTON = (WM_USER + 23),
        GETBUTTONTEXTW = (WM_USER + 75),
        BUTTONCOUNT = (WM_USER + 24)
    }

    public static class Win32Const
    {
        public const int TBIF_BYINDEX = unchecked((int)0x80000000); // this specifies that the wparam in Get/SetButtonInfo is an index, not id
        public const int TBIF_COMMAND = 0x20;
        public const int MEM_COMMIT = 0x1000;
        public const int MEM_RELEASE = 0x8000;
        public const int PAGE_READWRITE = 0x4;
        public const int TB_GETBUTTONINFOW = 1087;
        public const int TB_GETBUTTONTEXTW = 1099;
        public const int TB_BUTTONCOUNT = 1048;
    }

    public static class User32
    {
        public delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out IntPtr ProcessId);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, ref IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
    }

    public static class Kernel32
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, int flAllocationType, int flProtect);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, int dwFreeType);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref TBBUTTONINFOW lpBuffer, IntPtr nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref TBBUTTONINFOW lpBuffer, IntPtr nSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, IntPtr nSize, out IntPtr lpNumberOfBytesRead);
    }

    //[StructLayout(LayoutKind.Sequential)]
    //public struct TBBUTTON
    //{
    //    public int iBitmap;
    //    public int idCommand;
    //    [StructLayout(LayoutKind.Explicit)]
    //    private struct TBBUTTON_U
    //    {
    //        [FieldOffset(0)] public byte fsState;
    //        [FieldOffset(1)] public byte fsStyle;
    //        [FieldOffset(0)] private IntPtr bReserved;
    //    }
    //    private TBBUTTON_U union;
    //    public byte fsState { get { return union.fsState; } set { union.fsState = value; } }
    //    public byte fsStyle { get { return union.fsStyle; } set { union.fsStyle = value; } }
    //    public UIntPtr dwData;
    //    public IntPtr iString;
    //}

    [StructLayout(LayoutKind.Sequential)]
    public struct TBBUTTONINFOW
    {
        public int cbSize;
        public int dwMask;
        public int idCommand;
        public int iImage;
        public byte fsState;
        public byte fsStyle;
        public short cx;
        public IntPtr lParam;
        public IntPtr pszText;
        public int cchText;
        public IntPtr dwData;
    }
}
