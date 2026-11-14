using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;
using MusicLyricApp.Views;

namespace MusicLyricAppTest.Regression;

/// <summary>LRC and SRT timestamp formats: presets, custom formats with token buttons, preview and validation.</summary>
public class TimestampFormatSettingsRegressionTests
{

    [Theory]
    [InlineData("[mm:ss.SSS]", true, null, null)]
    [InlineData("HH:mm:ss,SSS", false, null, null)]
    [InlineData("", true, "Settings.Timestamp.Error.Empty", null)]
    [InlineData("[ss.SSS]", true, "Settings.Timestamp.Error.Minutes", null)]
    [InlineData("[mm.SSS]", true, "Settings.Timestamp.Error.Seconds", null)]
    [InlineData("[mm:ss.SSSS]", true, "Settings.Timestamp.Error.Milliseconds", null)]
    [InlineData("[mm:ss.SS.S]", true, "Settings.Timestamp.Error.Milliseconds", null)]
    [InlineData("mm:ss.SS", true, null, "Settings.Timestamp.Warn.LrcBrackets")]
    [InlineData("mm:ss,SSS", false, null, "Settings.Timestamp.Warn.SrtHours")]
    public void ValidatorSeparatesBrokenFormatsFromUnusualOnes(string format, bool isLrc, string? error, string? warning)
    {
        var result = TimestampFormatValidator.Validate(format, isLrc);
        Assert.Equal(error, result.ErrorKey);
        Assert.Equal(warning, result.WarningKey);
    }

    [Theory]
    [InlineData("[mm:ss]", "HH", 1, 1, "[HHmm:ss]", 3)]
    [InlineData("[mm:ss.SSS]", "SS", 1, 1, "[mm:ss.SS]", 9)]   // a millisecond token replaces the existing run
    [InlineData("[mm:ss]", "SSS", 6, 6, "[mm:ssSSS]", 9)]
    [InlineData("[xx:ss]", "mm", 1, 3, "[mm:ss]", 3)]          // a selection is replaced
    public void InsertTokenPutsTheTokenAtTheCaret(string text, string token, int start, int end, string expected, int caret)
    {
        Assert.Equal((expected, caret), TimestampFormatValidator.InsertToken(text, token, start, end));
    }

    [Fact]
    public void SavedFormatsSelectTheirPresetOrCustom()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.LrcTimestampFormat = "[mm:ss.SS]";
        ws.Settings.Config.SrtTimestampFormat = "HH:mm:ss:SSS";
        var vm = new SettingParamViewModel();
        vm.Bind(ws.Settings);

        Assert.Equal("[mm:ss.SS]", vm.LrcTimestamp.SelectedPreset!.Format);
        Assert.False(vm.LrcTimestamp.IsCustom);
        Assert.True(vm.SrtTimestamp.IsCustom);
        Assert.Equal("HH:mm:ss:SSS", vm.SrtTimestamp.Text);

        // Picking a preset writes it; picking Custom keeps the current format to edit.
        vm.LrcTimestamp.SelectedPreset = vm.LrcTimestamp.Presets.Single(p => p.Format == "[mm:ss]");
        Assert.Equal("[mm:ss]", ws.Settings.Config.LrcTimestampFormat);
        vm.LrcTimestamp.SelectedPreset = vm.LrcTimestamp.Presets.Single(p => p.IsCustom);
        Assert.Equal("[mm:ss]", vm.LrcTimestamp.Text);
        Assert.Equal("[mm:ss]", ws.Settings.Config.LrcTimestampFormat);
    }

    [Fact]
    public void DefaultsStayTheSame()
    {
        var config = new SettingBean().Config;
        Assert.Equal("[mm:ss.SSS]", config.LrcTimestampFormat);
        Assert.Equal("HH:mm:ss,SSS", config.SrtTimestampFormat);
    }

    [AvaloniaFact]
    public void InvalidCustomFormatIsShownButNotSaved() => TestWorkspace.WithLanguage("zh-CN", () =>
    {
        using var ws = new TestWorkspace();
        var vm = new SettingParamViewModel();
        vm.Bind(ws.Settings);
        vm.LrcTimestamp.SelectedPreset = vm.LrcTimestamp.Presets.Single(p => p.IsCustom);

        vm.LrcTimestamp.Text = "[ss.SSS]";
        Assert.Equal("需要包含分钟 mm", vm.LrcTimestamp.Error);
        Assert.Equal("[mm:ss.SSS]", ws.Settings.Config.LrcTimestampFormat);
        // The preview keeps showing the saved format, which is what output still uses.
        Assert.Equal("1:23.456 显示为 [01:23.456]", vm.LrcTimestamp.Preview);

        // A warning still saves: the timestamp prints, players may just not read it.
        vm.LrcTimestamp.Text = "mm:ss.SS";
        Assert.Equal("", vm.LrcTimestamp.Error);
        Assert.Equal("没有用 [ ] 括起来，多数播放器无法识别", vm.LrcTimestamp.Warning);
        Assert.Equal("mm:ss.SS", ws.Settings.Config.LrcTimestampFormat);
        Assert.Equal("1:23.456 显示为 01:23.45", vm.LrcTimestamp.Preview);
    });

    [AvaloniaFact]
    public void RoundingRuleFollowsTheFormatsAndThePreview() => TestWorkspace.WithLanguage("zh-CN", () =>
    {
        using var ws = new TestWorkspace();
        var vm = new SettingParamViewModel();
        vm.Bind(ws.Settings);

        // Defaults keep three digits, so rounding does nothing.
        Assert.False(vm.IsDotTypeApplicable);
        Assert.Equal("当前格式保留 3 位毫秒或不含毫秒，此项不生效", vm.DotTypeHint);

        vm.LrcTimestamp.SelectedPreset = vm.LrcTimestamp.Presets.Single(p => p.Format == "[mm:ss.SS]");
        Assert.True(vm.IsDotTypeApplicable);
        Assert.Equal("1:23.456 显示为 [01:23.45]", vm.LrcTimestamp.Preview);

        vm.DotType.Value = DotTypeEnum.HALF_UP;
        Assert.Equal("1:23.456 显示为 [01:23.46]", vm.LrcTimestamp.Preview);
    });

    [AvaloniaFact]
    public void CustomEditorAppearsOnlyForCustomAndTokenButtonsInsertAtTheCaret()
    {
        using var ws = new TestWorkspace();
        var vm = new SettingViewModel(ws.Settings, ws.Window.Object) { CurrentSection = SettingViewModel.SectionLyrics };
        var window = TestWorkspace.Host(new SettingView(), vm);
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            // A preset hides the custom editor; its row is not even laid out yet.
            Assert.DoesNotContain(window.GetVisualDescendants().OfType<TextBox>(),
                x => x.Name == "LrcTimestampInput" && x.IsEffectivelyVisible);

            var lrc = vm.SettingParamViewModel.LrcTimestamp;
            lrc.SelectedPreset = lrc.Presets.Single(p => p.IsCustom);
            Dispatcher.UIThread.RunJobs();
            var input = window.GetVisualDescendants().OfType<TextBox>().Single(x => x.Name == "LrcTimestampInput");
            Assert.True(input.IsEffectivelyVisible);
            Assert.Equal("[mm:ss.SSS]", input.Text);

            // Put the caret after "[" and insert HH, then switch the milliseconds to two digits.
            input.SelectionStart = input.SelectionEnd = input.CaretIndex = 1;
            Click(window, "LrcTimestampTokens", "HH");
            Assert.Equal("[HHmm:ss.SSS]", input.Text);
            Click(window, "LrcTimestampTokens", "SS");
            Assert.Equal("[HHmm:ss.SS]", ws.Settings.Config.LrcTimestampFormat);
        }
        finally { window.Close(); }
    }

    private static void Click(Window window, string tokensName, string token)
    {
        var tokens = window.GetVisualDescendants().OfType<ItemsControl>().Single(x => x.Name == tokensName);
        var button = tokens.GetVisualDescendants().OfType<Button>()
            .Single(b => b.DataContext is TimestampToken t && t.Token == token);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }
}
