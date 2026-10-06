using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
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
                    _trayIconService?.ShowNotification(
                        Localizer.Current.Get("Tray.SwitchNotificationTitle"),
                        Localizer.Current.Format("Tray.SwitchNotificationMessage", profile.Name));
                }
            }
        );
    }

    /// <summary>
    /// Replaces the design-time English texts of the main window overview, workspace
    /// list chrome, taskbar area and Restore Settings card with the effective
    /// language's strings. Dynamic texts (counts, capture time, fallbacks, scope
    /// status) come from the view models.
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

        TaskbarTabText.Text = localizer.Get("Taskbar.Title");
        TaskbarTitleText.Text = localizer.Get("Taskbar.Title");
        TaskbarBetaBadgeText.Text = localizer.Get("Taskbar.BetaBadge");
        TaskbarBetaNoticeText.Text = localizer.Get("Taskbar.BetaNotice");
        TaskbarEnabledText.Text = localizer.Get("Taskbar.EnabledLabel");
        string snapshotButton = localizer.Get("Taskbar.SnapshotButton");
        SnapshotTaskbarButtonText.Text = snapshotButton;
        SnapshotEmptyButtonText.Text = snapshotButton;
        ApplyTaskbarButtonText.Text = localizer.Get("Taskbar.ApplyButton");
        SyncStaticPinsButtonText.Text = localizer.Get("Taskbar.SyncButton");
        SyncStaticPinsButton.ToolTip = localizer.Get("Taskbar.SyncTooltip");
        TaskbarEmptyTitleText.Text = localizer.Get("Taskbar.EmptyTitle");
        TaskbarEmptyDescriptionText.Text = localizer.Get("Taskbar.EmptyDescription");

        // Toggle help: complete localized sentences split at the scope terms so the
        // key terms stay emphasized without fragment-spliced word order.
        ToggleHelpText.Inlines.Clear();
        foreach (var segment in TaskbarDisplay.CreateToggleHelpSegments(localizer))
        {
            var run = new Run(segment.Text);
            switch (segment.Kind)
            {
                case TaskbarHelpSegmentKind.StaticTerm:
                    run.FontWeight = FontWeights.Bold;
                    run.Foreground = (System.Windows.Media.Brush)FindResource("AccentIndigoBrush");
                    break;
                case TaskbarHelpSegmentKind.WorkspaceOnlyTerm:
                    run.FontWeight = FontWeights.Bold;
                    run.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0x60, 0xA5, 0xFA));
                    break;
                default:
                    run.Foreground = (System.Windows.Media.Brush)FindResource("TextMutedBrush");
                    break;
            }
            ToggleHelpText.Inlines.Add(run);
        }

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
                Localizer.Current.Get("Tray.CloseToTrayTitle"),
                Localizer.Current.Get("Tray.CloseToTrayMessage"),
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
