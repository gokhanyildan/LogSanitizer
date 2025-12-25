﻿﻿﻿using System.Configuration;
using System.Data;
using System.Windows;

namespace LogSanitizer.GUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        try
        {
            System.IO.File.AppendAllText("startup_trace.log", "OnStartup entered\n");
            var mainWindow = new MainWindow();
            this.MainWindow = mainWindow;
            mainWindow.Show();
            System.IO.File.AppendAllText("startup_trace.log", "MainWindow shown\n");
        }
        catch (Exception ex)
        {
            string errorMsg = $"Startup Error: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
            if (ex.InnerException != null)
            {
                errorMsg += $"\n\nInner Exception: {ex.InnerException.Message}";
            }
            System.IO.File.WriteAllText("startup_error.log", errorMsg);
            MessageBox.Show(errorMsg, "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        string errorMsg = $"An unhandled exception occurred: {e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}";
        if (e.Exception.InnerException != null)
        {
            errorMsg += $"\n\nInner Exception: {e.Exception.InnerException.Message}";
        }
        
        System.IO.File.WriteAllText("error.log", errorMsg);
        MessageBox.Show(errorMsg, "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
        Shutdown();
    }
}

