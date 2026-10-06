using System;
using System.Collections.Generic;

namespace WorkspaceSwitcher.Core.Localization;

/// <summary>
/// Emphasis role of a toggle-help span. The scope terms are highlighted wherever they
/// appear inside the localized sentence; the sentence text itself is never reassembled
/// from fragments, so word order stays language-correct.
/// </summary>
public enum TaskbarHelpSegmentKind
{
    Plain,
    StaticTerm,
    WorkspaceOnlyTerm
}

/// <summary>One contiguous span of the localized taskbar toggle help text.</summary>
public sealed record TaskbarHelpSegment(string Text, TaskbarHelpSegmentKind Kind);

/// <summary>
/// Language-dependent display text for the taskbar area (TASK-05). Pin-scope terms
/// (全局固定 / 仅此工作区) and operation result templates come from complete localized
/// strings; the persisted <c>IsStatic</c> flag, shortcut file names, target paths and
/// real application names never change with the display language.
/// </summary>
public static class TaskbarDisplay
{
    /// <summary>
    /// Localized pin-scope pill for a pinned item: the "global" scope kept in every
    /// workspace or the "workspace only" scope tied to one workspace.
    /// </summary>
    public static string ScopeStatusText(Localizer localizer, bool isStatic)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        return isStatic
            ? localizer.Get("Taskbar.ScopeStatic")
            : localizer.Get("Taskbar.ScopeWorkspaceOnly");
    }

    /// <summary>
    /// Splits the complete localized toggle help text at the localized scope terms so
    /// the UI can emphasize those terms. The help is stored as whole sentences and the
    /// split runs reassemble to it exactly; a term missing from its sentence simply
    /// stays unemphasized instead of dropping text.
    /// </summary>
    public static IReadOnlyList<TaskbarHelpSegment> CreateToggleHelpSegments(Localizer localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        var text = localizer.Get("Taskbar.ToggleHelp");
        var needles = new (string Text, TaskbarHelpSegmentKind Kind)[]
        {
            (localizer.Get("Taskbar.HelpTermStatic"), TaskbarHelpSegmentKind.StaticTerm),
            (localizer.Get("Taskbar.HelpTermWorkspaceOnly"), TaskbarHelpSegmentKind.WorkspaceOnlyTerm)
        };

        var segments = new List<TaskbarHelpSegment>();
        int plainStart = 0;
        int position = 0;

        while (position < text.Length)
        {
            (string Text, TaskbarHelpSegmentKind Kind)? match = null;
            foreach (var needle in needles)
            {
                if (needle.Text.Length > 0 &&
                    position + needle.Text.Length <= text.Length &&
                    string.CompareOrdinal(text, position, needle.Text, 0, needle.Text.Length) == 0)
                {
                    match = needle;
                    break;
                }
            }

            if (match is null)
            {
                position++;
                continue;
            }

            if (position > plainStart)
            {
                segments.Add(new TaskbarHelpSegment(text[plainStart..position], TaskbarHelpSegmentKind.Plain));
            }
            segments.Add(new TaskbarHelpSegment(match.Value.Text, match.Value.Kind));
            position += match.Value.Text.Length;
            plainStart = position;
        }

        if (plainStart < text.Length)
        {
            segments.Add(new TaskbarHelpSegment(text[plainStart..], TaskbarHelpSegmentKind.Plain));
        }

        return segments;
    }
}
