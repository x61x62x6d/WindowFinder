using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static void GetWindows()
        {
            OpenedWindows.Clear();
            IntPtr shellWindow = Win32Wrapper.GetShellWindow();

            Win32Wrapper.EnumWindows(delegate (IntPtr hWnd, int lParam)
            {
                if (hWnd == shellWindow) return true;
                if (!Win32Wrapper.IsWindowVisible(hWnd)) return true;

                int length = Win32Wrapper.GetWindowTextLength(hWnd);
                if (length == 0) return true;

                Win32Wrapper.GetWindowThreadProcessId(hWnd, out IntPtr processId);
                Process process = Process.GetProcessById(processId.ToInt32());
                if (process.ProcessName == "WindowFinder")
                {
                    return true;
                }

                StringBuilder titleBldr = new StringBuilder(length);
                Win32Wrapper.GetWindowText(hWnd, titleBldr, length + 1);
                OpenedWindows.Add(new WindowInfo(titleBldr.ToString(), process.ProcessName, process.Id, hWnd));
                return true;

            }, 0);
        }

    }
}
