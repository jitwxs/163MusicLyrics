using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia.Headless.XUnit;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.ViewModels;

namespace MusicLyricAppTest.Regression;

public class LocalizationRegressionTests
{
    [Fact]
    public void TraditionalResourcesContainEveryKeyAndPreservePlaceholders()
    {
        var assembly = typeof(LocalizationService).Assembly;
        Dictionary<string, string> Read(string culture)
        {
            using var stream = assembly.GetManifestResourceStream($"MusicLyricApp.Resources.Localization.{culture}.json");
            Assert.NotNull(stream);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
        }

        var simplified = Read("zh-CN");
        var traditional = Read("zh-TW");
        Assert.Equal(simplified.Keys.Order(), traditional.Keys.Order());
        foreach (var (key, value) in simplified)
        {
            Assert.False(string.IsNullOrWhiteSpace(traditional[key]));
            Assert.Equal(Regex.Matches(value, @"\{[^{}]+\}").Select(m => m.Value).Order(),
                Regex.Matches(traditional[key], @"\{[^{}]+\}").Select(m => m.Value).Order());
        }
    }

    [AvaloniaFact]
    public void TraditionalSettingRestoresAndSwitchesAtRuntime()
    {
        var original = LocalizationService.Instance.Language;
        var culture = CultureInfo.CurrentUICulture;
        using var ws = new TestWorkspace();
        try
        {
            ws.Settings.Config.Language = "zh-tw";
            var vm = new SettingParamViewModel();
            vm.Bind(ws.Settings);
            Assert.Equal("zh-TW", vm.SelectedLanguage!.Code);
            Assert.Equal("zh-TW", ws.Settings.Config.Language);
            Assert.Equal("zh-TW", LocalizationService.Instance.Language);
            Assert.Equal("zh-TW", CultureInfo.CurrentUICulture.Name);
            Assert.Equal("設定", LocalizationService.Get("Nav.Settings"));
            Assert.Equal("繁體中文", vm.SelectedLanguage.DisplayName);
            Assert.Equal("顯示 1 / 2 首", LocalizationService.Format("Batch.FilterCount", 1, 2));
            vm.SelectedLanguage = vm.Languages.Single(l => l.Code == "en-US");
            Assert.Equal("Settings", LocalizationService.Get("Nav.Settings"));
            vm.SelectedLanguage = vm.Languages.Single(l => l.Code == "zh-TW");
            Assert.Equal("設定", LocalizationService.Instance["Nav.Settings"]);
        }
        finally
        {
            LocalizationService.Instance.SetLanguage(original);
            CultureInfo.CurrentUICulture = culture;
        }
    }
}
