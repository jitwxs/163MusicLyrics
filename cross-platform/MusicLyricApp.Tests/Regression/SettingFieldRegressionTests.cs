using Avalonia.Headless.XUnit;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;

namespace MusicLyricAppTest.Regression;

/// <summary>EnumChoice and NumberSetting, which every enum select and number input of the settings is built from.</summary>
public class SettingFieldRegressionTests
{
    [Fact]
    public void EnumChoiceAppliesEachChangeOnceAndKeepsItsViewsInStep()
    {
        var applied = new List<DotTypeEnum>();
        var choice = new EnumChoice<DotTypeEnum>(applied.Add);
        var changed = new List<string?>();
        choice.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        choice.Value = DotTypeEnum.HALF_UP;
        choice.Value = DotTypeEnum.HALF_UP;               // no change, no second apply
        choice.SelectedValue = DotTypeEnum.DOWN;          // what a segmented control sets
        choice.SelectedValue = "not an enum";             // ignored

        Assert.Equal([DotTypeEnum.HALF_UP, DotTypeEnum.DOWN], applied);
        Assert.Same(choice.Items.Single(i => i.Value == DotTypeEnum.DOWN), choice.Selected);
        Assert.Equal(DotTypeEnum.DOWN, choice.SelectedValue);
        Assert.Contains(nameof(choice.Value), changed);
        Assert.Contains(nameof(choice.SelectedValue), changed);
    }

    [Fact]
    public void NumberSettingAppliesOnlyNumbersInRange()
    {
        var applied = new List<int>();
        var setting = new NumberSetting(1, 10, () => "range", applied.Add, notANumber: () => "nan");

        setting.Text = "abc";
        Assert.Equal(("nan", false), (setting.Error, setting.IsValid));
        setting.Text = "11";
        Assert.Equal("range", setting.Error);
        setting.Load(7);
        Assert.Equal(("7", "", true), (setting.Text, setting.Error, setting.IsValid));

        Assert.Equal([7], applied);
    }

    [AvaloniaFact]
    public void NumberSettingErrorsFollowTheLanguage() => TestWorkspace.WithLanguage("zh-CN", () =>
    {
        var setting = new NumberSetting(1, 65535, () => LocalizationService.Get("Message.InvalidPort"), _ => { });
        setting.Text = "70000";
        var chinese = setting.Error;

        LocalizationService.Instance.SetLanguage("en-US");

        Assert.Equal(LocalizationService.Get("Message.InvalidPort"), setting.Error);
        Assert.NotEqual(chinese, setting.Error);
    });

    [Fact]
    public void SettingsChoicesWriteTheirSettingAndItsSideEffects()
    {
        using var ws = new TestWorkspace();
        var vm = new SettingParamViewModel();
        vm.Bind(ws.Settings);

        vm.FileConflictStrategy.Value = FileConflictStrategyEnum.APPEND_NUMBER;
        Assert.Equal(FileConflictStrategyEnum.APPEND_NUMBER, ws.Settings.Config.FileConflictStrategy);
        Assert.True(vm.IsConflictSuffixEnabled);

        var proxyChanged = false;
        vm.PropertyChanged += (_, e) => proxyChanged |= e.PropertyName == nameof(vm.IsHttpProxySelected);
        vm.NetworkProxyMode.Value = NetworkProxyModeEnum.HTTP_PROXY;
        Assert.True(vm.IsHttpProxySelected);
        Assert.True(proxyChanged);
        vm.NetworkProxyMode.Value = NetworkProxyModeEnum.SYSTEM_PROXY;

        vm.SearchCacheMaxSizeMb.Text = "4096";
        Assert.NotEqual(4096, ws.Settings.Config.SearchCacheMaxSizeMb);
        vm.SearchCacheMaxSizeMb.Text = "256";
        Assert.Equal(256, ws.Settings.Config.SearchCacheMaxSizeMb);
    }

    [Fact]
    public void SearchChoicesAreRaisedUnderTheirOwnNameForThePreviewAndLoginState()
    {
        var param = new SearchParamViewModel();
        var bean = new PersistParamBean();
        param.Bind(bean);
        var changed = new List<string?>();
        param.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        param.SearchSource.Value = SearchSourceEnum.QQ_MUSIC;
        param.LrcType.Value = ShowLrcTypeEnum.MERGE;

        Assert.Equal((SearchSourceEnum.QQ_MUSIC, ShowLrcTypeEnum.MERGE), (bean.SearchSource, bean.ShowLrcType));
        Assert.Equal([nameof(param.SearchSource), nameof(param.LrcType)], changed);
    }
}
