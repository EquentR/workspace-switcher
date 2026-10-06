using System;
using System.ComponentModel;
using System.Windows;
using WorkspaceSwitcher.Core;
using WorkspaceSwitcher.Core.Hotkeys;
using WorkspaceSwitcher.Core.Localization;
using WorkspaceSwitcher.Core.Services;
using WorkspaceSwitcher.UI.Services;
using WorkspaceSwitcher.UI.ViewModels;

namespace WorkspaceSwitcher.UI;

public partial class MainWindow : Window
{
    private readonly WindowManager _windowManager;
    private readonly ProfileService _profileService;
    private readonly SettingsService _settingsService;
    private readonly HotkeyManager _hotkeyManager;
    private readonly TrayIconService _trayIconService;
    private readonly MainViewModel _viewModel;
    private bool _isExplicitExit;

    public MainWindow()
    {
        InitializeComponent();
        ApplyLocalization();

        _windowManager = new WindowManager();
        _profileService = new ProfileService();
        _settingsService = new SettingsService();
        _hotkeyManager = new HotkeyManager();

        _viewModel = new MainViewModel(_windowManager, _profileService, _settingsService, _hotkeyManager);
        DataContext = _viewModel;

        _trayIconService = new TrayIconService(
            _windowManager,
            _profileService,
            ShowWindow,
            ExplicitExit,
            profileName =>
            {
                var profile = _profileService.LoadProfile(profileName);
                if (profile != null)
                {
                    _viewModel.SwitchToWorkspace(profile, source: "Tray");
                    _trayIconService?.ShowNotification("Workspace Switched", $"Switched to '{profile.Name}'.");
                }
            }
        );
    }

    /// <summary>
    /// Replaces the design-time English texts of the main window overview, workspace
    /// list chrome and Restore Settings card with the effective language's strings.
    /// Dynamic texts (counts, capture time, fallbacks) come from the view models.
    /// </summary>
    private void ApplyLocalization()
    {
        var localizer = Localizer.Current;

        TaglineText.Text = localizer.Get("MainWindow.Tagline");
        RunningText.Text = localizer.Get("MainWindow.Running");
        MyWorkspacesText.Text = localizer.Get("MainWindow.MyWorkspaces");
        ImportButtonText.Text = localizer.Get("MainWindow.Import");
        ImportButton.ToolTip = localizer.Get("MainWindow.ImportTooltip");
        NewButton.Content = localizer.Get("MainWindow.New");
        BackgroundTitleText.Text = localizer.Get("MainWindow.BackgroundTitle");
        BackgroundHintText.Text = localizer.Get("MainWindow.BackgroundHint");

        RestoreButtonText.Text = localizer.Get("Detail.RestoreButton");
        ExportButtonText.Text = localizer.Get("Detail.ExportButton");
        DetailExportButton.ToolTip = localizer.Get("Detail.ExportTooltip");
        UpdateButtonText.Text = localizer.Get("Detail.UpdateButton");

        WindowsTabText.Text = localizer.Get("WindowDetail.TabTitle");
        WindowsListTitleText.Text = localizer.Get("WindowDetail.ListTitle");

        RestoreSettingsTitleText.Text = localizer.Get("RestoreSettings.Title");
        AutoLaunchLabelText.Text = localizer.Get("RestoreSettings.AutoLaunchLabel");
        AutoLaunchDescriptionText.Text = localizer.Get("RestoreSettings.AutoLaunchDescription");
        CloseOldLabelText.Text = localizer.Get("RestoreSettings.CloseOldLabel");
        CloseOldDescriptionText.Text = localizer.Get("RestoreSettings.CloseOldDescription");
        MinimizeTrayLabelText.Text = localizer.Get("RestoreSettings.MinimizeTrayLabel");
        MinimizeTrayDescriptionText.Text = localizer.Get("RestoreSettings.MinimizeTrayDescription");
        ActiveWorkspaceLabelText.Text = localizer.Get("RestoreSettings.ActiveWorkspaceLabel");
        TransitionDescriptionText.Text = localizer.Get("RestoreSettings.TransitionDescription");
        SwitchTaskbarLabelText.Text = localizer.Get("RestoreSettings.SwitchTaskbarLabel");
        SwitchTaskbarDescriptionText.Text = localizer.Get("RestoreSettings.SwitchTaskbarDescription");
        StaticPinsLabelText.Text = localizer.Get("RestoreSettings.StaticPinsLabel");
        StaticPinsValueText.Text = localizer.Get("RestoreSettings.StaticPinsValue");
        StaticPinsDescriptionText.Text = localizer.Get("RestoreSettings.StaticPinsDescription");
    }

    public void ShowWindow()
    {
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        Activate();
        Focus();
    }

    public void ExplicitExit()
    {
        _isExplicitExit = true;
        _trayIconService.Dispose();
        _hotkeyManager.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExplicitExit && _viewModel.MinimizeToTrayOnClose)
        {
            e.Cancel = true;
            Hide();
            _trayIconService.ShowNotification(
                "Workspace Switcher Active",
                "App is running in the background. Use global hotkeys (Ctrl+Alt+1..5) or the tray icon.",
                System.Windows.Forms.ToolTipIcon.Info
            );
        }
        else
        {
            _trayIconService.Dispose();
            _hotkeyManager.Dispose();
            base.OnClosing(e);
        }
    }
}
