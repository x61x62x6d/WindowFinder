using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowFinder
{
    public class WindowInfo
    {
        const int MAX_TITLE_LEN = 70;

        public string Title { get; private set; }
        public string ProcessName { get; private set; }
        public int ProcessId { get; private set; }
        public string Handle { get; private set; }

        public IntPtr WindowHandle { get; private set; }
        public string FullString { get; private set; }

        public WindowInfo(string title, string processName, int processId, IntPtr handle)
        {
            Title = title.Length <= MAX_TITLE_LEN ? title : $"{title.Substring(0, MAX_TITLE_LEN-3)}...";
            ProcessName = processName;
            ProcessId = processId;
            WindowHandle = handle;
            Handle = $"0x{handle.ToString("x")}";
            FullString = $"{title.ToLower()} {processName} {processId} {Handle}";
        }
    }
}
