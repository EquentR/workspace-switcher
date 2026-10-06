using System;
using WorkspaceSwitcher.Core;
using WorkspaceSwitcher.Core.Localization;
using Xunit;

namespace WorkspaceSwitcher.Tests;

/// <summary>
/// Behavior tests for the TASK-06 operation display facade: restore/switch result
/// combinations, switch-source display names, known-failure explanations and file
/// dialog strings. Expected texts come from docs/specs/i18n-zh-CN.md.
/// </summary>
public class OperationDisplayTests
{
    private static Localizer L(UiLanguage language) => new(language);

    [Theory]
    // No previous workspace, no taskbar switch.
    [InlineData(UiLanguage.English, "Work", 5, null, 0, null, null,
        "Restored 'Work' (5 windows repositioned).")]
    [InlineData(UiLanguage.English, "Work", 0, null, 0, null, null,
        "Restored 'Work' (0 windows repositioned).")]
    [InlineData(UiLanguage.ChineseSimplified, "工作区 {0} 🎮", 5, null, 0, null, null,
        "已恢复工作区‘工作区 {0} 🎮’（重新定位 5 个窗口）。")]
    [InlineData(UiLanguage.ChineseSimplified, "工作区 {0} 🎮", 0, null, 0, null, null,
        "已恢复工作区‘工作区 {0} 🎮’（重新定位 0 个窗口）。")]
    // No previous workspace, with taskbar switch.
    [InlineData(UiLanguage.English, "Work", 5, null, 0, 3, null,
        "Restored 'Work' (5 windows repositioned, taskbar switched to 3 pins).")]
    [InlineData(UiLanguage.ChineseSimplified, "工作区 {0} 🎮", 5, null, 0, 3, null,
        "已恢复工作区‘工作区 {0} 🎮’（重新定位 5 个窗口，任务栏已切换为 3 个固定项）。")]
    // Previous workspace, no taskbar switch; closed count and old name stay verbatim.
    [InlineData(UiLanguage.English, "Work", 5, "Old", 2, null, null,
        "Switched to 'Work' (5 repositioned, 2 closed from 'Old').")]
    [InlineData(UiLanguage.English, "Work", 5, "Old", 0, null, null,
        "Switched to 'Work' (5 repositioned, 0 closed from 'Old').")]
    [InlineData(UiLanguage.ChineseSimplified, "工作区 {0} 🎮", 5, "旧 {1} ‘x’", 2, null, null,
        "已切换到工作区‘工作区 {0} 🎮’（重新定位 5 个窗口，从‘旧 {1} ‘x’’关闭 2 个窗口）。")]
    // Previous workspace with taskbar switch: every data point in one template.
    [InlineData(UiLanguage.English, "Work", 5, "Old", 2, 3, null,
        "Switched to 'Work' (5 repositioned, 2 closed from 'Old', taskbar switched to 3 pins).")]
    [InlineData(UiLanguage.ChineseSimplified, "工作区 {0} 🎮", 5, "旧 {1} ‘x’", 2, 3, null,
        "已切换到工作区‘工作区 {0} 🎮’（重新定位 5 个窗口，从‘旧 {1} ‘x’’关闭 2 个窗口，任务栏已切换为 3 个固定项）。")]
    public void RestoreResult_WithAndWithoutOldWorkspaceAndTaskbar_RendersCompleteMessage(
        UiLanguage language, string name, int restored, string? oldName, int closed, int? pins, string? source,
        string expected)
    {
        var result = OperationDisplay.RestoreResult(L(language), name, restored, oldName, closed, pins, source);

        Assert.Equal(expected, result);

        // No template placeholder may survive rendering; braces that are part of the
        // verbatim user data (name/old name) obviously stay.
        foreach (var placeholder in new[] { "{0}", "{1}", "{2}", "{3}", "{4}", "{5}" })
        {
            if (!name.Contains(placeholder) && !(oldName?.Contains(placeholder) ?? false))
            {
                Assert.DoesNotContain(placeholder, result);
            }
        }
    }

    [Theory]
    [InlineData(UiLanguage.English, "Hotkey", false, "[Hotkey] Restored 'Work' (5 windows repositioned).")]
    [InlineData(UiLanguage.English, "Tray", false, "[Tray] Restored 'Work' (5 windows repositioned).")]
    [InlineData(UiLanguage.ChineseSimplified, "Hotkey", false, "[快捷键] 已恢复工作区‘Work’（重新定位 5 个窗口）。")]
    [InlineData(UiLanguage.ChineseSimplified, "Tray", false, "[托盘] 已恢复工作区‘Work’（重新定位 5 个窗口）。")]
    [InlineData(UiLanguage.English, "Hotkey", true,
        "[Hotkey] Switched to 'Work' (5 repositioned, 2 closed from 'Old', taskbar switched to 3 pins).")]
    [InlineData(UiLanguage.ChineseSimplified, "Tray", true,
        "[托盘] 已切换到工作区‘Work’（重新定位 5 个窗口，从‘Old’关闭 2 个窗口，任务栏已切换为 3 个固定项）。")]
    public void RestoreResult_WithSwitchSource_UsesLocalizedSourceInsideCompleteTemplate(
        UiLanguage language, string source, bool withOldWorkspace, string expected)
    {
        var result = withOldWorkspace
            ? OperationDisplay.RestoreResult(L(language), "Work", 5, "Old", 2, 3, source)
            : OperationDisplay.RestoreResult(L(language), "Work", 5, null, 0, null, source);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(UiLanguage.English, "Hotkey", "Hotkey")]
    [InlineData(UiLanguage.English, "Tray", "Tray")]
    [InlineData(UiLanguage.ChineseSimplified, "Hotkey", "快捷键")]
    [InlineData(UiLanguage.ChineseSimplified, "Tray", "托盘")]
    // Unknown or empty source identifiers are never translated.
    [InlineData(UiLanguage.English, "Manual", "Manual")]
    [InlineData(UiLanguage.ChineseSimplified, "Manual", "Manual")]
    [InlineData(UiLanguage.ChineseSimplified, "", "")]
    public void SwitchSourceText_MapsStableIdentifiersAndKeepsUnknownOnesVerbatim(
        UiLanguage language, string source, string expected)
    {
        Assert.Equal(expected, OperationDisplay.SwitchSourceText(L(language), source));
    }

    [Theory]
    [InlineData(UiLanguage.English, OperationFailureReason.NameEmpty, "Profile name cannot be empty.")]
    [InlineData(UiLanguage.English, OperationFailureReason.ProfileMissing, "Workspace profile does not exist.")]
    [InlineData(UiLanguage.English, OperationFailureReason.SourceFileMissing, "Source file not found.")]
    [InlineData(UiLanguage.English, OperationFailureReason.InvalidProfileFormat, "Invalid workspace profile format.")]
    [InlineData(UiLanguage.English, OperationFailureReason.HotkeyRegistrationFailed, "Hotkey registration failed.")]
    [InlineData(UiLanguage.ChineseSimplified, OperationFailureReason.NameEmpty, "名称为空。")]
    [InlineData(UiLanguage.ChineseSimplified, OperationFailureReason.ProfileMissing, "配置不存在。")]
    [InlineData(UiLanguage.ChineseSimplified, OperationFailureReason.SourceFileMissing, "源文件不存在。")]
    [InlineData(UiLanguage.ChineseSimplified, OperationFailureReason.InvalidProfileFormat, "无效配置格式。")]
    [InlineData(UiLanguage.ChineseSimplified, OperationFailureReason.HotkeyRegistrationFailed, "快捷键注册失败。")]
    public void ErrorDetail_KnownReasons_RenderLocalizedExplanationNotEnglishText(
        UiLanguage language, OperationFailureReason reason, string expected)
    {
        // The English exception text deliberately differs from the expected output:
        // the mapping must key off the stable reason, never off the message text.
        var exception = new OperationFailureException(reason, "some unrelated English detail");

        Assert.Equal(expected, OperationDisplay.ErrorDetail(L(language), exception));
    }

    [Theory]
    [InlineData(UiLanguage.English)]
    [InlineData(UiLanguage.ChineseSimplified)]
    public void ErrorDetail_UnknownException_KeepsOriginalMessageVerbatim(UiLanguage language)
    {
        const string message = "path {x} \"missing\" 路径不存在 🎮";

        var result = OperationDisplay.ErrorDetail(L(language), new InvalidOperationException(message));

        Assert.Equal(message, result);
    }

    [Fact]
    public void ErrorDetail_KnownReason_KeepsInnerExceptionAndMessageForDiagnostics()
    {
        var inner = new FormatException("input {0} was 不合法");
        var exception = new OperationFailureException(
            OperationFailureReason.InvalidProfileFormat, "Invalid workspace profile format.", inner);

        Assert.Same(inner, exception.InnerException);
        Assert.Equal("Invalid workspace profile format.", exception.Message);
        Assert.Equal("无效配置格式。", OperationDisplay.ErrorDetail(L(UiLanguage.ChineseSimplified), exception));
    }

    [Theory]
    [InlineData(UiLanguage.English, "Workspace Profile (*.json)|*.json|All Files (*.*)|*.*")]
    [InlineData(UiLanguage.ChineseSimplified, "工作区配置（*.json）|*.json|所有文件（*.*）|*.*")]
    public void ProfileFilter_LocalizesDescriptionsButKeepsFilterPatternsUnchanged(
        UiLanguage language, string expected)
    {
        var filter = OperationDisplay.ProfileFilter(L(language));

        Assert.Equal(expected, filter);
        Assert.Contains("|*.json|", filter);
        Assert.Contains("|*.*", filter);
    }

    [Theory]
    [InlineData(UiLanguage.English, "Import Workspace Profile")]
    [InlineData(UiLanguage.ChineseSimplified, "导入工作区配置")]
    public void ImportDialogTitle_IsLocalized(UiLanguage language, string expected)
    {
        Assert.Equal(expected, OperationDisplay.ImportDialogTitle(L(language)));
    }

    [Theory]
    [InlineData(UiLanguage.English, "Export Workspace Profile '工作区 {0} 🎮'")]
    [InlineData(UiLanguage.ChineseSimplified, "导出工作区配置‘工作区 {0} 🎮’")]
    public void ExportDialogTitle_IsLocalizedAndKeepsWorkspaceNameVerbatim(
        UiLanguage language, string expected)
    {
        Assert.Equal(expected, OperationDisplay.ExportDialogTitle(L(language), "工作区 {0} 🎮"));
    }
}
