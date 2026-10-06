using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using WorkspaceSwitcher.Core.Localization;
using WorkspaceSwitcher.Core.Models;
using WorkspaceSwitcher.Core.Services;
using WorkspaceSwitcher.UI.Services;

namespace WorkspaceSwitcher.UI.ViewModels;

public class WindowItemViewModel : INotifyPropertyChanged
{
    private readonly Action? _onChanged;
    private readonly Action<WindowItemViewModel>? _onRemove;
    private bool _isExpanded;

    public WindowInfo Model { get; }
    public string DisplayName => Model.DisplayName;
    public string Subtitle => !string.IsNullOrWhiteSpace(Model.ExecutablePath) ? System.IO.Path.GetFileName(Model.ExecutablePath) : Model.ProcessName;
    public string ProcessName => Model.ProcessName;
    public string WindowTitle => string.IsNullOrWhiteSpace(Model.WindowTitle) ? Model.DisplayName : Model.WindowTitle;
    public string ExecutablePath => Model.ExecutablePath ?? Localizer.Current.Get("WindowDetail.SystemProcess");

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            _isExpanded = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ChevronIcon));
        }
    }

    public string ChevronIcon => IsExpanded ? "∧" : "⌵";

    public WindowState State
    {
        get => Model.Placement.State;
        set
        {
            if (Model.Placement.State != value)
            {
                Model.Placement.State = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StateText));
                OnPropertyChanged(nameof(StateName));
                _onChanged?.Invoke();
            }
        }
    }

    /// <summary>Localized status label; the persisted enum value stays in <see cref="State"/>.</summary>
    public string StateText => WindowDetailsDisplay.StateText(Localizer.Current, State);

    /// <summary>Stable enum name driving the state badge styling; never translated.</summary>
    public string StateName => State.ToString();

    public IReadOnlyList<WindowStateOption> StateOptions { get; }

    public List<MonitorDisplayOption> AvailableMonitors { get; }

    public MonitorDisplayOption? SelectedMonitor
    {
        get
        {
            var p = Model.Placement.NormalPosition;
            int midX = p.Left + (p.Width / 2);
            int midY = p.Top + (p.Height / 2);

            return AvailableMonitors.FirstOrDefault(m => midX >= m.Monitor.Left && midX < m.Monitor.Left + m.Monitor.Width && midY >= m.Monitor.Top && midY < m.Monitor.Top + m.Monitor.Height)
                ?? AvailableMonitors.FirstOrDefault(m => p.Left >= m.Monitor.Left && p.Left < m.Monitor.Left + m.Monitor.Width)
                ?? AvailableMonitors.FirstOrDefault();
        }
        set
        {
            if (value != null)
            {
                var currentMon = SelectedMonitor;
                if (currentMon != null && currentMon.Monitor.Index != value.Monitor.Index)
                {
                    int offsetX = value.Monitor.Left - currentMon.Monitor.Left;
                    int offsetY = value.Monitor.Top - currentMon.Monitor.Top;

                    Model.Placement.NormalPosition.Left += offsetX;
                    Model.Placement.NormalPosition.Right += offsetX;
                    Model.Placement.NormalPosition.Top += offsetY;
                    Model.Placement.NormalPosition.Bottom += offsetY;

                    Model.Bounds.Left += offsetX;
                    Model.Bounds.Right += offsetX;
                    Model.Bounds.Top += offsetY;
                    Model.Bounds.Bottom += offsetY;

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Left));
                    OnPropertyChanged(nameof(Top));
                    OnPropertyChanged(nameof(MonitorDisplay));
                    OnPropertyChanged(nameof(FormattedCoordinates));
                    _onChanged?.Invoke();
                }
            }
        }
    }

    public string MonitorDisplay
    {
        get
        {
            var mon = SelectedMonitor;
            return WindowDetailsDisplay.MonitorShortText(Localizer.Current, mon?.Monitor.Index ?? 1);
        }
    }

    public int Left
    {
        get => Model.Placement.NormalPosition.Left;
        set
        {
            int w = Width;
            Model.Placement.NormalPosition.Left = value;
            Model.Placement.NormalPosition.Right = value + w;
            Model.Bounds.Left = value;
            Model.Bounds.Right = value + w;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FormattedCoordinates));
            OnPropertyChanged(nameof(MonitorDisplay));
            OnPropertyChanged(nameof(SelectedMonitor));
            _onChanged?.Invoke();
        }
    }

    public int Top
    {
        get => Model.Placement.NormalPosition.Top;
        set
        {
            int h = Height;
            Model.Placement.NormalPosition.Top = value;
            Model.Placement.NormalPosition.Bottom = value + h;
            Model.Bounds.Top = value;
            Model.Bounds.Bottom = value + h;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FormattedCoordinates));
            OnPropertyChanged(nameof(MonitorDisplay));
            OnPropertyChanged(nameof(SelectedMonitor));
            _onChanged?.Invoke();
        }
    }

    public int Width
    {
        get => Model.Placement.NormalPosition.Width;
        set
        {
            int w = Math.Max(100, value);
            Model.Placement.NormalPosition.Right = Model.Placement.NormalPosition.Left + w;
            Model.Bounds.Right = Model.Bounds.Left + w;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FormattedCoordinates));
            _onChanged?.Invoke();
        }
    }

    public int Height
    {
        get => Model.Placement.NormalPosition.Height;
        set
        {
            int h = Math.Max(100, value);
            Model.Placement.NormalPosition.Bottom = Model.Placement.NormalPosition.Top + h;
            Model.Bounds.Bottom = Model.Bounds.Top + h;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FormattedCoordinates));
            _onChanged?.Invoke();
        }
    }

    public string FormattedCoordinates
    {
        get
        {
            var p = Model.Placement.NormalPosition;
            return $"{p.Left}, {p.Top} • {p.Width} × {p.Height}";
        }
    }

    public ImageSource? AppIcon { get; }

    // Static window-details labels (TASK-04) consumed by the item template. All text
    // comes from the localization facade; the view holds no format fragments.
    public string StateLabel => Localizer.Current.Get("WindowDetail.StateLabel");
    public string MonitorLabel => Localizer.Current.Get("WindowDetail.MonitorLabel");
    public string LeftLabel => Localizer.Current.Get("WindowDetail.LeftLabel");
    public string TopLabel => Localizer.Current.Get("WindowDetail.TopLabel");
    public string WidthLabel => Localizer.Current.Get("WindowDetail.WidthLabel");
    public string HeightLabel => Localizer.Current.Get("WindowDetail.HeightLabel");
    public string AutoSaveHint => Localizer.Current.Get("WindowDetail.AutoSaveHint");
    public string RemoveButtonText => Localizer.Current.Get("WindowDetail.RemoveButton");

    public ICommand ToggleExpandCommand { get; }
    public ICommand RemoveWindowCommand { get; }

    public WindowItemViewModel(
        WindowInfo windowInfo, 
        Action? onChanged = null, 
        Action<WindowItemViewModel>? onRemove = null)
    {
        Model = windowInfo;
        _onChanged = onChanged;
        _onRemove = onRemove;

        AppIcon = IconHelper.GetIconForExecutable(windowInfo.ExecutablePath, windowInfo.ProcessName);
        AvailableMonitors = MonitorService.GetConnectedMonitors()
            .Select(m => WindowDetailsDisplay.CreateMonitorOption(Localizer.Current, m))
            .ToList();
        StateOptions = WindowDetailsDisplay.CreateStateOptions(Localizer.Current);

        ToggleExpandCommand = new RelayCommand(() => IsExpanded = !IsExpanded);
        RemoveWindowCommand = new RelayCommand(() => _onRemove?.Invoke(this));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
