using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace KinectDesktopController
{
    public partial class CursorWindow : Window
    {
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int GWL_EXSTYLE = (-20);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        public CursorWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }

        public void UpdateCursor(double x, double y, bool isDown)
        {
            Canvas.SetLeft(CursorGrid, x);
            Canvas.SetTop(CursorGrid, y);

            if (isDown)
            {
                InnerCursor.Fill = Brushes.Red;
                FeedbackRing.Width = 40;
                FeedbackRing.Height = 40;
            }
            else
            {
                InnerCursor.Fill = Brushes.White;
                FeedbackRing.Width = 80;
                FeedbackRing.Height = 80;
            }
        }
        
        public void SetCursorVisibility(bool visible)
        {
            CursorCanvas.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }
    }
}
