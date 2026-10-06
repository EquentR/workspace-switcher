using System;

namespace WorkspaceSwitcher.Core.Localization;

/// <summary>
/// Language-dependent display text for workspace operations (TASK-06): restore/switch
/// result templates, switch-source display names, known-failure explanations and file
/// dialog strings. Result text comes from complete localized templates only; the
/// persisted source identifiers (Hotkey/Tray), user file names, paths and the file
/// filter patterns (*.json, *.*) never change with the display language.
/// </summary>
public static class OperationDisplay
{
    /// <summary>
    /// Display name of a switch-source identifier ("Hotkey" → 快捷键, "Tray" → 托盘).
    /// Identifiers themselves are never translated: unknown or empty values pass
    /// through verbatim.
    /// </summary>
    public static string SwitchSourceText(Localizer localizer, string? source)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        return source switch
        {
            "Hotkey" => localizer.Get("SwitchSource.Hotkey"),
            "Tray" => localizer.Get("SwitchSource.Tray"),
            _ => source ?? string.Empty
        };
    }

    /// <summary>
    /// Complete localized result message for a restore/switch operation. When
    /// <paramref name="previousWorkspaceName"/> is present the message reports the
    /// switch (with the old workspace name and the closed-window count); otherwise it
    /// reports a plain restore. The optional taskbar result and switch source each
    /// select a full template variant — the message is never assembled from fragments.
    /// </summary>
    /// <param name="restoredCount">Windows repositioned to the target layout.</param>
    /// <param name="previousWorkspaceName">Old workspace name, or null when there was none.</param>
    /// <param name="closedCount">Windows closed from the old workspace (only rendered alongside it).</param>
    /// <param name="taskbarPinCount">Taskbar pin count when taskbar pins were switched, otherwise null.</param>
    /// <param name="switchSource">Stable source identifier ("Hotkey"/"Tray"), or null for a manual switch.</param>
    public static string RestoreResult(
        Localizer localizer,
        string workspaceName,
        int restoredCount,
        string? previousWorkspaceName,
        int closedCount,
        int? taskbarPinCount,
        string? switchSource)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentNullException.ThrowIfNull(workspaceName);

        bool fromOldWorkspace = !string.IsNullOrEmpty(previousWorkspaceName);
        string key = "Status."
            + (fromOldWorkspace ? "SwitchResult" : "RestoreResult")
            + (taskbarPinCount.HasValue ? "Taskbar" : string.Empty)
            + (string.IsNullOrEmpty(switchSource) ? string.Empty : "FromSource");

        return localizer.Format(
            key,
            workspaceName,
            restoredCount,
            closedCount,
            previousWorkspaceName ?? string.Empty,
            taskbarPinCount ?? 0,
            SwitchSourceText(localizer, switchSource));
    }

    /// <summary>
    /// Localized explanation of a failed operation. Known business failures render
    /// as their localized reason text (mapped via the stable reason identifier, never
    /// by matching the English exception text); any other exception keeps its
    /// original message verbatim.
    /// </summary>
    public static string ErrorDetail(Localizer localizer, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentNullException.ThrowIfNull(exception);

        return exception is OperationFailureException { Reason: var reason } && reason != OperationFailureReason.Unknown
            ? localizer.Get(ReasonKey(reason))
            : exception.Message;
    }

    /// <summary>Import file dialog title.</summary>
    public static string ImportDialogTitle(Localizer localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        return localizer.Get("FileDialog.ImportTitle");
    }

    /// <summary>Export file dialog title; the workspace name stays verbatim.</summary>
    public static string ExportDialogTitle(Localizer localizer, string workspaceName)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        return localizer.Format("FileDialog.ExportTitle", workspaceName);
    }

    /// <summary>
    /// File-type filter for workspace profile dialogs. Only the descriptions are
    /// localized; the filter patterns (*.json, *.*) are literal and unchanged.
    /// </summary>
    public static string ProfileFilter(Localizer localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        return localizer.Get("FileDialog.ProfileFilter") + "|*.json|"
            + localizer.Get("FileDialog.AllFilesFilter") + "|*.*";
    }

    private static string ReasonKey(OperationFailureReason reason) => reason switch
    {
        OperationFailureReason.NameEmpty => "Error.NameEmpty",
        OperationFailureReason.ProfileMissing => "Error.ProfileMissing",
        OperationFailureReason.SourceFileMissing => "Error.SourceFileMissing",
        OperationFailureReason.InvalidProfileFormat => "Error.InvalidProfileFormat",
        OperationFailureReason.HotkeyRegistrationFailed => "Error.HotkeyRegistrationFailed",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown reasons take the exception-message path.")
    };
}
