using WorkspaceSwitcher.Core.Services;
using Xunit;

namespace WorkspaceSwitcher.Tests;

public class AppIdentityHelperTests
{
    [Theory]
    [InlineData("code", "Visual Studio Code")]
    [InlineData("devenv", "Visual Studio")]
    [InlineData("firefox", "Mozilla Firefox")]
    [InlineData("chrome", "Google Chrome")]
    [InlineData("msedge", "Microsoft Edge")]
    [InlineData("explorer", "File Explorer")]
    [InlineData("spotify", "Spotify")]
    [InlineData("steam", "Steam")]
    [InlineData("steamwebhelper", "Steam")]
    [InlineData("taskmgr", "Task Manager")]
    public void GetFriendlyName_KnownProcessNames_ReturnsMappedFriendlyName(string procName, string expected)
    {
        var result = AppIdentityHelper.GetFriendlyName(procName);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("notepad_win64", "notepad")]
    [InlineData("myapp_x64", "myapp")]
    [InlineData("ts3client_win64", "ts3")]
    [InlineData("CustomClient", "Custom")]
    public void CleanProcessName_StripsTechnicalSuffixes(string procName, string expected)
    {
        var result = AppIdentityHelper.CleanProcessName(procName);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("steam", "steamwebhelper", true)]
    [InlineData("steamwebhelper", "steam", true)]
    [InlineData("powershell", "pwsh", true)]
    [InlineData("WindowsTerminal", "wt", true)]
    [InlineData("teamspeak", "ts3client_win64", true)]
    [InlineData("notepad", "calc", false)]
    public void IsMatchingProcess_CorrectlyCorrelatesHelpersAndAliases(string proc1, string proc2, bool expectedMatch)
    {
        var result = AppIdentityHelper.IsMatchingProcess(proc1, proc2);
        Assert.Equal(expectedMatch, result);
    }

    [Fact]
    public void ResolveLaunchableExecutable_ForStandardApps_ReturnsValidOrSensibleExecutable()
    {
        var cmd = AppIdentityHelper.ResolveLaunchableExecutable("cmd", null);
        Assert.NotNull(cmd);
        Assert.EndsWith("cmd.exe", cmd, StringComparison.OrdinalIgnoreCase);

        var explorer = AppIdentityHelper.ResolveLaunchableExecutable("explorer", null);
        Assert.NotNull(explorer);
        Assert.EndsWith("explorer.exe", explorer, StringComparison.OrdinalIgnoreCase);
    }
}
