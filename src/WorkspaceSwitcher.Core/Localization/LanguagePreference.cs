namespace WorkspaceSwitcher.Core.Localization;

/// <summary>
/// Persisted language preference values (the settings "language" field).
/// </summary>
public static class LanguagePreference
{
    public const string System = "system";
    public const string English = "en";
    public const string ChineseSimplified = "zh-CN";

    /// <summary>
    /// Maps missing or unrecognized preference values to <see cref="System"/>.
    /// </summary>
    public static string Normalize(string? value) => value switch
    {
        English => English,
        ChineseSimplified => ChineseSimplified,
        _ => System
    };
}
