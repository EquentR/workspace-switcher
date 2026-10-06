using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using WorkspaceSwitcher.Core;
using WorkspaceSwitcher.Core.Localization;
using WorkspaceSwitcher.Core.Services;

namespace WorkspaceSwitcher.UI.Services;

public class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly WindowManager _windowManager;
    private readonly IProfileService _profileService;
    private readonly Action _showMainWindowAction;
    private readonly Action _exitAppAction;
    private readonly Action<string>? _switchWorkspaceAction;

    public TrayIconService(
        WindowManager windowManager,
        IProfileService profileService,
        Action showMainWindowAction,
        Action exitAppAction,
        Action<string>? switchWorkspaceAction = null)
    {
        _windowManager = windowManager;
        _profileService = profileService;
        _showMainWindowAction = showMainWindowAction;
        _exitAppAction = exitAppAction;
        _switchWorkspaceAction = switchWorkspaceAction;

        _notifyIcon = new NotifyIcon
        {
            Text = Localizer.Current.Get("Tray.HoverText"),
            Visible = true,
            Icon = GenerateAppIcon()
        };

        _notifyIcon.DoubleClick += (s, e) => _showMainWindowAction();
        _notifyIcon.MouseClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                _showMainWindowAction();
            }
        };

        RebuildContextMenu();
    }

    public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon.ShowBalloonTip(3000, title, message, icon);
    }

    public void RebuildContextMenu()
    {
        // Every rebuild reads the process-wide facade, so the menu always uses this
        // session's effective language even after a language preference save.
        var localizer = Localizer.Current;
        var menu = new ContextMenuStrip();

        // Title item (brand name stays literal)
        var titleItem = new ToolStripMenuItem("🪟 Workspace Switcher")
        {
            Enabled = false,
            Font = new Font(FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Bold)
        };
        menu.Items.Add(titleItem);
        menu.Items.Add(new ToolStripSeparator());

        // Dynamic Profiles Submenu
        var profiles = _profileService.GetProfileNames();
        var quickSwitchMenu = new ToolStripMenuItem("⚡ " + localizer.Get("Tray.QuickSwitchMenu"));

        if (profiles.Count == 0)
        {
            quickSwitchMenu.DropDownItems.Add(new ToolStripMenuItem(localizer.Get("Tray.NoWorkspacesHint")) { Enabled = false });
        }
        else
        {
            foreach (var name in profiles)
            {
                var item = new ToolStripMenuItem(name);
                item.Click += (s, e) =>
                {
                    if (_switchWorkspaceAction != null)
                    {
                        _switchWorkspaceAction(name);
                    }
                    else
                    {
                        // Fallback restore path: same localized switch notification
                        // as the wired tray switch.
                        var profile = _profileService.LoadProfile(name);
                        if (profile != null)
                        {
                            _windowManager.RestoreWorkspace(profile, launchIfNotRunning: false);
                            ShowNotification(
                                Localizer.Current.Get("Tray.SwitchNotificationTitle"),
                                Localizer.Current.Format("Tray.SwitchNotificationMessage", profile.Name));
                        }
                    }
                };
                quickSwitchMenu.DropDownItems.Add(item);
            }
        }
        menu.Items.Add(quickSwitchMenu);

        // Snapshot Action. The Quick_ name prefix is a stable snapshot identifier and
        // stays verbatim; only the description of NEW snapshots is localized.
        var snapshotItem = new ToolStripMenuItem("📸 " + localizer.Get("Tray.SnapshotMenu"), null, (s, e) =>
        {
            string profileName = $"Quick_{DateTime.Now:HHmmss}";
            var profile = _windowManager.CaptureWorkspace(profileName, Localizer.Current.Get("Tray.SnapshotDescription"));
            _profileService.SaveProfile(profile);
            RebuildContextMenu();
            ShowNotification(
                Localizer.Current.Get("Tray.SnapshotNotificationTitle"),
                Localizer.Current.Format("Tray.SnapshotNotificationMessage", profileName, profile.Windows.Count));
        });
        menu.Items.Add(snapshotItem);

        menu.Items.Add(new ToolStripSeparator());

        // Open Dashboard
        var openItem = new ToolStripMenuItem("⚙️ " + localizer.Get("Tray.OpenMenu"), null, (s, e) => _showMainWindowAction());
        menu.Items.Add(openItem);

        // Exit
        var exitItem = new ToolStripMenuItem("❌ " + localizer.Get("Tray.ExitMenu"), null, (s, e) => _exitAppAction());
        menu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = menu;
    }

    private static Icon GenerateAppIcon()
    {
        try
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(iconPath))
            {
                return new Icon(iconPath, 32, 32);
            }

            if (!string.IsNullOrEmpty(Environment.ProcessPath))
            {
                var exeIcon = Icon.ExtractAssociatedIcon(Environment.ProcessPath);
                if (exeIcon != null) return exeIcon;
            }
        }
        catch
        {
            // Fallback to drawn icon
        }

        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var bgBrush = new SolidBrush(Color.FromArgb(17, 22, 42));
        g.FillRectangle(bgBrush, 0, 0, 32, 32);

        using var tileBrush1 = new SolidBrush(Color.FromArgb(99, 102, 241));
        using var tileBrush2 = new SolidBrush(Color.FromArgb(96, 205, 255));
        using var tileBrush3 = new SolidBrush(Color.FromArgb(168, 85, 247));
        using var tileBrush4 = new SolidBrush(Color.FromArgb(16, 185, 129));

        g.FillRectangle(tileBrush1, 4, 4, 10, 10);
        g.FillRectangle(tileBrush2, 17, 4, 11, 10);
        g.FillRectangle(tileBrush3, 4, 17, 10, 11);
        g.FillRectangle(tileBrush4, 17, 17, 11, 11);

        IntPtr hIcon = bmp.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        GC.SuppressFinalize(this);
    }
}
