using System;
using WorkspaceSwitcher.Core.Localization;
using Xunit;

namespace WorkspaceSwitcher.Tests;

/// <summary>
/// Display formatting seam (TASK-03): the localizer renders list/detail counts and
/// capture timestamps for an explicit reference time, so assertions never depend on
/// the wall clock. All times are local; the day-boundary judgment uses local days.
/// </summary>
public class DisplayFormattingTests
{
    [Theory]
    [InlineData(UiLanguage.English, "Today, 09:05")]
    [InlineData(UiLanguage.ChineseSimplified, "今天 09:05")]
    public void FormatRelativeTime_SameLocalDay_ShowsToday(UiLanguage language, string expected)
    {
        var result = new Localizer(language).FormatRelativeTime(
            new DateTime(2026, 10, 5, 9, 5, 0), new DateTime(2026, 10, 5, 14, 30, 0));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(UiLanguage.English, "Yesterday, 23:50")]
    [InlineData(UiLanguage.ChineseSimplified, "昨天 23:50")]
    public void FormatRelativeTime_LateEveningBeforeEarlyMorningReference_ShowsYesterdayByLocalDay(
        UiLanguage language, string expected)
    {
        // Elapsed time is under 24h but the local calendar day already flipped.
        var result = new Localizer(language).FormatRelativeTime(
            new DateTime(2026, 10, 4, 23, 50, 0), new DateTime(2026, 10, 5, 0, 30, 0));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(UiLanguage.English, "Yesterday, 21:00")]
    [InlineData(UiLanguage.ChineseSimplified, "昨天 21:00")]
    public void FormatRelativeTime_PreviousLocalDay_ShowsYesterday(UiLanguage language, string expected)
    {
        var result = new Localizer(language).FormatRelativeTime(
            new DateTime(2026, 10, 4, 21, 0, 0), new DateTime(2026, 10, 5, 14, 30, 0));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(UiLanguage.English, "3 days ago, 10:00")]
    [InlineData(UiLanguage.ChineseSimplified, "3 天前 10:00")]
    public void FormatRelativeTime_RecentDays_ShowsDayCount(UiLanguage language, string expected)
    {
        var result = new Localizer(language).FormatRelativeTime(
            new DateTime(2026, 10, 2, 10, 0, 0), new DateTime(2026, 10, 5, 14, 30, 0));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(UiLanguage.English, "6 days ago, 08:00")]
    [InlineData(UiLanguage.ChineseSimplified, "6 天前 08:00")]
    public void FormatRelativeTime_JustInsideRecentWindow_ShowsDayCount(UiLanguage language, string expected)
    {
        var result = new Localizer(language).FormatRelativeTime(
            new DateTime(2026, 9, 29, 8, 0, 0), new DateTime(2026, 10, 5, 14, 30, 0));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(UiLanguage.English, "28. Sep, 14:30")]
    [InlineData(UiLanguage.ChineseSimplified, "2026年9月28日 14:30")]
    public void FormatRelativeTime_SevenDayBoundary_ShowsAbsoluteDate(UiLanguage language, string expected)
    {
        // Exactly seven days old: outside the recent window.
        var result = new Localizer(language).FormatRelativeTime(
            new DateTime(2026, 9, 28, 14, 30, 0), new DateTime(2026, 10, 5, 14, 30, 0));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(UiLanguage.English, "20. Sep, 08:15")]
    [InlineData(UiLanguage.ChineseSimplified, "2026年9月20日 08:15")]
    public void FormatRelativeTime_OlderDate_ShowsLanguageAbsoluteFormat(UiLanguage language, string expected)
    {
        var result = new Localizer(language).FormatRelativeTime(
            new DateTime(2026, 9, 20, 8, 15, 0), new DateTime(2026, 10, 5, 14, 30, 0));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(UiLanguage.English, "31. Dec, 23:59")]
    [InlineData(UiLanguage.ChineseSimplified, "2025年12月31日 23:59")]
    public void FormatRelativeTime_OlderDateAcrossYear_ShowsYearInChineseFormat(UiLanguage language, string expected)
    {
        var result = new Localizer(language).FormatRelativeTime(
            new DateTime(2025, 12, 31, 23, 59, 0), new DateTime(2026, 10, 5, 14, 30, 0));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(UiLanguage.English, "ListRow.WindowCount", 5, "5w")]
    [InlineData(UiLanguage.ChineseSimplified, "ListRow.WindowCount", 5, "5 个窗口")]
    [InlineData(UiLanguage.English, "Detail.WindowsStat", 12, "12 Windows")]
    [InlineData(UiLanguage.ChineseSimplified, "Detail.WindowsStat", 12, "12 个窗口")]
    [InlineData(UiLanguage.English, "Detail.MonitorsStat", 2, "2 Monitors")]
    [InlineData(UiLanguage.ChineseSimplified, "Detail.MonitorsStat", 2, "2 台显示器")]
    [InlineData(UiLanguage.English, "Detail.PinsStat", 3, "3 Taskbar Pins")]
    [InlineData(UiLanguage.ChineseSimplified, "Detail.PinsStat", 3, "3 个任务栏固定项")]
    public void Format_CountTemplates_RenderCompletePhrases(
        UiLanguage language, string key, int count, string expected)
    {
        var result = new Localizer(language).Format(key, count);

        Assert.Equal(expected, result);
        Assert.DoesNotContain("{0}", result);
    }

    [Theory]
    [InlineData(UiLanguage.English, "Today, 09:05", "Captured: Today, 09:05")]
    [InlineData(UiLanguage.ChineseSimplified, "今天 09:05", "保存时间：今天 09:05")]
    public void Format_Captured_PrefixesRelativeTime(UiLanguage language, string relativeTime, string expected)
    {
        Assert.Equal(expected, new Localizer(language).Format("Detail.Captured", relativeTime));
    }

    [Theory]
    [InlineData(UiLanguage.English, "Workspace.DescriptionFallback", "workspace")]
    [InlineData(UiLanguage.ChineseSimplified, "Workspace.DescriptionFallback", "工作区")]
    [InlineData(UiLanguage.English, "Detail.NoSelectionTitle", "Select a Workspace")]
    [InlineData(UiLanguage.ChineseSimplified, "Detail.NoSelectionTitle", "选择工作区")]
    [InlineData(UiLanguage.English, "Detail.NoSelectionSubtitle", "Choose a workspace from the left panel")]
    [InlineData(UiLanguage.ChineseSimplified, "Detail.NoSelectionSubtitle", "请从左侧选择一个工作区")]
    [InlineData(UiLanguage.English, "RestoreSettings.ActiveNone", "None")]
    [InlineData(UiLanguage.ChineseSimplified, "RestoreSettings.ActiveNone", "无")]
    public void Get_DisplayFallbackTexts_ResolveToLocalizedWords(UiLanguage language, string key, string expected)
    {
        // No-selection, empty-description and "active workspace: none" fallbacks.
        var text = new Localizer(language).Get(key);

        Assert.Equal(expected, text);
        Assert.NotEqual(key, text);
    }
}
