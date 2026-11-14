using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using Moq;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Service.Login;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;
using MusicLyricApp.Views;

namespace MusicLyricAppTest.Regression;

public class LoginStatusRegressionTests
{
    private const string NetEaseCookie = "MUSIC_U=abc; __csrf=def";
    private const string QqCookie = "uin=o0012345; qm_keyst=Q_H_L_key";

    /// <summary>Records each probe and answers from a table, optionally after a gate opens.</summary>
    private sealed class FakeProbe(Func<SearchSourceEnum, string, LoginProbeResult> answer) : ILoginProbe
    {
        public List<(SearchSourceEnum Source, string Cookie)> Calls { get; } = [];
        public ManualResetEventSlim? Gate { get; init; }

        public LoginProbeResult Probe(SearchSourceEnum source, string cookie)
        {
            lock (Calls) Calls.Add((source, cookie));
            Gate?.Wait(TimeSpan.FromSeconds(10));
            return answer(source, cookie);
        }
    }

    private static FakeProbe Valid(string nickname = "Lemon") => new((_, _) => new(LoginState.Valid, nickname));

    [Theory]
    [InlineData("Cookie: MUSIC_U=abc;\r\n __csrf=def ", "MUSIC_U=abc; __csrf=def")]
    [InlineData("\"MUSIC_U=abc\"", "MUSIC_U=abc")]
    [InlineData("   ", "")]
    public void PastedCookiesAreCleanedUp(string pasted, string expected)
    {
        Assert.Equal(expected, CookieInspector.Normalize(pasted));
    }

    [Theory]
    [InlineData(SearchSourceEnum.NET_EASE_MUSIC, "MUSIC_U=abc", true)]
    [InlineData(SearchSourceEnum.NET_EASE_MUSIC, "NMTID=abc; __csrf=1", false)]
    [InlineData(SearchSourceEnum.QQ_MUSIC, "uin=o0012345; qm_keyst=Q_H_L", true)]
    [InlineData(SearchSourceEnum.QQ_MUSIC, "wxuin=o1152921504; qqmusic_key=W_X", true)]
    [InlineData(SearchSourceEnum.QQ_MUSIC, "uin=o0012345", false)]
    [InlineData(SearchSourceEnum.QQ_MUSIC, "qm_keyst=Q_H_L", false)]
    public void LoginTokenIsRequiredBeforeAnyRequest(SearchSourceEnum source, string cookie, bool expected)
    {
        Assert.Equal(expected, CookieInspector.HasLoginToken(source, cookie));
    }

    [Fact]
    public void QqUinDropsWebClientPrefix()
    {
        Assert.Equal("12345", CookieInspector.QqUin("pgv_pvid=1; uin=o0012345; qm_keyst=x"));
        Assert.Equal("", CookieInspector.QqUin("uin=; qm_keyst=x"));
    }

    [Fact]
    public void PlatformAnswersMapToStates()
    {
        Assert.Equal(new LoginProbeResult(LoginState.Valid, "Lemon"),
            NativeLoginProbe.ParseNetEase("""{"code":200,"account":{"id":1},"profile":{"nickname":"Lemon"}}"""));
        Assert.Equal(LoginState.Invalid, NativeLoginProbe.ParseNetEase("""{"code":200,"account":null,"profile":null}""").State);
        Assert.Equal(LoginState.Unknown, NativeLoginProbe.ParseNetEase("""{"code":-460,"message":"Cheating"}""").State);
        Assert.Equal(new LoginProbeResult(LoginState.Valid, "Lemon~"),
            NativeLoginProbe.ParseQq("""{"code":0,"data":{"creator":{"nick":"Lemon~"}}}"""));
        Assert.Equal(LoginState.Invalid, NativeLoginProbe.ParseQq("""{"code":1000,"subcode":1000,"msg":"","data":{}}""").State);
        // Other codes are not proof that the cookie is wrong.
        Assert.Equal(LoginState.Unknown, NativeLoginProbe.ParseQq("""{"code":-1,"data":{}}""").State);
    }

    [Fact]
    public void EmptyAndIncompleteCookiesAreDecidedWithoutRequests()
    {
        var probe = Valid();
        var cookies = new Dictionary<SearchSourceEnum, string>
            { [SearchSourceEnum.NET_EASE_MUSIC] = "", [SearchSourceEnum.QQ_MUSIC] = "uin=o001" };
        using var login = new LoginStatusViewModel(s => cookies[s], probe);
        Assert.Equal(LoginState.NotConfigured, login.NetEase.State);
        Assert.Equal(LoginState.Malformed, login.QQ.State);
        Assert.True(login.QQ.NeedsAttention);
        Assert.Contains("qm_keyst", login.QQ.Detail);
        Assert.Empty(probe.Calls);
    }

    [Fact]
    public async Task CheckShowsNicknameAndTimeAndKeepsUncertainResultsApartFromExpired()
    {
        var answers = new Queue<LoginProbeResult>([new(LoginState.Valid, "Lemon"), new(LoginState.Unknown, Detail: "timeout"), new(LoginState.Invalid)]);
        var probe = new FakeProbe((_, _) => answers.Dequeue());
        using var login = new LoginStatusViewModel(_ => NetEaseCookie, probe);

        await login.CheckAsync(SearchSourceEnum.NET_EASE_MUSIC);
        Assert.Equal(LocalizationService.Format("Login.State.ValidAs", "Lemon"), login.NetEase.Label);
        Assert.NotNull(login.NetEase.CheckedAt);
        Assert.Contains(login.NetEase.CheckedAt!.Value.ToString("HH:mm"), login.NetEase.Detail);

        await login.CheckAsync(SearchSourceEnum.NET_EASE_MUSIC);
        Assert.True(login.NetEase.IsUncertain);
        Assert.False(login.NetEase.NeedsAttention);
        Assert.Contains("timeout", login.NetEase.Detail);

        await login.CheckAsync(SearchSourceEnum.NET_EASE_MUSIC);
        Assert.True(login.NetEase.IsInvalid);
        Assert.Equal(TKCode.Arc.BadgeTone.Danger, login.NetEase.Tone);
    }

    [Fact]
    public async Task EditingChecksOnceAfterTypingStopsWithTheLatestCookie()
    {
        var probe = Valid();
        var cookie = "";
        using var login = new LoginStatusViewModel(_ => cookie, probe, TimeSpan.FromMilliseconds(80));
        foreach (var typed in new[] { "MUSIC_U=a", "MUSIC_U=ab", "MUSIC_U=abc" })
        {
            cookie = typed;
            login.ScheduleCheck(SearchSourceEnum.NET_EASE_MUSIC);
            Assert.True(login.NetEase.IsChecking);
        }

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (login.NetEase.State != LoginState.Valid && DateTime.UtcNow < deadline) await Task.Delay(20, TestContext.Current.CancellationToken);
        Assert.Equal(LoginState.Valid, login.NetEase.State);
        Assert.Equal([(SearchSourceEnum.NET_EASE_MUSIC, "MUSIC_U=abc")], probe.Calls);

        // Clearing the field needs no request and shows "not signed in" at once.
        cookie = "";
        login.ScheduleCheck(SearchSourceEnum.NET_EASE_MUSIC);
        Assert.Equal(LoginState.NotConfigured, login.NetEase.State);
        Assert.Single(probe.Calls);
    }

    [Fact]
    public async Task AnOlderCheckDoesNotOverwriteANewerEdit()
    {
        using var gate = new ManualResetEventSlim();
        var probe = new FakeProbe((_, c) => new(LoginState.Valid, c)) { Gate = gate };
        var cookie = "MUSIC_U=old";
        using var login = new LoginStatusViewModel(_ => cookie, probe);
        var slow = login.CheckAsync(SearchSourceEnum.NET_EASE_MUSIC);
        cookie = "";
        login.ScheduleCheck(SearchSourceEnum.NET_EASE_MUSIC);
        gate.Set();
        await slow;
        Assert.Equal(LoginState.NotConfigured, login.NetEase.State);
    }

    [Fact]
    public async Task NeedLoginErrorRechecksAConfiguredCookieOnly()
    {
        var probe = Valid();
        var cookies = new Dictionary<SearchSourceEnum, string>
            { [SearchSourceEnum.NET_EASE_MUSIC] = NetEaseCookie, [SearchSourceEnum.QQ_MUSIC] = "" };
        using var login = new LoginStatusViewModel(s => cookies[s], probe);
        await login.CheckAllAsync();
        Assert.Single(probe.Calls);

        login.ReportNeedLogin(SearchSourceEnum.QQ_MUSIC);
        login.ReportNeedLogin(SearchSourceEnum.NET_EASE_MUSIC);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (probe.Calls.Count < 2 && DateTime.UtcNow < deadline) await Task.Delay(20, TestContext.Current.CancellationToken);
        Assert.Equal(2, probe.Calls.Count);
        Assert.All(probe.Calls, call => Assert.Equal(SearchSourceEnum.NET_EASE_MUSIC, call.Source));
        Assert.True(MainWindowViewModel.IsNeedLoginMessage("网易云: " + ErrorMsgConst.NEED_LOGIN));
        Assert.False(MainWindowViewModel.IsNeedLoginMessage(ErrorMsgConst.SONG_NOT_EXIST));
    }

    [AvaloniaFact]
    public void EditingACookieInSettingsSavesItCleanAndSchedulesACheck()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.QQMusicCookie = QqCookie;
        var probe = Valid();
        using var login = new LoginStatusViewModel(
            s => s == SearchSourceEnum.NET_EASE_MUSIC ? ws.Settings.Config.NetEaseCookie : ws.Settings.Config.QQMusicCookie,
            probe, TimeSpan.FromHours(1));
        var vm = new SettingViewModel(ws.Settings, ws.Window.Object, login: login) { CurrentSection = SettingViewModel.SectionExtensions };
        Assert.Empty(probe.Calls);
        Assert.Equal(LoginState.NotConfigured, login.NetEase.State);

        vm.SettingParamViewModel.NetEaseCookie = "Cookie: " + NetEaseCookie + "\n";
        Assert.Equal(NetEaseCookie, ws.Settings.Config.NetEaseCookie);
        Assert.True(login.NetEase.IsChecking);

        var window = TestWorkspace.Host(new SettingView(), vm);
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var badge = window.GetVisualDescendants().OfType<TKCode.Arc.Badge>().Single(b => b.Name == "NetEaseLoginBadge");
            Assert.True(badge.IsEffectivelyVisible);
            Assert.Equal(TKCode.Arc.BadgeTone.Info, badge.Tone);
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(),
                t => t.Text == LocalizationService.Get("Login.State.Checking") && t.IsEffectivelyVisible);
            var recheck = window.GetVisualDescendants().OfType<TKCode.Arc.Button>().Single(b => b.Name == "QqLoginCheckButton");
            Assert.Same(login.CheckCommand, recheck.Command);
            Assert.Equal(SearchSourceEnum.QQ_MUSIC, recheck.CommandParameter);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void OnlineLyricsPageShowsTheSourcesStateAndOpensCookieSettings()
    {
        using var ws = new TestWorkspace();
        ws.Storage().SaveConfig(ws.Settings);
        var window = new MainWindow(ws.Storage(), ws.Search(new(MockBehavior.Strict)));
        var vm = Assert.IsType<MainWindowViewModel>(window.DataContext);
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var text = window.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "LoginStatusText");
            Assert.True(text.IsEffectivelyVisible);
            Assert.Equal(vm.Login.Get(vm.SearchParamViewModel.SearchSource.Value).SourceLabel, text.Text);
            Assert.Contains(LocalizationService.Get("Login.State.NotConfigured"), text.Text);

            var other = vm.SearchParamViewModel.SearchSource.Items.First(x => !Equals(x.Value, vm.SearchParamViewModel.SearchSource.Value));
            vm.SearchParamViewModel.SearchSource.Selected = other;
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(other.Value.ToDescription(), text.Text);

            // An expired cookie reads 未登录 here, tinted as worth a look rather than red; settings keep 已失效.
            vm.Login.NetEase.Apply(new(LoginState.Invalid));
            vm.Login.QQ.Apply(new(LoginState.Invalid));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(LocalizationService.Format("Login.SourceState", other.Value.ToDescription(),
                LocalizationService.Get("Login.State.NotConfigured")), text.Text);
            Assert.DoesNotContain(LocalizationService.Get("Login.State.Invalid"), text.Text);
            Assert.Equal(LocalizationService.Get("Login.State.Invalid"), vm.Login.QQ.Label);
            var icon = window.GetVisualDescendants().OfType<TKCode.Arc.ArcGlyph>().Single(g => g.Name == "LoginStatusIcon");
            Assert.Contains("login-uncertain", icon.Classes);
            Assert.DoesNotContain("login-invalid", icon.Classes);
            Assert.Same(TKCode.Arc.ArcIcons.User, icon.Data);
            var button = window.GetVisualDescendants().OfType<TKCode.Arc.Button>().Single(b => b.Name == "LoginStatusButton");
            var tip = Assert.IsType<string>(ToolTip.GetTip(button));
            Assert.Contains(LocalizationService.Get("Login.Detail.Invalid"), tip);
            Assert.Contains(LocalizationService.Get("Login.ClickToSettings"), tip);

            // A click opens the cookie settings at the picked source's field, scrolled into view and focused.
            button.Command!.Execute(null);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(MainWindowViewModel.PageCookieSettings, vm.CurrentPageKey);
            Assert.Same(vm.Login, vm.SettingViewModel!.Login);
            var field = window.GetVisualDescendants().OfType<Control>().Single(c =>
                c.Name == (Equals(other.Value, SearchSourceEnum.QQ_MUSIC) ? "QqCookieField" : "NetEaseCookieField"));
            Assert.True(field.IsEffectivelyVisible);
            Assert.True(field.IsKeyboardFocusWithin);
            Assert.Null(vm.SettingViewModel.PendingCookieFocus);
        }
        finally { window.Close(); WeakReferenceMessenger.Default.UnregisterAll(vm); }
    }
}
