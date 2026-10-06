using System;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;

namespace WorkspaceSwitcher.Core.Localization;

/// <summary>
/// UI-agnostic localization facade. Resolves the effective language from an explicit
/// preference plus the system UI culture, looks up resource strings with per-key
/// English fallback and formats parameterized messages.
/// </summary>
public sealed class Localizer
{
    private static readonly ResourceManager Resources =
        new("WorkspaceSwitcher.Core.Localization.Strings", typeof(Localizer).Assembly);

    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo ChineseCulture = CultureInfo.GetCultureInfo("zh-CN");

    private static Localizer? _current;

    private readonly CultureInfo _culture;

    public Localizer(UiLanguage language)
    {
        Language = language;
        _culture = language == UiLanguage.ChineseSimplified ? ChineseCulture : EnglishCulture;
    }

    public UiLanguage Language { get; }

    /// <summary>
    /// Process-wide facade. Initialized once at startup before any window, dialog or
    /// tray object is created; the effective language is fixed for the process.
    /// </summary>
    public static Localizer Current =>
        _current ?? throw new InvalidOperationException(
            "Localizer.Initialize must run before any UI object is created.");

    /// <summary>
    /// Fixes the process-wide effective language. Must run exactly once per process,
    /// before the first window, dialog or tray object is constructed.
    /// </summary>
    public static Localizer Initialize(string? languagePreference, CultureInfo systemUiCulture)
    {
        if (_current != null)
        {
            throw new InvalidOperationException(
                "Localizer is already initialized; the effective language is fixed for the process.");
        }

        _current = new Localizer(ResolveLanguage(languagePreference, systemUiCulture));
        return _current;
    }

    /// <summary>
    /// Resolves the effective language from an explicit preference and the system UI
    /// culture. An explicit choice always wins; missing or invalid preferences behave
    /// as <see cref="LanguagePreference.System"/>. Automatic mode selects Simplified
    /// Chinese only for Simplified variants (zh-CN, zh-SG, zh-Hans and their regions);
    /// Traditional Chinese and every other culture — including a bare "zh" — get English.
    /// </summary>
    public static UiLanguage ResolveLanguage(string? languagePreference, CultureInfo systemUiCulture)
    {
        ArgumentNullException.ThrowIfNull(systemUiCulture);

        return LanguagePreference.Normalize(languagePreference) switch
        {
            LanguagePreference.English => UiLanguage.English,
            LanguagePreference.ChineseSimplified => UiLanguage.ChineseSimplified,
            _ => IsSimplifiedChinese(systemUiCulture) ? UiLanguage.ChineseSimplified : UiLanguage.English
        };
    }

    /// <summary>
    /// Returns the resource text for <paramref name="key"/> in the effective language,
    /// falling back per key to the neutral English text. Never returns a resource key
    /// or an empty string; a key missing everywhere is a programming error and throws.
    /// </summary>
    public string Get(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        var text = Resources.GetString(key, _culture);
        if (!string.IsNullOrEmpty(text))
        {
            return text;
        }

        text = Resources.GetString(key, EnglishCulture);
        if (!string.IsNullOrEmpty(text))
        {
            return text;
        }

        throw new KeyNotFoundException(
            $"Localization key '{key}' is missing from the neutral English resources.");
    }

    /// <summary>
    /// Formats the resource template for <paramref name="key"/> with the given arguments
    /// in the effective language's culture.
    /// </summary>
    public string Format(string key, params object?[] args) => string.Format(_culture, Get(key), args);

    /// <summary>
    /// Formats a capture timestamp for display relative to an explicit reference time.
    /// Both arguments must already be local times; the day-boundary judgment (today /
    /// yesterday) compares local calendar days and never reads the wall clock, so the
    /// rendering is deterministic for a given pair of times.
    /// </summary>
    public string FormatRelativeTime(DateTime localTimestamp, DateTime referenceLocalTime)
    {
        if (localTimestamp.Date == referenceLocalTime.Date)
        {
            return Format("Time.Today", localTimestamp);
        }

        if (localTimestamp.Date == referenceLocalTime.Date.AddDays(-1))
        {
            return Format("Time.Yesterday", localTimestamp);
        }

        var elapsedDays = (referenceLocalTime - localTimestamp).TotalDays;
        if (elapsedDays < 7)
        {
            return Format("Time.DaysAgo", (int)elapsedDays, localTimestamp);
        }

        return localTimestamp.ToString(Get("Time.AbsoluteFormat"), _culture);
    }

    private static bool IsSimplifiedChinese(CultureInfo culture)
    {
        for (var current = culture; !string.IsNullOrEmpty(current.Name); current = current.Parent)
        {
            var name = current.Name;
            if (name.StartsWith("zh-Hans", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("zh-SG", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
