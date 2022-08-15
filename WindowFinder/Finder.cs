using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowFinder
{
    public static class Finder
    {
        public static List<WindowInfo> OpenedWindows { get; set; }

        public static void Initialize()
        {
            OpenedWindows = new List<WindowInfo>();
        }

        static IntPtr GetSystemTrayHandle()
        {
            IntPtr hWndTray = User32.FindWindow("Shell_TrayWnd", null);
            if (hWndTray != IntPtr.Zero)
            {
                hWndTray = User32.FindWindowEx(hWndTray, IntPtr.Zero, "TrayNotifyWnd", null);
                if (hWndTray != IntPtr.Zero)
                {
                    hWndTray = User32.FindWindowEx(hWndTray, IntPtr.Zero, "SysPager", null);
                    if (hWndTray != IntPtr.Zero)
                    {
                        hWndTray = User32.FindWindowEx(hWndTray, IntPtr.Zero, "ToolbarWindow32", null);
                        return hWndTray;
                    }
                }
            }
            return IntPtr.Zero;
        }



        public static void ScanToolbarButtons()
        {
            List<string> buttonTexts = new List<string>();
            const int PROCESS_ALL_ACCESS = 0x001FFFFF;
            var trayHandle = GetSystemTrayHandle();
            if (trayHandle == IntPtr.Zero)
                return;

            var count = User32.SendMessage(trayHandle, (int)TB.BUTTONCOUNT, 0, IntPtr.Zero).ToInt32();
            if (count == 0)
                return;

            User32.GetWindowThreadProcessId(trayHandle, out var pid);
            var hProcess = Kernel32.OpenProcess(PROCESS_ALL_ACCESS, false, pid.ToInt32());
            if (hProcess == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            var size = (IntPtr)Marshal.SizeOf<TBBUTTONINFOW>();
            var buffer = Kernel32.VirtualAllocEx(hProcess, IntPtr.Zero, size, Win32Const.MEM_COMMIT, Win32Const.PAGE_READWRITE);
            if (buffer == IntPtr.Zero)
            {
                Kernel32.CloseHandle(hProcess);
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            for (int i = 0; i < count; i++)
            {
                var btn = new TBBUTTONINFOW();
                btn.cbSize = size.ToInt32();
                btn.dwMask = Win32Const.TBIF_BYINDEX | Win32Const.TBIF_COMMAND;
                if (Kernel32.WriteProcessMemory(hProcess, buffer, ref btn, size, out var written))
                {
                    // we want the identifier
                    var res = User32.SendMessage(trayHandle, Win32Const.TB_GETBUTTONINFOW, i, buffer);
                    int r = res.ToInt32();
                    if (res.ToInt32() >= 0)
                    {
                        if (Kernel32.ReadProcessMemory(hProcess, buffer, ref btn, size, out var read))
                        {
                            // now get display text using the identifier
                            // first pass we ask for size
                            var textSize = User32.SendMessage(trayHandle, Win32Const.TB_GETBUTTONTEXTW, btn.idCommand, IntPtr.Zero);
                            if (textSize.ToInt32() != -1)
                            {
                                // we need to allocate for the terminating zero and unicode
                                var utextSize = (IntPtr)((1 + textSize.ToInt32()) * 2);
                                var textBuffer = Kernel32.VirtualAllocEx(hProcess, IntPtr.Zero, utextSize, Win32Const.MEM_COMMIT, Win32Const.PAGE_READWRITE);
                                if (textBuffer != IntPtr.Zero)
                                {
                                    res = User32.SendMessage(trayHandle, Win32Const.TB_GETBUTTONTEXTW, btn.idCommand, textBuffer);
                                    if (res == textSize)
                                    {
                                        var localBuffer = Marshal.AllocHGlobal(utextSize.ToInt32());
                                        if (Kernel32.ReadProcessMemory(hProcess, textBuffer, localBuffer, utextSize, out read))
                                        {
                                            var text = Marshal.PtrToStringUni(localBuffer);
                                            buttonTexts.Add(text);
                                        }
                                        Marshal.FreeHGlobal(localBuffer);
                                    }
                                    Kernel32.VirtualFreeEx(hProcess, textBuffer, IntPtr.Zero, Win32Const.MEM_RELEASE);
                                }
                            }
                        }
                    }
                }
            }

            Kernel32.VirtualFreeEx(hProcess, buffer, IntPtr.Zero, Win32Const.MEM_RELEASE);
            Kernel32.CloseHandle(hProcess);
        }


        public static void GetWindows()
        {
            OpenedWindows.Clear();
            //ScanToolbarButtons();
            IntPtr shellWindow = User32.GetShellWindow();

            User32.EnumWindows(delegate (IntPtr hWnd, int lParam)
            {
                if (hWnd == shellWindow) return true;
                if (!User32.IsWindowVisible(hWnd)) return true;

                int length = User32.GetWindowTextLength(hWnd);
                if (length == 0) return true;

                User32.GetWindowThreadProcessId(hWnd, out IntPtr processId);
                Process process = Process.GetProcessById(processId.ToInt32());
                if (process.ProcessName == "WindowFinder")
                {
                    return true;
                }

                StringBuilder titleBldr = new StringBuilder(length);
                User32.GetWindowText(hWnd, titleBldr, length + 1);
                OpenedWindows.Add(new WindowInfo(titleBldr.ToString(), process.ProcessName, process.Id, hWnd));
                return true;

            }, 0);
        }

    }
}
