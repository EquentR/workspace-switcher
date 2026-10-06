using System.Collections.Generic;
using WorkspaceSwitcher.Core.Hotkeys;
using WorkspaceSwitcher.Core.Localization;

namespace WorkspaceSwitcher.Core.Models;

public class AppSettings
{
    public bool AutoLaunchMissingApps { get; set; } = false;
    public bool MinimizeToTrayOnClose { get; set; } = true;
    public bool CloseAppsOnSwitch { get; set; } = true;
    public string? LastActiveProfileName { get; set; }
    public bool StartWithWindows { get; set; } = false;
    public string? CustomProfilesDirectory { get; set; }
    public bool SwitchTaskbarPins { get; set; } = true;
    public List<string> StaticPinnedApps { get; set; } = new();
    public List<HotKeyBinding> Hotkeys { get; set; } = new();

    /// <summary>
    /// Language preference: <see cref="LanguagePreference.System"/>,
    /// <see cref="LanguagePreference.English"/> or
    /// <see cref="LanguagePreference.ChineseSimplified"/>. Missing or invalid values
    /// behave as <see cref="LanguagePreference.System"/>.
    /// </summary>
    public string Language { get; set; } = LanguagePreference.System;
}
