using System;
using System.Collections.Generic;
using WorkspaceSwitcher.Core.Models;
using WorkspaceSwitcher.Core.Services;

namespace WorkspaceSwitcher.Core.Localization;

/// <summary>
/// A window-state selector option: <see cref="Value"/> is the persisted
/// <see cref="WindowState"/> enum value bound and saved unchanged, while
/// <see cref="Display"/> is the language-dependent label rendered in the UI.
/// Localized display text must never be written back to the workspace profile.
/// </summary>
public sealed record WindowStateOption(WindowState Value, string Display);

/// <summary>
/// A monitor selector option: <see cref="Monitor"/> carries the structured monitor data
/// used for positioning, while <see cref="Display"/> is the localized name formatted
/// from that structured data. The English <see cref="MonitorOption.Name"/> string is
/// never parsed or reused for display.
/// </summary>
public sealed record MonitorDisplayOption(MonitorOption Monitor, string Display);

/// <summary>
/// Language-dependent display text for the window-details area (TASK-04). Window states
/// keep their enum values and monitor names are formatted from structured monitor data
/// (index, primary flag, resolution, position) through complete localized templates.
/// </summary>
public static class WindowDetailsDisplay
{
    /// <summary>Selector options for the window-state combo box.</summary>
    public static IReadOnlyList<WindowStateOption> CreateStateOptions(Localizer localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        return new[]
        {
            new WindowStateOption(WindowState.Normal, StateText(localizer, WindowState.Normal)),
            new WindowStateOption(WindowState.Maximized, StateText(localizer, WindowState.Maximized)),
            new WindowStateOption(WindowState.Minimized, StateText(localizer, WindowState.Minimized))
        };
    }

    /// <summary>
    /// Localized label for a persisted window-state enum value. Unknown values keep the
    /// raw enum name instead of a guessed translation.
    /// </summary>
    public static string StateText(Localizer localizer, WindowState state)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        return state switch
        {
            WindowState.Normal => localizer.Get("WindowState.Normal"),
            WindowState.Maximized => localizer.Get("WindowState.Maximized"),
            WindowState.Minimized => localizer.Get("WindowState.Minimized"),
            _ => state.ToString()
        };
    }

    /// <summary>Compact monitor name for list rows ("Monitor 2" / "显示器 2").</summary>
    public static string MonitorShortText(Localizer localizer, int index)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        return localizer.Format("Monitor.Short", index);
    }

    /// <summary>
    /// Full monitor name formatted from structured monitor data. Primary monitors use the
    /// primary template; other monitors include their virtual-desktop position, so
    /// negative coordinates keep their minus sign.
    /// </summary>
    public static string MonitorName(Localizer localizer, int index, bool isPrimary, int width, int height, int left, int top)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        return isPrimary
            ? localizer.Format("Monitor.Primary", index, width, height)
            : localizer.Format("Monitor.NonPrimary", index, width, height, left, top);
    }

    /// <summary>Wraps enumerated monitor data with its localized display name.</summary>
    public static MonitorDisplayOption CreateMonitorOption(Localizer localizer, MonitorOption monitor)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentNullException.ThrowIfNull(monitor);

        return new MonitorDisplayOption(
            monitor,
            MonitorName(localizer, monitor.Index, monitor.IsPrimary, monitor.Width, monitor.Height, monitor.Left, monitor.Top));
    }
}
