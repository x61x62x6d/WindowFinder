using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
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

        TaskAwaiter getWindowsAwaiter;
        bool fresh = true;
        NotifyIcon notifyIcon = new NotifyIcon();

        public MainWindow()
        {
            InitializeComponent();
            var iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/WindowFinder;component/windowFinder.ico")).Stream;
            notifyIcon.Icon = new Icon(iconStream);
            iconStream.Dispose();
            notifyIcon.Visible = true;
            Logic.Initialize();
            notifyIcon.DoubleClick += OnFocus;
            notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(new System.Windows.Forms.MenuItem[] 
            { new System.Windows.Forms.MenuItem("Quit", Quit)});
            Activated += OnFocus;
            GetWindows();
            CenterWindowOnScreen();
            WindowState = WindowState.Minimized;
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
            Win32Wrapper.RegisterHotKey(handle, HOTKEY_ID, WIN_MODIFIER, VK_ESCAPE);
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
            if (fresh || getWindowsAwaiter.IsCompleted)
            {
                getWindowsAwaiter = Task.Run(() =>
                {
                    Logic.GetWindows();

                }).GetAwaiter();
                getWindowsAwaiter.OnCompleted(() =>
                {
                    refreshList();
                });
                fresh = false;
            }
        }

        private void BringToFront()
        {
            var selected = (WindowInfo)WindowsGrid.SelectedItem;
            var handle = selected.WindowHandle;
            if (Win32Wrapper.IsIconic(handle))
            {
                Win32Wrapper.ShowWindow(handle, SW_MAXIMIZE);
            }
            Win32Wrapper.SetForegroundWindow(handle);
        }

        private void clearTextbox()
        {
            SearchBox.Focus();
            SearchBox.Text = "";
        }

        private void refreshList()
        {
            WindowsGrid.ItemsSource = new ObservableCollection<WindowInfo>(Logic.OpenedWindows
                .Where(x => x.FullString.Contains(SearchBox.Text.ToLower())).ToList());
            WindowsGrid.SelectedIndex = 0;
        }

        private void GridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BringToFront();
        }
    }
}
