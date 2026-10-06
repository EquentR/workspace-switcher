using System;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using WorkspaceSwitcher.Core.Localization;
using WorkspaceSwitcher.Core.Services;

namespace WorkspaceSwitcher.UI;

using WpfApp = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;

public partial class App : WpfApp
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // Fix the effective UI language before the first window, dialog or tray
        // object is created. MainWindow is constructed manually for this reason
        // (no StartupUri): the choice takes effect on the NEXT launch only.
        var settings = new SettingsService().Load();
        Localizer.Initialize(settings.Language, CultureInfo.CurrentUICulture);

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WpfMessageBox.Show($"UI Error: {e.Exception.Message}\n\n{e.Exception.StackTrace}", "Workspace Switcher - Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            WpfMessageBox.Show($"Fatal Error: {ex.Message}\n\n{ex.StackTrace}", "Workspace Switcher - Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
