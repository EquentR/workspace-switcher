using System;
using System.Collections.Generic;
using WorkspaceSwitcher.Core.Localization;

namespace WorkspaceSwitcher.Core.Hotkeys;

/// <summary>
/// A hotkey selector option: <see cref="Value"/> is the stable internal token persisted
/// to configuration and parsed by <see cref="HotkeyHelper"/>, while <see cref="Display"/>
/// is the language-dependent label rendered in the UI. Localized display text must never
/// be written back to configuration or passed to the parser.
/// </summary>
public sealed record HotkeyOption(string Value, string Display);

/// <summary>
/// Language-dependent display text for hotkey values. Every method keeps the stable
/// internal tokens ("Ctrl + Alt", "Auto (1-5)", "None (Disabled)", "None", key tokens)
/// separate from the labels shown in the UI; key tokens keep their technical names.
/// </summary>
public static class HotkeyDisplay
{
    /// <summary>Selector options for the modifier combo box.</summary>
    public static IReadOnlyList<HotkeyOption> CreateModifierOptions(Localizer localizer) =>
        Build(HotkeyHelper.AvailableModifiers, localizer);

    /// <summary>Selector options for the key combo box.</summary>
    public static IReadOnlyList<HotkeyOption> CreateKeyOptions(Localizer localizer) =>
        Build(HotkeyHelper.AvailableKeys, localizer);

    /// <summary>
    /// Selects the option matching a profile's stored internal value when opening the
    /// editor. Unknown legacy values fall back to the first option, matching the
    /// pre-existing dialog behavior.
    /// </summary>
    public static HotkeyOption Resolve(IReadOnlyList<HotkeyOption> options, string? internalValue)
    {
        ArgumentNullException.ThrowIfNull(options);

        foreach (var option in options)
        {
            if (string.Equals(option.Value, internalValue, StringComparison.Ordinal))
            {
                return option;
            }
        }

        return options[0];
    }

    /// <summary>Localized display label for a single internal modifier/key token.</summary>
    public static string DisplayText(Localizer localizer, string internalValue)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        return internalValue switch
        {
            "None" => localizer.Get("Hotkey.ModifierNone"),
            "Auto (1-5)" => localizer.Get("Hotkey.Auto"),
            "None (Disabled)" => localizer.Get("Hotkey.Disabled"),
            _ => internalValue
        };
    }

    /// <summary>
    /// Composed display text for a hotkey combination (e.g. "Ctrl + Alt + 1"); the
    /// no-hotkey case renders the localized "No Hotkey" / "未设置快捷键" text.
    /// </summary>
    public static string Format(Localizer localizer, string? modifier, string? key, int index = 0)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        return HotkeyHelper.ComposeDisplayText(modifier, key, index) ?? localizer.Get("Hotkey.NotSet");
    }

    private static IReadOnlyList<HotkeyOption> Build(IReadOnlyList<string> values, Localizer localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        var options = new HotkeyOption[values.Count];
        for (int i = 0; i < values.Count; i++)
        {
            options[i] = new HotkeyOption(values[i], DisplayText(localizer, values[i]));
        }

        return options;
    }
}
