using System;
using System.Windows;

namespace KinectDesktopController
{
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            System.IO.File.WriteAllText("crash.log", "Dispatcher Exception: " + e.Exception.ToString());
            MessageBox.Show("Dispatcher Exception: " + e.Exception.ToString(), "Crash Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            System.IO.File.WriteAllText("crash.log", "AppDomain Exception: " + e.ExceptionObject.ToString());
            MessageBox.Show("AppDomain Exception: " + e.ExceptionObject.ToString(), "Crash Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
