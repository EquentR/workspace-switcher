using System;
using System.IO;
using WorkspaceSwitcher.Core.Hotkeys;
using WorkspaceSwitcher.Core.Models;
using WorkspaceSwitcher.Core.Services;
using Xunit;

namespace WorkspaceSwitcher.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _testSettingsFile;
    private readonly SettingsService _settingsService;

    public SettingsServiceTests()
    {
        _testSettingsFile = Path.Combine(Path.GetTempPath(), $"SettingsTests_{Guid.NewGuid():N}.json");
        _settingsService = new SettingsService(_testSettingsFile);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_testSettingsFile))
            {
                File.Delete(_testSettingsFile);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsDefaultsAndCreatesFile()
    {
        var settings = _settingsService.Load();

        Assert.NotNull(settings);
        Assert.True(File.Exists(_testSettingsFile));
        Assert.False(settings.AutoLaunchMissingApps);
        Assert.True(settings.MinimizeToTrayOnClose);
        Assert.True(settings.SwitchTaskbarPins);
        Assert.True(settings.CloseAppsOnSwitch);
    }

    [Fact]
    public void SaveAndLoad_PersistsCustomPreferences()
    {
        var custom = new AppSettings
        {
            AutoLaunchMissingApps = false,
            MinimizeToTrayOnClose = false,
            CloseAppsOnSwitch = true,
            SwitchTaskbarPins = false,
            LastActiveProfileName = "CustomSetup",
            StaticPinnedApps = { "chrome.exe", "code.exe" }
        };

        _settingsService.Save(custom);
        var loaded = _settingsService.Load();

        Assert.NotNull(loaded);
        Assert.False(loaded.AutoLaunchMissingApps);
        Assert.False(loaded.MinimizeToTrayOnClose);
        Assert.True(loaded.CloseAppsOnSwitch);
        Assert.False(loaded.SwitchTaskbarPins);
        Assert.Equal("CustomSetup", loaded.LastActiveProfileName);
        Assert.Equal(2, loaded.StaticPinnedApps.Count);
        Assert.Contains("chrome.exe", loaded.StaticPinnedApps);
        Assert.Contains("code.exe", loaded.StaticPinnedApps);
    }

    [Fact]
    public void Load_WhenJsonLacksLanguageField_ReturnsSystemDefault()
    {
        // Settings written before the language field existed.
        File.WriteAllText(_testSettingsFile, """
            {
              "autoLaunchMissingApps": true,
              "minimizeToTrayOnClose": true,
              "closeAppsOnSwitch": true,
              "lastActiveProfileName": "Legacy",
              "switchTaskbarPins": true,
              "staticPinnedApps": [ "chrome.exe" ],
              "hotkeys": []
            }
            """);

        var settings = _settingsService.Load();

        Assert.NotNull(settings);
        Assert.Equal("system", settings.Language);
        Assert.True(settings.AutoLaunchMissingApps);
        Assert.Equal("Legacy", settings.LastActiveProfileName);
        Assert.Contains("chrome.exe", settings.StaticPinnedApps);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsLanguagePreference()
    {
        _settingsService.Save(new AppSettings { Language = "zh-CN" });
        Assert.Equal("zh-CN", _settingsService.Load().Language);

        _settingsService.Save(new AppSettings { Language = "en" });
        Assert.Equal("en", _settingsService.Load().Language);
    }

    [Fact]
    public void Save_WhenOtherOptionsChange_PreservesLanguageAndExistingFields()
    {
        var custom = new AppSettings
        {
            Language = "zh-CN",
            AutoLaunchMissingApps = false,
            MinimizeToTrayOnClose = false,
            CloseAppsOnSwitch = true,
            SwitchTaskbarPins = false,
            LastActiveProfileName = "CustomSetup",
            StaticPinnedApps = { "chrome.exe", "code.exe" },
            Hotkeys =
            {
                new HotKeyBinding
                {
                    Id = 1,
                    TargetProfileName = "CustomSetup",
                    Modifiers = KeyModifiers.Control | KeyModifiers.Alt,
                    VirtualKey = 49,
                    Action = HotKeyAction.RestoreProfile,
                    KeyDisplayString = "Ctrl+Alt+1"
                }
            }
        };
        _settingsService.Save(custom);

        // Same load-modify-save shape as MainViewModel.SaveCurrentSettings:
        // only other options are touched.
        var settings = _settingsService.Load();
        settings.AutoLaunchMissingApps = true;
        settings.LastActiveProfileName = "OtherSetup";
        _settingsService.Save(settings);

        var loaded = _settingsService.Load();
        Assert.Equal("zh-CN", loaded.Language);
        Assert.True(loaded.AutoLaunchMissingApps);
        Assert.Equal("OtherSetup", loaded.LastActiveProfileName);
        Assert.False(loaded.MinimizeToTrayOnClose);
        Assert.False(loaded.SwitchTaskbarPins);
        Assert.Equal(2, loaded.StaticPinnedApps.Count);
        Assert.Contains("chrome.exe", loaded.StaticPinnedApps);
        Assert.Contains("code.exe", loaded.StaticPinnedApps);
        var hotkey = Assert.Single(loaded.Hotkeys);
        Assert.Equal("CustomSetup", hotkey.TargetProfileName);
        Assert.Equal(KeyModifiers.Control | KeyModifiers.Alt, hotkey.Modifiers);
        Assert.Equal(49u, hotkey.VirtualKey);
    }
}
