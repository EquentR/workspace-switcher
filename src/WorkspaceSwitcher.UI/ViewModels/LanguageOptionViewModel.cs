namespace WorkspaceSwitcher.UI.ViewModels;

/// <summary>
/// A language selector option: the stable persisted value plus its localized display name.
/// </summary>
public sealed record LanguageOptionViewModel(string Value, string Display);
