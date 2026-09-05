using System;
using System.IO;
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
}
