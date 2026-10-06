using System.Linq;
using WorkspaceSwitcher.Core.Hotkeys;
using WorkspaceSwitcher.Core.Localization;
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

    [Theory]
    [InlineData(UiLanguage.English, "None", "None")]
    [InlineData(UiLanguage.English, "Ctrl + Alt", "Ctrl + Alt")]
    [InlineData(UiLanguage.ChineseSimplified, "None", "无")]
    [InlineData(UiLanguage.ChineseSimplified, "Ctrl + Alt", "Ctrl + Alt")]
    [InlineData(UiLanguage.ChineseSimplified, "Ctrl + Shift", "Ctrl + Shift")]
    [InlineData(UiLanguage.ChineseSimplified, "Win + Alt", "Win + Alt")]
    public void ModifierOptions_SeparateDisplayTextFromStableValue(UiLanguage language, string value, string expectedDisplay)
    {
        var option = HotkeyDisplay.CreateModifierOptions(new Localizer(language)).Single(o => o.Value == value);

        Assert.Equal(expectedDisplay, option.Display);
    }

    [Theory]
    [InlineData(UiLanguage.English, "Auto (1-5)", "Auto (1-5)")]
    [InlineData(UiLanguage.English, "None (Disabled)", "None (Disabled)")]
    [InlineData(UiLanguage.English, "F5", "F5")]
    [InlineData(UiLanguage.ChineseSimplified, "Auto (1-5)", "自动分配（1–5）")]
    [InlineData(UiLanguage.ChineseSimplified, "None (Disabled)", "禁用快捷键")]
    [InlineData(UiLanguage.ChineseSimplified, "1", "1")]
    [InlineData(UiLanguage.ChineseSimplified, "A", "A")]
    [InlineData(UiLanguage.ChineseSimplified, "F5", "F5")]
    public void KeyOptions_SeparateDisplayTextFromStableValue(UiLanguage language, string value, string expectedDisplay)
    {
        var option = HotkeyDisplay.CreateKeyOptions(new Localizer(language)).Single(o => o.Value == value);

        Assert.Equal(expectedDisplay, option.Display);
    }

    [Theory]
    [InlineData("无", "None", KeyModifiers.None)]
    [InlineData("Ctrl + Win", "Ctrl + Win", KeyModifiers.Control | KeyModifiers.Win)]
    [InlineData("Ctrl + Alt", "Ctrl + Alt", KeyModifiers.Control | KeyModifiers.Alt)]
    public void ModifierOptionSelectedByChineseDisplay_SavedValue_ParsesToOriginalModifiers(
        string display, string expectedValue, KeyModifiers expected)
    {
        var option = HotkeyDisplay.CreateModifierOptions(new Localizer(UiLanguage.ChineseSimplified))
            .Single(o => o.Display == display);

        Assert.Equal(expectedValue, option.Value);
        Assert.Equal(expected, HotkeyHelper.ParseModifiers(option.Value));
    }

    [Theory]
    [InlineData("自动分配（1–5）", "Auto (1-5)", 2, (uint)'3')]
    [InlineData("禁用快捷键", "None (Disabled)", 0, 0u)]
    [InlineData("F5", "F5", 0, 0x74u)]
    public void KeyOptionSelectedByChineseDisplay_SavedValue_ParsesToOriginalVirtualKey(
        string display, string expectedValue, int defaultIndex, uint expectedVk)
    {
        var option = HotkeyDisplay.CreateKeyOptions(new Localizer(UiLanguage.ChineseSimplified))
            .Single(o => o.Display == display);

        Assert.Equal(expectedValue, option.Value);
        Assert.Equal(expectedVk, HotkeyHelper.ParseVirtualKey(option.Value, defaultIndex));
    }

    [Theory]
    [InlineData("Ctrl + Alt", "1", 0, "Ctrl + Alt + 1")]
    [InlineData("Ctrl + Win", "Auto (1-5)", 0, "Ctrl + Win + 1")]
    [InlineData("None", "F5", 0, "F5")]
    [InlineData("Ctrl + Alt", "None (Disabled)", 0, "未设置快捷键")]
    [InlineData("Ctrl + Alt", "None (Disabled)", 9, "未设置快捷键")]
    public void Format_Localized_ReturnsDisplayTextWithLocalizedNoHotkeyText(
        string modifier, string? key, int index, string expected)
    {
        var result = HotkeyDisplay.Format(new Localizer(UiLanguage.ChineseSimplified), modifier, key, index);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Resolve_EditOfOldProfileValues_SelectsMatchingOption()
    {
        var localizer = new Localizer(UiLanguage.ChineseSimplified);
        var modifiers = HotkeyDisplay.CreateModifierOptions(localizer);
        var keys = HotkeyDisplay.CreateKeyOptions(localizer);

        Assert.Equal("自动分配（1–5）", HotkeyDisplay.Resolve(keys, "Auto (1-5)").Display);
        Assert.Equal("禁用快捷键", HotkeyDisplay.Resolve(keys, "None (Disabled)").Display);
        Assert.Equal("F12", HotkeyDisplay.Resolve(keys, "F12").Display);
        Assert.Equal("无", HotkeyDisplay.Resolve(modifiers, "None").Display);
        Assert.Equal("Ctrl + Alt", HotkeyDisplay.Resolve(modifiers, "Ctrl + Alt").Display);
    }

    [Fact]
    public void Resolve_UnknownLegacyValue_FallsBackToFirstOption()
    {
        var modifiers = HotkeyDisplay.CreateModifierOptions(new Localizer(UiLanguage.English));

        var option = HotkeyDisplay.Resolve(modifiers, "Ctrl + Super");

        Assert.Same(modifiers[0], option);
    }
}
