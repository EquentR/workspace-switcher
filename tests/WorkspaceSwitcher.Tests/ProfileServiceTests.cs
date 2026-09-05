using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WorkspaceSwitcher.Core.Models;
using WorkspaceSwitcher.Core.Services;
using Xunit;

namespace WorkspaceSwitcher.Tests;

public class ProfileServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ProfileService _profileService;

    public ProfileServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "WorkspaceSwitcher_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
        _profileService = new ProfileService(_testDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public void SaveAndLoadProfile_PreservesAllProperties()
    {
        var profile = new WorkspaceProfile("Development")
        {
            Description = "Coding & Debugging environment",
            IconGlyph = "💻",
            HotkeyModifier = "Ctrl + Alt",
            HotkeyKey = "1",
            DisplayCount = 2,
            Windows =
            {
                new WindowInfo
                {
                    ProcessName = "Code",
                    ExecutablePath = @"C:\Tools\Code.exe",
                    WindowTitle = "WorkspaceSwitcher - VS Code",
                    ClassName = "Chrome_WidgetWin_1",
                    Bounds = new WindowRect(0, 0, 1920, 1080),
                    Placement = new WindowPlacementInfo
                    {
                        State = WindowState.Normal,
                        NormalPosition = new WindowRect(0, 0, 1920, 1080)
                    }
                }
            },
            Taskbar = new TaskbarConfiguration
            {
                Favorites = new byte[] { 1, 2, 3, 4 },
                PinnedItems =
                {
                    new TaskbarPinnedItem
                    {
                        DisplayName = "VS Code",
                        ShortcutFileName = "Visual Studio Code.lnk",
                        TargetPath = @"C:\Tools\Code.exe",
                        IsStatic = true
                    }
                }
            }
        };

        _profileService.SaveProfile(profile);

        Assert.True(_profileService.ProfileExists("Development"));
        var loaded = _profileService.LoadProfile("Development");

        Assert.NotNull(loaded);
        Assert.Equal("Development", loaded.Name);
        Assert.Equal("Coding & Debugging environment", loaded.Description);
        Assert.Equal("💻", loaded.IconGlyph);
        Assert.Equal("Ctrl + Alt", loaded.HotkeyModifier);
        Assert.Equal("1", loaded.HotkeyKey);
        Assert.Equal(2, loaded.DisplayCount);
        Assert.Single(loaded.Windows);
        Assert.Equal("Code", loaded.Windows[0].ProcessName);
        Assert.Equal(WindowState.Normal, loaded.Windows[0].Placement.State);
        Assert.NotNull(loaded.Taskbar);
        Assert.Single(loaded.Taskbar.PinnedItems);
        Assert.True(loaded.Taskbar.PinnedItems[0].IsStatic);
    }

    [Fact]
    public async Task SaveProfileAsync_PersistsSuccessfully()
    {
        var profile = new WorkspaceProfile("AsyncProfile")
        {
            Description = "Testing async save",
            IconGlyph = "🚀"
        };

        await _profileService.SaveProfileAsync(profile);

        var loaded = await _profileService.LoadProfileAsync("AsyncProfile");
        Assert.NotNull(loaded);
        Assert.Equal("AsyncProfile", loaded.Name);
        Assert.Equal("🚀", loaded.IconGlyph);
    }

    [Fact]
    public void DeleteProfile_RemovesFileAndReturnsTrue()
    {
        var profile = new WorkspaceProfile("To Delete");
        _profileService.SaveProfile(profile);
        Assert.True(_profileService.ProfileExists("To Delete"));

        bool deleted = _profileService.DeleteProfile("To Delete");
        Assert.True(deleted);
        Assert.False(_profileService.ProfileExists("To Delete"));
        Assert.Null(_profileService.LoadProfile("To Delete"));
    }

    [Fact]
    public void DeleteProfile_NonExistent_ReturnsFalse()
    {
        bool deleted = _profileService.DeleteProfile("Does Not Exist");
        Assert.False(deleted);
    }

    [Fact]
    public void GetAllProfiles_ReturnsAllSavedProfiles()
    {
        _profileService.SaveProfile(new WorkspaceProfile("P1") { Description = "Profile 1" });
        _profileService.SaveProfile(new WorkspaceProfile("P2") { Description = "Profile 2" });
        _profileService.SaveProfile(new WorkspaceProfile("P3") { Description = "Profile 3" });

        var names = _profileService.GetProfileNames();
        Assert.Equal(3, names.Count);
        Assert.Contains("P1", names);
        Assert.Contains("P2", names);
        Assert.Contains("P3", names);

        var all = _profileService.GetAllProfiles();
        Assert.Equal(3, all.Count);
        Assert.Contains(all, p => p.Name == "P1");
    }

    [Fact]
    public void ExportAndImportProfile_TransfersProfileCorrectly()
    {
        var original = new WorkspaceProfile("OriginalProfile")
        {
            Description = "To be exported",
            IconGlyph = "📦"
        };
        _profileService.SaveProfile(original);

        string exportFilePath = Path.Combine(_testDirectory, "Exported", "ProfileBackup.json");
        _profileService.ExportProfile("OriginalProfile", exportFilePath);
        Assert.True(File.Exists(exportFilePath));

        // Delete from profile service
        _profileService.DeleteProfile("OriginalProfile");
        Assert.False(_profileService.ProfileExists("OriginalProfile"));

        // Import
        var imported = _profileService.ImportProfile(exportFilePath);
        Assert.NotNull(imported);
        Assert.Equal("OriginalProfile", imported.Name);
        Assert.Equal("📦", imported.IconGlyph);
        Assert.True(_profileService.ProfileExists("OriginalProfile"));
    }

    [Fact]
    public void AeroSnapBounds_AreNormalizedOnLoad()
    {
        var profile = new WorkspaceProfile("SnappedProfile")
        {
            Windows =
            {
                new WindowInfo
                {
                    ProcessName = "Notepad",
                    Bounds = new WindowRect(0, 0, 960, 1040),
                    Placement = new WindowPlacementInfo
                    {
                        State = WindowState.Normal,
                        NormalPosition = new WindowRect(100, 100, 500, 500) // Desynced normal position
                    }
                }
            }
        };

        _profileService.SaveProfile(profile);
        var loaded = _profileService.LoadProfile("SnappedProfile");

        Assert.NotNull(loaded);
        Assert.Single(loaded.Windows);
        var win = loaded.Windows[0];

        // Should have normalized NormalPosition to match Bounds when Normal state
        Assert.Equal(0, win.Placement.NormalPosition.Left);
        Assert.Equal(0, win.Placement.NormalPosition.Top);
        Assert.Equal(960, win.Placement.NormalPosition.Right);
        Assert.Equal(1040, win.Placement.NormalPosition.Bottom);
    }
}
