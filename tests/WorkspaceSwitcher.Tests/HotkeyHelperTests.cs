using WorkspaceSwitcher.Core.Hotkeys;
using Xunit;

namespace WorkspaceSwitcher.Tests;

public class HotkeyHelperTests
{
    [Theory]
    [InlineData("Ctrl + Alt", KeyModifiers.Control | KeyModifiers.Alt)]
    [InlineData("Ctrl + Shift", KeyModifiers.Control | KeyModifiers.Shift)]
    [InlineData("Alt + Shift", KeyModifiers.Alt | KeyModifiers.Shift)]
    [InlineData("Win + Alt", KeyModifiers.Win | KeyModifiers.Alt)]
    [InlineData("Ctrl + Win", KeyModifiers.Control | KeyModifiers.Win)]
    [InlineData("None", KeyModifiers.None)]
    [InlineData("", KeyModifiers.Control | KeyModifiers.Alt)]
    [InlineData(null, KeyModifiers.Control | KeyModifiers.Alt)]
    public void ParseModifiers_ReturnsExpectedFlags(string? input, KeyModifiers expected)
    {
        var result = HotkeyHelper.ParseModifiers(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1", 0, (uint)'1')]
    [InlineData("9", 0, (uint)'9')]
    [InlineData("A", 0, (uint)'A')]
    [InlineData("Z", 0, (uint)'Z')]
    [InlineData("F1", 0, 0x70u)]
    [InlineData("F12", 0, 0x7Bu)]
    [InlineData("Auto (1-5)", 0, (uint)'1')]
    [InlineData("Auto (1-5)", 2, (uint)'3')]
    [InlineData("None (Disabled)", 0, 0u)]
    public void ParseVirtualKey_ReturnsExpectedVirtualKeyCode(string? keyStr, int defaultIndex, uint expectedVk)
    {
        var result = HotkeyHelper.ParseVirtualKey(keyStr, defaultIndex);
        Assert.Equal(expectedVk, result);
    }

    [Theory]
    [InlineData("Ctrl + Alt", "1", 0, "Ctrl + Alt + 1")]
    [InlineData("Ctrl + Alt", "Auto (1-5)", 0, "Ctrl + Alt + 1")]
    [InlineData("Ctrl + Alt", "Auto (1-5)", 1, "Ctrl + Alt + 2")]
    [InlineData("Ctrl + Alt", "None (Disabled)", 0, "No Hotkey")]
    [InlineData("None", "F5", 0, "F5")]
    public void FormatDisplayHotkey_ReturnsHumanReadableString(string? modifier, string? key, int index, string expected)
    {
        var result = HotkeyHelper.FormatDisplayHotkey(modifier, key, index);
        Assert.Equal(expected, result);
    }
}
