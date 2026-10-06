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
        // Outer explanation and title are localized; the original message and stack
        // trace are passed through verbatim as diagnostic detail.
        WpfMessageBox.Show(
            Localizer.Current.Format("Error.UiMessage", e.Exception.Message, e.Exception.StackTrace),
            Localizer.Current.Get("Error.UiTitle"),
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            WpfMessageBox.Show(
                Localizer.Current.Format("Error.FatalMessage", ex.Message, ex.StackTrace),
                Localizer.Current.Get("Error.FatalTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
