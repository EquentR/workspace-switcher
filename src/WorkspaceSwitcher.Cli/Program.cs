using System;
using System.IO;
using System.Linq;
using System.Threading;
using WorkspaceSwitcher.Core;
using WorkspaceSwitcher.Core.Hotkeys;
using WorkspaceSwitcher.Core.Models;
using WorkspaceSwitcher.Core.Services;

var windowManager = new WindowManager();
var profileService = new ProfileService();
var settingsService = new SettingsService();
var taskbarService = new TaskbarService();

if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
{
    PrintBanner();
    PrintHelp();
    return;
}

string command = args[0].ToLowerInvariant();

switch (command)
{
    case "list":
        ListProfiles(profileService);
        break;

    case "apply":
        if (args.Length < 2)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Missing profile name. Usage: workspaceswitcher apply <profileName> [--launch] [--close-others]");
            Console.ResetColor();
            return;
        }
        ApplyProfile(args[1], args, profileService, windowManager, settingsService, taskbarService);
        break;

    case "snapshot":
        if (args.Length < 2)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Missing profile name. Usage: workspaceswitcher snapshot <profileName> [--desc <desc>] [--icon <icon>] [--no-taskbar]");
            Console.ResetColor();
            return;
        }
        SnapshotProfile(args[1], args, profileService, windowManager, settingsService);
        break;

    case "export":
        if (args.Length < 3)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Usage: workspaceswitcher export <profileName> <destinationFilePath>");
            Console.ResetColor();
            return;
        }
        ExportProfile(args[1], args[2], profileService);
        break;

    case "import":
        if (args.Length < 2)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Usage: workspaceswitcher import <sourceFilePath>");
            Console.ResetColor();
            return;
        }
        ImportProfile(args[1], profileService);
        break;

    case "delete":
        if (args.Length < 2)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Missing profile name. Usage: workspaceswitcher delete <profileName>");
            Console.ResetColor();
            return;
        }
        DeleteProfile(args[1], profileService);
        break;

    case "monitor":
    case "daemon":
    case "run":
        RunMonitor(profileService, windowManager, settingsService);
        break;

    case "apply-taskbar":
        {
            string profileName = args.Length > 1 ? args[1] : "Coding";
            var prof = profileService.LoadProfile(profileName);
            if (prof?.Taskbar == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[ERROR] Profile has no taskbar configuration!");
                Console.ResetColor();
                return;
            }
            bool success = taskbarService.ApplyTaskbar(prof.Taskbar);
            Console.WriteLine($"[RESULT] ApplyTaskbar returned: {success}");
            break;
        }

    case "snapshot-taskbar":
        {
            string profileName = args.Length > 1 ? args[1] : "Coding";
            var prof = profileService.LoadProfile(profileName);
            if (prof != null)
            {
                prof.Taskbar = taskbarService.CaptureCurrentTaskbar();
                profileService.SaveProfile(prof);
                Console.WriteLine($"[SNAPSHOT] Saved {prof.Taskbar.PinnedItems.Count} taskbar pin(s) to '{prof.Name}'.");
            }
            break;
        }

    default:
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Unknown command: '{command}'");
        Console.ResetColor();
        PrintHelp();
        break;
}

static void PrintBanner()
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("==========================================================");
    Console.WriteLine("  Workspace Switcher - Command Line Interface (CLI)");
    Console.WriteLine("==========================================================");
    Console.ResetColor();
}

static void PrintHelp()
{
    Console.WriteLine(@"
Usage:
  workspaceswitcher <command> [options]

Commands:
  list                                          List all saved workspace profiles
  apply <name> [--launch] [--close-others]      Restore window layout & taskbar pins
  snapshot <name> [options]                     Capture active windows to profile
    --desc <text>                               Optional description
    --icon <glyph>                              Optional icon glyph (e.g. 💻, 🎮)
    --no-taskbar                                Skip taskbar pins capture
  export <name> <path.json>                     Export workspace profile to JSON
  import <path.json>                            Import workspace profile from JSON
  delete <name>                                 Delete a workspace profile
  monitor                                       Run headless global hotkey daemon
  help, --help, -h                              Show this help information

Examples:
  workspaceswitcher list
  workspaceswitcher apply Coding --launch
  workspaceswitcher snapshot Gaming --desc ""Triple Monitor Setup"" --icon 🎮
  workspaceswitcher export Coding C:\Backups\CodingWorkspace.json
  workspaceswitcher import C:\Backups\CodingWorkspace.json
");
}

static void ListProfiles(ProfileService profileService)
{
    PrintBanner();
    var profiles = profileService.GetAllProfiles();
    if (profiles.Count == 0)
    {
        Console.WriteLine("No profiles found. Create one using 'snapshot <name>' or via the GUI.");
        return;
    }

    Console.WriteLine($"\nFound {profiles.Count} profile(s) in: {profileService.ProfilesDirectory}\n");
    Console.WriteLine(string.Format("{0,-18} {1,-6} {2,-9} {3,-14} {4,-18} {5}", "Name", "Icon", "Windows", "Taskbar Pins", "Hotkey", "Last Modified"));
    Console.WriteLine(new string('-', 85));

    int idx = 0;
    foreach (var p in profiles.OrderByDescending(x => x.LastModifiedAt))
    {
        string hotkey = HotkeyHelper.FormatDisplayHotkey(p.HotkeyModifier, p.HotkeyKey, idx++);
        int winCount = p.Windows?.Count ?? 0;
        int pinCount = p.Taskbar?.PinnedItems?.Count ?? 0;
        string modStr = p.LastModifiedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

        Console.WriteLine(string.Format("{0,-18} {1,-6} {2,-9} {3,-14} {4,-18} {5}",
            p.Name.Length > 16 ? p.Name[..14] + ".." : p.Name,
            p.IconGlyph,
            winCount,
            pinCount,
            hotkey,
            modStr));
    }
    Console.WriteLine();
}

static void ApplyProfile(
    string profileName,
    string[] args,
    ProfileService profileService,
    WindowManager windowManager,
    SettingsService settingsService,
    TaskbarService taskbarService)
{
    var profile = profileService.LoadProfile(profileName);
    if (profile == null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: Profile '{profileName}' not found.");
        Console.ResetColor();
        return;
    }

    bool autoLaunch = args.Contains("--launch", StringComparer.OrdinalIgnoreCase);
    bool closeOthers = args.Contains("--close-others", StringComparer.OrdinalIgnoreCase);
    bool skipTaskbar = args.Contains("--skip-taskbar", StringComparer.OrdinalIgnoreCase);

    Console.WriteLine($"Applying workspace '{profile.Name}' ({profile.Windows.Count} windows)...");

    var settings = settingsService.Load();
    var previousProfile = string.IsNullOrEmpty(settings.LastActiveProfileName) ? null : profileService.LoadProfile(settings.LastActiveProfileName);

    var (restored, closed) = windowManager.RestoreWorkspace(
        profile,
        launchIfNotRunning: autoLaunch,
        previousProfile: previousProfile,
        closeAppsOnSwitch: closeOthers,
        switchTaskbarPins: !skipTaskbar,
        staticAppIdentifiers: settings.StaticPinnedApps);

    Console.ForegroundColor = ConsoleColor.Green;
    if (closed > 0)
    {
        Console.WriteLine($"Successfully repositioned {restored} window(s) and closed {closed} window(s) from previous workspace.");
    }
    else
    {
        Console.WriteLine($"Successfully repositioned {restored}/{profile.Windows.Count} window(s).");
    }
    Console.ResetColor();

    settings.LastActiveProfileName = profile.Name;
    settingsService.Save(settings);
}

static void SnapshotProfile(
    string profileName,
    string[] args,
    ProfileService profileService,
    WindowManager windowManager,
    SettingsService settingsService)
{
    string? desc = null;
    string icon = "💻";
    bool captureTaskbar = true;

    for (int i = 2; i < args.Length; i++)
    {
        if (args[i].Equals("--desc", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            desc = args[++i];
        }
        else if (args[i].Equals("--icon", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            icon = args[++i];
        }
        else if (args[i].Equals("--no-taskbar", StringComparison.OrdinalIgnoreCase))
        {
            captureTaskbar = false;
        }
    }

    var staticPins = settingsService.Load().StaticPinnedApps;

    Console.WriteLine($"Capturing snapshot for '{profileName}'...");
    var profile = windowManager.CaptureWorkspace(
        profileName,
        desc,
        icon,
        captureTaskbar: captureTaskbar,
        staticAppIdentifiers: staticPins);

    profileService.SaveProfile(profile);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"Saved profile '{profile.Name}' with {profile.Windows.Count} windows and {profile.Taskbar?.PinnedItems.Count ?? 0} taskbar pins.");
    Console.ResetColor();
}

static void ExportProfile(string profileName, string destinationFilePath, ProfileService profileService)
{
    try
    {
        profileService.ExportProfile(profileName, destinationFilePath);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Profile '{profileName}' successfully exported to: {destinationFilePath}");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Export failed: {ex.Message}");
        Console.ResetColor();
    }
}

static void ImportProfile(string sourceFilePath, ProfileService profileService)
{
    try
    {
        var imported = profileService.ImportProfile(sourceFilePath);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Profile '{imported.Name}' successfully imported with {imported.Windows.Count} windows.");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Import failed: {ex.Message}");
        Console.ResetColor();
    }
}

static void DeleteProfile(string profileName, ProfileService profileService)
{
    if (profileService.DeleteProfile(profileName))
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Profile '{profileName}' was deleted.");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Profile '{profileName}' does not exist.");
        Console.ResetColor();
    }
}

static void RunMonitor(ProfileService profileService, WindowManager windowManager, SettingsService settingsService)
{
    PrintBanner();
    Console.WriteLine("Starting Headless Global Hotkey Daemon...\n");

    using var hotkeyManager = new HotkeyManager();

    hotkeyManager.HotKeyPressed += (sender, e) =>
    {
        if (e.Binding != null && e.Binding.Action == HotKeyAction.RestoreProfile)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Hotkey triggered for profile: '{e.Binding.TargetProfileName}'");
            Console.ResetColor();

            var target = profileService.LoadProfile(e.Binding.TargetProfileName);
            if (target != null)
            {
                var settings = settingsService.Load();
                int restored = windowManager.RestoreWorkspace(target, launchIfNotRunning: settings.AutoLaunchMissingApps);
                Console.WriteLine($"  Restored {restored} window positions.");
            }
        }
    };

    var profiles = profileService.GetAllProfiles();
    int idx = 0;
    foreach (var p in profiles.OrderByDescending(x => x.LastModifiedAt).Take(5))
    {
        uint vk = HotkeyHelper.ParseVirtualKey(p.HotkeyKey, idx);
        var mods = HotkeyHelper.ParseModifiers(p.HotkeyModifier);
        if (vk != 0)
        {
            try
            {
                int id = hotkeyManager.Register(mods, vk, p.Name, HotKeyAction.RestoreProfile);
                string display = HotkeyHelper.FormatDisplayHotkey(p.HotkeyModifier, p.HotkeyKey, idx);
                Console.WriteLine($"  [Registered] {display} -> {p.Name} (ID: {id})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [Warning] Could not register hotkey for {p.Name}: {ex.Message}");
            }
        }
        idx++;
    }

    Console.WriteLine("\nDaemon is running. Press [Ctrl+C] to exit.");

    var autoReset = new ManualResetEvent(false);
    Console.CancelKeyPress += (s, e) =>
    {
        e.Cancel = true;
        autoReset.Set();
    };
    autoReset.WaitOne();
    Console.WriteLine("\nShutting down monitor daemon...");
}
