using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using WorkspaceSwitcher.Core.Hotkeys;
using WorkspaceSwitcher.Core.Localization;
using WorkspaceSwitcher.Core.Models;
using WorkspaceSwitcher.Core.Services;

namespace WorkspaceSwitcher.UI.ViewModels;

public class ProfileItemViewModel : INotifyPropertyChanged
{
    private WorkspaceProfile _profile;
    private readonly int _colorIndex;
    private readonly Action<ProfileItemViewModel>? _onProfileUpdated;
    private readonly Action<TaskbarItemViewModel, ProfileItemViewModel>? _onTaskbarStaticToggled;
    private bool _isActive;

    public ObservableCollection<WindowItemViewModel> WindowItems { get; } = new();
    public ObservableCollection<TaskbarItemViewModel> TaskbarItems { get; } = new();

    public bool HasTaskbarConfig => _profile.Taskbar != null && _profile.Taskbar.PinnedItems.Count > 0;
    public int TaskbarItemCount => _profile.Taskbar?.PinnedItems.Count ?? 0;

    public bool IsTaskbarEnabled
    {
        get => _profile.Taskbar?.Enabled ?? false;
        set
        {
            if (_profile.Taskbar == null)
            {
                _profile.Taskbar = new TaskbarConfiguration { Enabled = value };
            }
            else
            {
                _profile.Taskbar.Enabled = value;
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasTaskbarConfig));
            _onProfileUpdated?.Invoke(this);
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value)
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }
    }

    public WorkspaceProfile Profile
    {
        get => _profile;
        set
        {
            _profile = value;
            ReloadWindowItems();
            NotifyAll();
        }
    }

    public string Name => _profile.Name;
    public string Description => string.IsNullOrWhiteSpace(_profile.Description)
        ? Localizer.Current.Get("Workspace.DescriptionFallback")
        : _profile.Description;
    
    public string IconGlyph
    {
        get => string.IsNullOrWhiteSpace(_profile.IconGlyph) ? "💻" : _profile.IconGlyph;
        set
        {
            if (_profile.IconGlyph != value)
            {
                _profile.IconGlyph = value;
                OnPropertyChanged();
                _onProfileUpdated?.Invoke(this);
            }
        }
    }

    public static IReadOnlyList<string> AvailableIcons { get; } = new[]
    {
        "💻", "🎮", "📚", "💼", "🎨", "🚀", "🌐", "⚙️", "🎬", "🎧", "⚡", "🔥", "🏆", "📱", "💡", "☕"
    };

    public string HotkeyModifier
    {
        get => string.IsNullOrWhiteSpace(_profile.HotkeyModifier) ? "Ctrl + Alt" : _profile.HotkeyModifier;
        set
        {
            if (_profile.HotkeyModifier != value)
            {
                _profile.HotkeyModifier = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayHotkey));
                _onProfileUpdated?.Invoke(this);
            }
        }
    }

    public string HotkeyKey
    {
        get => _profile.HotkeyKey ?? "Auto (1-5)";
        set
        {
            if (_profile.HotkeyKey != value)
            {
                _profile.HotkeyKey = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayHotkey));
                _onProfileUpdated?.Invoke(this);
            }
        }
    }

    public string DisplayHotkey => HotkeyDisplay.Format(Localizer.Current, _profile.HotkeyModifier, _profile.HotkeyKey, _colorIndex);

    public int WindowCount => _profile.Windows?.Count ?? 0;

    public int MonitorCount
    {
        get
        {
            if (_profile.Windows == null || _profile.Windows.Count == 0) return 1;
            var monitors = MonitorService.GetConnectedMonitors();
            var detected = new HashSet<int>();
            foreach (var w in _profile.Windows)
            {
                var p = w.Placement.NormalPosition;
                int midX = p.Left + (p.Width / 2);
                int midY = p.Top + (p.Height / 2);

                var mon = monitors.FirstOrDefault(m => midX >= m.Left && midX < m.Left + m.Width && midY >= m.Top && midY < m.Top + m.Height)
                    ?? monitors.FirstOrDefault(m => p.Left >= m.Left && p.Left < m.Left + m.Width);

                if (mon != null)
                {
                    detected.Add(mon.Index);
                }
            }
            return Math.Max(1, detected.Count);
        }
    }

    /// <summary>
    /// Capture timestamp rendered for the current language. The day-boundary
    /// judgment runs in local time against the wall clock here; the formatting
    /// itself is deterministic per explicit reference time (Localizer.FormatRelativeTime).
    /// </summary>
    public string RelativeTime =>
        Localizer.Current.FormatRelativeTime(_profile.LastModifiedAt.ToLocalTime(), DateTime.Now);

    // Display strings consumed by MainWindow.xaml (TASK-03). All text and count
    // formatting comes from the localization facade; the view holds no format fragments.
    public string ActiveBadgeText => Localizer.Current.Get("ListRow.ActiveBadge");
    public string ApplyTooltip => Localizer.Current.Get("ListRow.ApplyTooltip");
    public string EditTooltip => Localizer.Current.Get("ListRow.EditTooltip");
    public string ExportTooltip => Localizer.Current.Get("ListRow.ExportTooltip");
    public string DeleteTooltip => Localizer.Current.Get("ListRow.DeleteTooltip");

    public string WindowCountDisplay => Localizer.Current.Format("ListRow.WindowCount", WindowCount);
    public string MonitorCountDisplay => Localizer.Current.Format("ListRow.MonitorCount", MonitorCount);
    public string TaskbarPinCountDisplay => Localizer.Current.Format("ListRow.PinCount", TaskbarItemCount);

    public string WindowsStatDisplay => Localizer.Current.Format("Detail.WindowsStat", WindowCount);
    public string MonitorsStatDisplay => Localizer.Current.Format("Detail.MonitorsStat", MonitorCount);
    public string TaskbarPinsStatDisplay => Localizer.Current.Format("Detail.PinsStat", TaskbarItemCount);
    public string CapturedDisplay => Localizer.Current.Format("Detail.Captured", RelativeTime);

    public System.Windows.Media.Brush IconBrush
    {
        get
        {
            return (_colorIndex % 4) switch
            {
                0 => new SolidColorBrush(System.Windows.Media.Color.FromRgb(99, 102, 241)), // Indigo
                1 => new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)), // Emerald Green
                2 => new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)), // Blue
                _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 92, 246))  // Purple
            };
        }
    }

    public ProfileItemViewModel(
        WorkspaceProfile profile, 
        int index = 0, 
        Action<ProfileItemViewModel>? onProfileUpdated = null,
        Action<TaskbarItemViewModel, ProfileItemViewModel>? onTaskbarStaticToggled = null)
    {
        _profile = profile;
        _colorIndex = index;
        _onProfileUpdated = onProfileUpdated;
        _onTaskbarStaticToggled = onTaskbarStaticToggled;

        ReloadWindowItems();
        ReloadTaskbarItems();
    }

    public void ReloadTaskbarItems()
    {
        TaskbarItems.Clear();
        if (_profile.Taskbar?.PinnedItems != null)
        {
            foreach (var item in _profile.Taskbar.PinnedItems)
            {
                TaskbarItems.Add(new TaskbarItemViewModel(
                    item,
                    onStaticToggled: OnTaskbarItemToggled,
                    onRemove: OnTaskbarItemRemoved
                ));
            }
        }
        OnPropertyChanged(nameof(HasTaskbarConfig));
        OnPropertyChanged(nameof(TaskbarItemCount));
        OnPropertyChanged(nameof(IsTaskbarEnabled));
        NotifyDisplayStats();
    }

    private void OnTaskbarItemToggled(TaskbarItemViewModel item)
    {
        _onTaskbarStaticToggled?.Invoke(item, this);
        _onProfileUpdated?.Invoke(this);
    }

    private void OnTaskbarItemRemoved(TaskbarItemViewModel item)
    {
        TaskbarItems.Remove(item);
        _profile.Taskbar?.PinnedItems.Remove(item.Model);
        OnPropertyChanged(nameof(HasTaskbarConfig));
        OnPropertyChanged(nameof(TaskbarItemCount));
        NotifyDisplayStats();
        _onProfileUpdated?.Invoke(this);
    }

    private void ReloadWindowItems()
    {
        WindowItems.Clear();
        if (_profile.Windows != null)
        {
            foreach (var w in _profile.Windows)
            {
                WindowItems.Add(new WindowItemViewModel(
                    w,
                    onChanged: OnWindowChanged,
                    onRemove: OnWindowRemoved
                ));
            }
        }
    }

    private void OnWindowChanged()
    {
        OnPropertyChanged(nameof(WindowCount));
        OnPropertyChanged(nameof(MonitorCount));
        NotifyDisplayStats();
        _onProfileUpdated?.Invoke(this);
    }

    private void OnWindowRemoved(WindowItemViewModel item)
    {
        WindowItems.Remove(item);
        _profile.Windows?.Remove(item.Model);
        OnPropertyChanged(nameof(WindowCount));
        OnPropertyChanged(nameof(MonitorCount));
        NotifyDisplayStats();
        _onProfileUpdated?.Invoke(this);
    }

    public void NotifyAll()
    {
        OnPropertyChanged(nameof(Profile));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(WindowCount));
        OnPropertyChanged(nameof(MonitorCount));
        OnPropertyChanged(nameof(RelativeTime));
        OnPropertyChanged(nameof(CapturedDisplay));
        OnPropertyChanged(nameof(WindowItems));
        OnPropertyChanged(nameof(TaskbarItems));
        OnPropertyChanged(nameof(HasTaskbarConfig));
        OnPropertyChanged(nameof(TaskbarItemCount));
        OnPropertyChanged(nameof(IsTaskbarEnabled));
        OnPropertyChanged(nameof(IsActive));
        NotifyDisplayStats();
    }

    /// <summary>
    /// Raises change notifications for every preformatted count display so list rows
    /// and detail stats stay in sync with the underlying counts.
    /// </summary>
    private void NotifyDisplayStats()
    {
        OnPropertyChanged(nameof(WindowCountDisplay));
        OnPropertyChanged(nameof(MonitorCountDisplay));
        OnPropertyChanged(nameof(TaskbarPinCountDisplay));
        OnPropertyChanged(nameof(WindowsStatDisplay));
        OnPropertyChanged(nameof(MonitorsStatDisplay));
        OnPropertyChanged(nameof(TaskbarPinsStatDisplay));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
