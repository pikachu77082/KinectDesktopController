using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace KinectDesktopController
{
    public partial class MainWindow : Window
    {
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int GWL_EXSTYLE = (-20);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private KinectManager _kinectManager;
        private CursorWindow _cursorWindow;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Position top-center
            this.Top = 0;
            this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;

            // Make the window click-through so we can interact with desktop underneath
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);

            _cursorWindow = new CursorWindow();
            _cursorWindow.Show();

            TouchInjector.Init();

            _kinectManager = new KinectManager(this);
            _kinectManager.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            _kinectManager?.Stop();
            _cursorWindow?.Close();
            base.OnClosed(e);
        }

        // Methods for KinectManager to update UI
        public void UpdateStatus(string text, Brush color = null)
        {
            StatusText.Text = text;
            StatusText.Foreground = color ?? Brushes.White;
        }

        public void SetShadowSource(ImageSource source)
        {
            ShadowImage.Source = source;
        }

        public void UpdateCursor(double x, double y, bool isDown)
        {
            _cursorWindow?.UpdateCursor(x, y, isDown);
        }

        public void HideCursor()
        {
            _cursorWindow?.SetCursorVisibility(false);
        }
        
        public void ShowCursor()
        {
            _cursorWindow?.SetCursorVisibility(true);
        }
    }
}
