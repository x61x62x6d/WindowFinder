using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;

namespace WindowFinder
{
    public partial class MainWindow : Window
    {
        const int SW_MAXIMIZE = 3;
        const int SW_NORMALIZE = 1;
        const int GA_ROOT_OWNER = 3;
        const int HOTKEY_ID = 9001;
        const int WIN_MODIFIER = 8;
        const int VK_ESCAPE = 0x1B;

        TaskAwaiter getWindowsAwaiter = Task.CompletedTask.GetAwaiter();
        NotifyIcon trayIcon = new NotifyIcon();

        public MainWindow()
        {
            InitializeComponent();           
            Finder.Initialize();
            InitTrayIcon();
            Activated += OnFocus;
            Closed += OnClosing;
            CenterWindowOnScreen();
            WindowState = WindowState.Minimized;            
            this.ShowInTaskbar = false;
            
            //HACK: if Hide() is not called the minimized windows is visible in bottom left corner
            //if Show() is not called earlier hotkey doesn't work
            this.Show(); 
            this.Hide();
        }

        private void InitTrayIcon()
        {
            var iconUri = new Uri("pack://application:,,,/WindowFinder;component/windowFinder.ico");
            var iconStream = System.Windows.Application.GetResourceStream(iconUri).Stream;
            trayIcon.Icon = new Icon(iconStream);            
            trayIcon.Visible = true;
            trayIcon.DoubleClick += OnFocus;
            trayIcon.ContextMenu = new System.Windows.Forms.ContextMenu(new System.Windows.Forms.MenuItem[]
            { new System.Windows.Forms.MenuItem("Quit", Quit)});

            iconStream.Dispose();
        }


        public void OnClosing(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
        }

        public void OnFocus(object sender, EventArgs e)
        {
            this.Show();
            Activate();
            WindowState = WindowState.Normal;
            GetWindows();
            SearchBox.Focus();
            SearchBox.SelectAll();
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            this.Hide();
            GC.Collect();
        }

        public void Quit(object sender, EventArgs e)
        {
            this.Close();
        }

        protected IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0312 && wParam.ToInt32() == HOTKEY_ID)
            {
                if (!this.IsActive)
                {
                    this.Show();
                    Activate();
                    WindowState = WindowState.Normal;
                }
                else
                {
                    WindowState = WindowState.Minimized;
                }
            }
            return IntPtr.Zero;
        }

        private void CenterWindowOnScreen()
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 3) - (windowHeight / 2);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            refreshList();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;
            HwndSource source = HwndSource.FromHwnd(handle);
            source.AddHook(new HwndSourceHook(WndProc));
            User32.RegisterHotKey(handle, HOTKEY_ID, WIN_MODIFIER, VK_ESCAPE);  //shortcut to bring up window - win+esc
        }

        private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BringToFront();
            }
            else if (e.Key == Key.Down || e.Key == Key.Up)
            {
                if (!WindowsGrid.IsKeyboardFocusWithin)
                {
                    Keyboard.Focus(WindowsGrid);
                    DataGridRow row = (DataGridRow)WindowsGrid.ItemContainerGenerator.ContainerFromIndex(0);
                    row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
            }
            else if (e.Key == Key.Escape)
            {
                clearTextbox();
            }
            else if (e.Key == Key.LWin || e.Key == Key.RWin)
            {
                return;
            }
            else
            {
                SearchBox.Focus();
            }
        }

        //----

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

        private void OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            PropertyDescriptor propertyDescriptor = (PropertyDescriptor)e.PropertyDescriptor;
            e.Column.Header = propertyDescriptor.DisplayName;
            if (propertyDescriptor.DisplayName == "FullString" || propertyDescriptor.DisplayName == "WindowHandle")
            {
                e.Cancel = true;
            }
        }

        public void GetWindows()
        {
            if ( getWindowsAwaiter.IsCompleted)
            {
                getWindowsAwaiter = Task.Run(() =>
                {
                    Finder.GetWindows();

                }).GetAwaiter();
                getWindowsAwaiter.OnCompleted(() =>
                {
                    refreshList();
                });
            }
        }

        private void BringToFront()
        {
            var selected = (WindowInfo)WindowsGrid.SelectedItem;
            if (selected == null) return;

            var handle = selected.WindowHandle;

            if (User32.IsIconic(handle))
            {
                User32.ShowWindow(handle, SW_MAXIMIZE);
            }
            User32.SetForegroundWindow(handle);
        }

        private void clearTextbox()
        {
            SearchBox.Focus();
            SearchBox.Text = "";
        }

        private void refreshList()
        {
            WindowsGrid.ItemsSource = new ObservableCollection<WindowInfo>(Finder.OpenedWindows
                .Where(x => x.FullString.Contains(SearchBox.Text.ToLower())).ToList());
            WindowsGrid.SelectedIndex = 0;
        }

        private void GridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BringToFront();
        }
    }
}
