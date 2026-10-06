using System.Linq;
using WorkspaceSwitcher.Core.Localization;
using WorkspaceSwitcher.Core.Models;
using WorkspaceSwitcher.Core.Services;
using Xunit;

namespace WorkspaceSwitcher.Tests;

/// <summary>
/// Window-details display seam (TASK-04): state labels and monitor names are rendered
/// through the localization facade over simulated monitor data (index, primary flag,
/// resolution, position) — no hardware enumeration runs in these tests. Persisted enum
/// values stay in the binding/save path and are never replaced by display text.
/// </summary>
public class WindowDetailsDisplayTests
{
    [Theory]
    [InlineData(UiLanguage.English, WindowState.Normal, "Normal")]
    [InlineData(UiLanguage.English, WindowState.Maximized, "Maximized")]
    [InlineData(UiLanguage.English, WindowState.Minimized, "Minimized")]
    [InlineData(UiLanguage.ChineseSimplified, WindowState.Normal, "正常")]
    [InlineData(UiLanguage.ChineseSimplified, WindowState.Maximized, "最大化")]
    [InlineData(UiLanguage.ChineseSimplified, WindowState.Minimized, "最小化")]
    public void StateText_PersistedEnumValue_RendersLocalizedLabel(
        UiLanguage language, WindowState state, string expected)
    {
        Assert.Equal(expected, WindowDetailsDisplay.StateText(new Localizer(language), state));
    }

    [Theory]
    [InlineData(UiLanguage.English)]
    [InlineData(UiLanguage.ChineseSimplified)]
    public void StateText_UnknownEnumValue_KeepsRawEnumName(UiLanguage language)
    {
        // Out-of-range persisted values must never render as a guessed translation.
        Assert.Equal("99", WindowDetailsDisplay.StateText(new Localizer(language), (WindowState)99));
    }

    [Theory]
    [InlineData(UiLanguage.English, "Normal", "Maximized", "Minimized")]
    [InlineData(UiLanguage.ChineseSimplified, "正常", "最大化", "最小化")]
    public void CreateStateOptions_KeepEnumValuesAndMatchStatusLabels(
        UiLanguage language, string normal, string maximized, string minimized)
    {
        var localizer = new Localizer(language);
        var options = WindowDetailsDisplay.CreateStateOptions(localizer);

        // Binding/persistence keeps the original enum values and order.
        Assert.Equal(
            new[] { WindowState.Normal, WindowState.Maximized, WindowState.Minimized },
            options.Select(o => o.Value).ToArray());
        Assert.Equal(new[] { normal, maximized, minimized }, options.Select(o => o.Display).ToArray());

        // Dropdown options and the status label consume the same strings.
        Assert.All(options, o => Assert.Equal(WindowDetailsDisplay.StateText(localizer, o.Value), o.Display));
    }

    [Theory]
    [InlineData(UiLanguage.English, 1, true, 1920, 1080, 0, 0, "Monitor 1 (Primary - 1920×1080)")]
    [InlineData(UiLanguage.ChineseSimplified, 1, true, 1920, 1080, 0, 0, "显示器 1（主显示器，1920×1080）")]
    [InlineData(UiLanguage.English, 2, false, 2560, 1440, -2560, -200, "Monitor 2 (2560×1440 at -2560,-200)")]
    [InlineData(UiLanguage.ChineseSimplified, 2, false, 2560, 1440, -2560, -200, "显示器 2（2560×1440，位置 -2560, -200）")]
    public void MonitorName_SimulatedMonitorData_RendersCompleteLocalizedNames(
        UiLanguage language, int index, bool isPrimary, int width, int height, int left, int top, string expected)
    {
        Assert.Equal(
            expected,
            WindowDetailsDisplay.MonitorName(new Localizer(language), index, isPrimary, width, height, left, top));
    }

    [Theory]
    [InlineData(UiLanguage.English, 1, "Monitor 1")]
    [InlineData(UiLanguage.English, 3, "Monitor 3")]
    [InlineData(UiLanguage.ChineseSimplified, 1, "显示器 1")]
    [InlineData(UiLanguage.ChineseSimplified, 3, "显示器 3")]
    public void MonitorShortText_Index_RendersCompactName(UiLanguage language, int index, string expected)
    {
        Assert.Equal(expected, WindowDetailsDisplay.MonitorShortText(new Localizer(language), index));
    }

    [Fact]
    public void CreateMonitorOption_FormatsFromStructuredData_NotFromEnglishOptionName()
    {
        var monitor = new MonitorOption
        {
            Index = 2,
            Name = "English name that must never be parsed",
            Left = -1920,
            Top = 0,
            Width = 1920,
            Height = 1080,
            IsPrimary = false
        };

        var option = WindowDetailsDisplay.CreateMonitorOption(new Localizer(UiLanguage.ChineseSimplified), monitor);

        Assert.Same(monitor, option.Monitor);
        Assert.Equal("显示器 2（1920×1080，位置 -1920, 0）", option.Display);
    }
}
