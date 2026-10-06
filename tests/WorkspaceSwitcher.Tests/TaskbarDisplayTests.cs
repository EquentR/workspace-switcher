using System.Linq;
using WorkspaceSwitcher.Core.Localization;
using Xunit;

namespace WorkspaceSwitcher.Tests;

/// <summary>
/// Taskbar localization seam (TASK-05): the facade renders the pin-scope status terms,
/// the sentence-based toggle help, the pin-count templates and the taskbar operation
/// result templates. Workspace and app names containing braces, Chinese characters and
/// emoji pass through format arguments verbatim. No test touches the real taskbar,
/// pinned shortcuts or Explorer.
/// </summary>
public class TaskbarDisplayTests
{
    private const string WorkspaceName = "游戏 {0}🎮";
    private const string AppName = "记事本 {x}🚀";
    private const string ExceptionMessage = "拒绝访问 {x}";

    [Theory]
    [InlineData(UiLanguage.English, true, "📌 Static (All Workspaces)")]
    [InlineData(UiLanguage.English, false, "🗔 Workspace Only")]
    [InlineData(UiLanguage.ChineseSimplified, true, "📌 全局固定（所有工作区）")]
    [InlineData(UiLanguage.ChineseSimplified, false, "🗔 仅此工作区")]
    public void ScopeStatusText_RendersScopeTermForThePinnedFlag(UiLanguage language, bool isStatic, string expected)
    {
        var result = TaskbarDisplay.ScopeStatusText(new Localizer(language), isStatic);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToggleHelpSegments_English_KeepCurrentRunTextsAndHighlightScopeTerms()
    {
        var segments = TaskbarDisplay.CreateToggleHelpSegments(new Localizer(UiLanguage.English));

        Assert.Equal(new[]
        {
            new TaskbarHelpSegment("Toggle ", TaskbarHelpSegmentKind.Plain),
            new TaskbarHelpSegment("📌 Static", TaskbarHelpSegmentKind.StaticTerm),
            new TaskbarHelpSegment(" on any app to keep it pinned across all workspaces (e.g. Browser, File Explorer). ", TaskbarHelpSegmentKind.Plain),
            new TaskbarHelpSegment("🗔 Workspace Only", TaskbarHelpSegmentKind.WorkspaceOnlyTerm),
            new TaskbarHelpSegment(" apps will appear exclusively when this workspace is active.", TaskbarHelpSegmentKind.Plain)
        }, segments);
    }

    [Fact]
    public void ToggleHelpSegments_Chinese_TranslateWholeSentencesWithoutBreakingWordOrder()
    {
        var segments = TaskbarDisplay.CreateToggleHelpSegments(new Localizer(UiLanguage.ChineseSimplified));

        Assert.Equal(new[]
        {
            new TaskbarHelpSegment("将应用设为“", TaskbarHelpSegmentKind.Plain),
            new TaskbarHelpSegment("全局固定", TaskbarHelpSegmentKind.StaticTerm),
            new TaskbarHelpSegment("”，即可在所有工作区保留它（例如浏览器、文件资源管理器）。设为“", TaskbarHelpSegmentKind.Plain),
            new TaskbarHelpSegment("仅此工作区", TaskbarHelpSegmentKind.WorkspaceOnlyTerm),
            new TaskbarHelpSegment("”的应用只会在此工作区启用时出现。", TaskbarHelpSegmentKind.Plain)
        }, segments);
    }

    [Theory]
    [InlineData(UiLanguage.English)]
    [InlineData(UiLanguage.ChineseSimplified)]
    public void ToggleHelpSegments_ReassembleToTheCompleteHelpText(UiLanguage language)
    {
        var localizer = new Localizer(language);

        var segments = TaskbarDisplay.CreateToggleHelpSegments(localizer);

        Assert.Equal(localizer.Get("Taskbar.ToggleHelp"), string.Concat(segments.Select(s => s.Text)));
        Assert.Equal(
            new[]
            {
                TaskbarHelpSegmentKind.Plain,
                TaskbarHelpSegmentKind.StaticTerm,
                TaskbarHelpSegmentKind.Plain,
                TaskbarHelpSegmentKind.WorkspaceOnlyTerm,
                TaskbarHelpSegmentKind.Plain
            },
            segments.Select(s => s.Kind));
    }

    [Theory]
    [InlineData(UiLanguage.English, "ListRow.PinCount", 0, "📌0")]
    [InlineData(UiLanguage.English, "ListRow.PinCount", 1, "📌1")]
    [InlineData(UiLanguage.English, "ListRow.PinCount", 7, "📌7")]
    [InlineData(UiLanguage.ChineseSimplified, "ListRow.PinCount", 0, "📌0")]
    [InlineData(UiLanguage.ChineseSimplified, "ListRow.PinCount", 7, "📌7")]
    [InlineData(UiLanguage.English, "Detail.PinsStat", 0, "0 Taskbar Pins")]
    [InlineData(UiLanguage.English, "Detail.PinsStat", 1, "1 Taskbar Pins")]
    [InlineData(UiLanguage.English, "Detail.PinsStat", 7, "7 Taskbar Pins")]
    [InlineData(UiLanguage.ChineseSimplified, "Detail.PinsStat", 0, "0 个任务栏固定项")]
    [InlineData(UiLanguage.ChineseSimplified, "Detail.PinsStat", 1, "1 个任务栏固定项")]
    [InlineData(UiLanguage.ChineseSimplified, "Detail.PinsStat", 7, "7 个任务栏固定项")]
    public void PinCountTemplates_ZeroOneAndMultiplePins_RenderCountWithoutResidue(
        UiLanguage language, string key, int count, string expected)
    {
        var result = new Localizer(language).Format(key, count);

        Assert.Equal(expected, result);
        Assert.DoesNotContain("{0}", result);
    }

    [Theory]
    [InlineData(UiLanguage.English, 0, "Captured 0 taskbar pin(s) for '游戏 {0}🎮'.")]
    [InlineData(UiLanguage.English, 1, "Captured 1 taskbar pin(s) for '游戏 {0}🎮'.")]
    [InlineData(UiLanguage.English, 7, "Captured 7 taskbar pin(s) for '游戏 {0}🎮'.")]
    [InlineData(UiLanguage.ChineseSimplified, 0, "已为工作区‘游戏 {0}🎮’保存 0 个任务栏固定项。")]
    [InlineData(UiLanguage.ChineseSimplified, 1, "已为工作区‘游戏 {0}🎮’保存 1 个任务栏固定项。")]
    [InlineData(UiLanguage.ChineseSimplified, 7, "已为工作区‘游戏 {0}🎮’保存 7 个任务栏固定项。")]
    public void TaskbarCaptured_ZeroOneAndMultiplePins_RenderCountAndVerbatimWorkspaceName(
        UiLanguage language, int count, string expected)
    {
        var result = new Localizer(language).Format("Status.TaskbarCaptured", count, WorkspaceName);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(UiLanguage.English, true, "Applied taskbar layout for '游戏 {0}🎮' (3 pinned apps).")]
    [InlineData(UiLanguage.English, false, "Failed to apply taskbar layout for '游戏 {0}🎮'.")]
    [InlineData(UiLanguage.ChineseSimplified, true, "已为工作区‘游戏 {0}🎮’应用任务栏布局（3 个固定项）。")]
    [InlineData(UiLanguage.ChineseSimplified, false, "为工作区‘游戏 {0}🎮’应用任务栏布局失败。")]
    public void TaskbarApply_SuccessAndFailure_RenderCompleteResult(UiLanguage language, bool success, string expected)
    {
        var localizer = new Localizer(language);

        var result = success
            ? localizer.Format("Status.TaskbarApplySuccess", WorkspaceName, 3)
            : localizer.Format("Status.TaskbarApplyFailed", WorkspaceName);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(UiLanguage.English, "No taskbar configuration captured for this workspace.")]
    [InlineData(UiLanguage.ChineseSimplified, "此工作区尚未保存任务栏布局。")]
    public void TaskbarNoConfig_ExplainsMissingLayout(UiLanguage language, string expected)
    {
        var result = new Localizer(language).Get("Status.TaskbarNoConfig");

        Assert.Equal(expected, result);
        Assert.NotEqual("Status.TaskbarNoConfig", result);
    }

    [Theory]
    [InlineData(UiLanguage.English, "Synchronized static taskbar pins across all workspaces.")]
    [InlineData(UiLanguage.ChineseSimplified, "已将全局固定项同步到所有已保存的工作区。")]
    public void TaskbarSyncSuccess_UsesStaticPinTerm(UiLanguage language, string expected)
    {
        Assert.Equal(expected, new Localizer(language).Get("Status.TaskbarSyncSuccess"));
    }

    [Theory]
    [InlineData(UiLanguage.English, true, "'记事本 {x}🚀' is now marked Static (preserved across all workspaces).")]
    [InlineData(UiLanguage.English, false, "'记事本 {x}🚀' is now workspace-only for '游戏 {0}🎮'.")]
    [InlineData(UiLanguage.ChineseSimplified, true, "已将‘记事本 {x}🚀’标记为全局固定（在所有工作区保留）。")]
    [InlineData(UiLanguage.ChineseSimplified, false, "已将‘记事本 {x}🚀’标记为仅此工作区（工作区‘游戏 {0}🎮’）。")]
    public void TaskbarScopeChange_GlobalAndWorkspaceOnly_RenderCompleteResult(
        UiLanguage language, bool isStatic, string expected)
    {
        var localizer = new Localizer(language);

        var result = isStatic
            ? localizer.Format("Status.TaskbarMarkedStatic", AppName)
            : localizer.Format("Status.TaskbarMarkedWorkspaceOnly", AppName, WorkspaceName);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(UiLanguage.English, "Status.TaskbarCaptureFailed", "Error capturing taskbar pins: 拒绝访问 {x}")]
    [InlineData(UiLanguage.English, "Status.TaskbarApplyError", "Error applying taskbar: 拒绝访问 {x}")]
    [InlineData(UiLanguage.English, "Status.TaskbarSyncFailed", "Error syncing static pins: 拒绝访问 {x}")]
    [InlineData(UiLanguage.English, "Status.TaskbarScopeError", "Error updating static pin: 拒绝访问 {x}")]
    [InlineData(UiLanguage.ChineseSimplified, "Status.TaskbarCaptureFailed", "保存任务栏固定项失败：拒绝访问 {x}")]
    [InlineData(UiLanguage.ChineseSimplified, "Status.TaskbarApplyError", "应用任务栏布局失败：拒绝访问 {x}")]
    [InlineData(UiLanguage.ChineseSimplified, "Status.TaskbarSyncFailed", "同步全局固定项失败：拒绝访问 {x}")]
    [InlineData(UiLanguage.ChineseSimplified, "Status.TaskbarScopeError", "更新固定项失败：拒绝访问 {x}")]
    public void TaskbarErrorTemplates_KeepOriginalExceptionMessageVerbatim(
        UiLanguage language, string key, string expected)
    {
        var result = new Localizer(language).Format(key, ExceptionMessage);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(UiLanguage.English, "Taskbar.Title", "Taskbar Pinned Apps")]
    [InlineData(UiLanguage.ChineseSimplified, "Taskbar.Title", "任务栏固定应用")]
    [InlineData(UiLanguage.English, "Taskbar.EnabledLabel", "Enabled for this workspace")]
    [InlineData(UiLanguage.ChineseSimplified, "Taskbar.EnabledLabel", "为此工作区启用")]
    [InlineData(UiLanguage.English, "Taskbar.SnapshotButton", "Snapshot Current Taskbar")]
    [InlineData(UiLanguage.ChineseSimplified, "Taskbar.SnapshotButton", "保存当前任务栏布局")]
    [InlineData(UiLanguage.English, "Taskbar.ApplyButton", "Apply Taskbar Now")]
    [InlineData(UiLanguage.ChineseSimplified, "Taskbar.ApplyButton", "立即应用任务栏布局")]
    [InlineData(UiLanguage.English, "Taskbar.SyncButton", "Sync Static Pins")]
    [InlineData(UiLanguage.ChineseSimplified, "Taskbar.SyncButton", "同步全局固定项")]
    [InlineData(UiLanguage.English, "Taskbar.SyncTooltip", "Sync all static pins across all saved workspaces")]
    [InlineData(UiLanguage.ChineseSimplified, "Taskbar.SyncTooltip", "将全局固定项同步到所有已保存的工作区")]
    [InlineData(UiLanguage.English, "Taskbar.RemoveTooltip", "Remove from this workspace layout")]
    [InlineData(UiLanguage.ChineseSimplified, "Taskbar.RemoveTooltip", "从此工作区布局移除")]
    [InlineData(UiLanguage.English, "Taskbar.ShellShortcutFallback", "Windows Shell Shortcut")]
    [InlineData(UiLanguage.ChineseSimplified, "Taskbar.ShellShortcutFallback", "Windows Shell 快捷方式")]
    [InlineData(UiLanguage.English, "Taskbar.EmptyTitle", "No Taskbar Layout Captured For This Workspace")]
    [InlineData(UiLanguage.ChineseSimplified, "Taskbar.EmptyTitle", "此工作区尚未保存任务栏布局")]
    public void TaskbarChromeTexts_ResolveToLocalizedWords(UiLanguage language, string key, string expected)
    {
        var text = new Localizer(language).Get(key);

        Assert.Equal(expected, text);
        Assert.NotEqual(key, text);
    }

    [Theory]
    [InlineData(UiLanguage.English, "Click 'Snapshot Current Taskbar' to save your currently pinned taskbar apps (games, IDEs, tools) to this workspace.")]
    [InlineData(UiLanguage.ChineseSimplified, "点击“保存当前任务栏布局”，将当前固定的应用（游戏、开发工具等）保存到此工作区。")]
    public void TaskbarEmptyDescription_ReferencesTheSharedSnapshotButtonName(UiLanguage language, string expected)
    {
        var localizer = new Localizer(language);

        Assert.Equal(expected, localizer.Get("Taskbar.EmptyDescription"));
    }
}
