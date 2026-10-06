using System.Collections.Generic;
using System.Globalization;
using WorkspaceSwitcher.Core.Localization;
using Xunit;

namespace WorkspaceSwitcher.Tests;

public class LocalizationTests
{
    private static CultureInfo Culture(string name) => CultureInfo.GetCultureInfo(name);

    [Theory]
    [InlineData("en", "zh-CN", UiLanguage.English)]
    [InlineData("en", "zh-Hans", UiLanguage.English)]
    [InlineData("en", "zh-SG", UiLanguage.English)]
    [InlineData("zh-CN", "en-US", UiLanguage.ChineseSimplified)]
    [InlineData("zh-CN", "zh-TW", UiLanguage.ChineseSimplified)]
    [InlineData("zh-CN", "zh-Hant", UiLanguage.ChineseSimplified)]
    public void ResolveLanguage_ExplicitPreference_WinsOverSystemUiLanguage(
        string preference, string systemUiCulture, UiLanguage expected)
    {
        Assert.Equal(expected, Localizer.ResolveLanguage(preference, Culture(systemUiCulture)));
    }

    [Theory]
    [InlineData("zh-CN", UiLanguage.ChineseSimplified)]
    [InlineData("zh-Hans", UiLanguage.ChineseSimplified)]
    [InlineData("zh-SG", UiLanguage.ChineseSimplified)]
    [InlineData("zh-Hans-SG", UiLanguage.ChineseSimplified)]
    [InlineData("zh-TW", UiLanguage.English)]
    [InlineData("zh-HK", UiLanguage.English)]
    [InlineData("zh-Hant", UiLanguage.English)]
    [InlineData("zh-Hant-TW", UiLanguage.English)]
    [InlineData("zh", UiLanguage.English)]
    [InlineData("en-US", UiLanguage.English)]
    [InlineData("", UiLanguage.English)]
    public void ResolveLanguage_SystemPreference_FollowsSystemUiLanguage(
        string systemUiCulture, UiLanguage expected)
    {
        Assert.Equal(expected, Localizer.ResolveLanguage(LanguagePreference.System, Culture(systemUiCulture)));
    }

    [Theory]
    [InlineData(null, "zh-Hans", UiLanguage.ChineseSimplified)]
    [InlineData("", "zh-CN", UiLanguage.ChineseSimplified)]
    [InlineData("fr", "zh-CN", UiLanguage.ChineseSimplified)]
    [InlineData("fr", "zh-TW", UiLanguage.English)]
    [InlineData("de-DE", "en-US", UiLanguage.English)]
    public void ResolveLanguage_MissingOrInvalidPreference_BehavesAsSystem(
        string? preference, string systemUiCulture, UiLanguage expected)
    {
        Assert.Equal(expected, Localizer.ResolveLanguage(preference, Culture(systemUiCulture)));
    }

    [Theory]
    [InlineData("en", "en")]
    [InlineData("zh-CN", "zh-CN")]
    [InlineData("system", "system")]
    [InlineData(null, "system")]
    [InlineData("", "system")]
    [InlineData("fr", "system")]
    [InlineData("zh-TW", "system")]
    public void LanguagePreference_Normalize_MissingOrInvalidValues_MapToSystem(string? value, string expected)
    {
        Assert.Equal(expected, LanguagePreference.Normalize(value));
    }

    [Theory]
    [InlineData(UiLanguage.English, "LanguageSetting.Label", "Language")]
    [InlineData(UiLanguage.English, "LanguageSetting.FollowSystem", "Follow System")]
    [InlineData(UiLanguage.English, "LanguageSetting.RestartHint", "Language settings take effect on the next restart.")]
    [InlineData(UiLanguage.ChineseSimplified, "LanguageSetting.Label", "语言")]
    [InlineData(UiLanguage.ChineseSimplified, "LanguageSetting.FollowSystem", "跟随系统")]
    [InlineData(UiLanguage.ChineseSimplified, "LanguageSetting.RestartHint", "语言设置将在下次启动时生效。")]
    [InlineData(UiLanguage.English, "LanguageSetting.English", "English")]
    [InlineData(UiLanguage.ChineseSimplified, "LanguageSetting.English", "English")]
    [InlineData(UiLanguage.English, "LanguageSetting.ChineseSimplified", "简体中文")]
    [InlineData(UiLanguage.ChineseSimplified, "LanguageSetting.ChineseSimplified", "简体中文")]
    public void Get_ReturnsRealTextInEffectiveLanguage(UiLanguage language, string key, string expected)
    {
        Assert.Equal(expected, new Localizer(language).Get(key));
    }

    [Fact]
    public void Get_WhenChineseResourceMissesKey_FallsBackToEnglishText()
    {
        // "LanguageSetting.English" is intentionally absent from the zh-CN resource:
        // it is a self-name and the missing-key path must resolve to the English text.
        var text = new Localizer(UiLanguage.ChineseSimplified).Get("LanguageSetting.English");

        Assert.Equal("English", text);
        Assert.NotEqual("LanguageSetting.English", text);
        Assert.False(string.IsNullOrWhiteSpace(text));
    }

    [Fact]
    public void Get_UnknownKey_ThrowsInsteadOfReturningKeyOrEmpty()
    {
        var localizer = new Localizer(UiLanguage.English);

        Assert.Throws<KeyNotFoundException>(() => localizer.Get("LanguageSetting.DoesNotExist"));
    }

    [Theory]
    [InlineData(UiLanguage.English)]
    [InlineData(UiLanguage.ChineseSimplified)]
    public void Format_WithoutParameters_ReturnsEffectiveTemplateText(UiLanguage language)
    {
        var localizer = new Localizer(language);

        Assert.Equal(localizer.Get("LanguageSetting.RestartHint"), localizer.Format("LanguageSetting.RestartHint"));
    }
}
