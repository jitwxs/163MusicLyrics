using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Service.Login;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;
using TKCode.Arc;

namespace MusicLyricApp.ViewModels;

/// <summary>The sign-in state of one platform, worded for the settings page and the online lyrics page.</summary>
public partial class AccountStatusViewModel(SearchSourceEnum source) : ViewModelBase
{
    public SearchSourceEnum Source { get; } = source;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Label), nameof(SourceLabel), nameof(Detail), nameof(Tone), nameof(Icon),
        nameof(IsChecking), nameof(NeedsAttention), nameof(IsValid), nameof(IsInvalid), nameof(IsUncertain))]
    private LoginState _state = LoginState.NotConfigured;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Label), nameof(SourceLabel), nameof(Detail))]
    private string _nickname = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Detail))]
    private string _error = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Detail))]
    private DateTime? _checkedAt;

    public bool IsChecking => State == LoginState.Checking;
    public bool IsValid => State == LoginState.Valid;
    public bool IsInvalid => State == LoginState.Invalid;
    /// <summary>Malformed cookie or a check that could not finish: worth a look, not necessarily broken.</summary>
    public bool IsUncertain => State is LoginState.Malformed or LoginState.Unknown;

    /// <summary>A cookie was filled in but does not work, so the user should look at it.</summary>
    public bool NeedsAttention => State is LoginState.Malformed or LoginState.Invalid;

    /// <summary>Short state, e.g. "已登录 · 昵称" or "已失效".</summary>
    public string Label => State == LoginState.Valid && Nickname.Length > 0
        ? LocalizationService.Format("Login.State.ValidAs", Nickname)
        : LocalizationService.Get("Login.State." + State);

    /// <summary>The state with the platform name, for the online lyrics page, e.g. "网易云：已登录 · 昵称".</summary>
    public string SourceLabel => LocalizationService.Format("Login.SourceState", Source.ToDescription(), Label);

    /// <summary>What the state means for the user and what to do about it.</summary>
    public string Detail
    {
        get
        {
            var key = State switch
            {
                LoginState.Malformed => Source == SearchSourceEnum.NET_EASE_MUSIC
                    ? "Login.Detail.MalformedNetEase" : "Login.Detail.MalformedQq",
                _ => "Login.Detail." + State
            };
            var detail = LocalizationService.Format(key, Error);
            return CheckedAt is { } at && State is LoginState.Valid or LoginState.Invalid or LoginState.Unknown
                ? detail + LocalizationService.Format("Login.CheckedAt", at.ToString("HH:mm"))
                : detail;
        }
    }

    public BadgeTone Tone => State switch
    {
        LoginState.Valid => BadgeTone.Success,
        LoginState.Invalid => BadgeTone.Danger,
        LoginState.Malformed or LoginState.Unknown => BadgeTone.Warning,
        LoginState.Checking => BadgeTone.Info,
        _ => BadgeTone.Neutral
    };

    public Geometry Icon => State switch
    {
        LoginState.Valid => ArcIcons.CircleCheck,
        LoginState.Invalid => ArcIcons.CircleX,
        LoginState.Malformed or LoginState.Unknown => ArcIcons.CircleAlert,
        LoginState.Checking => ArcIcons.LoaderCircle,
        _ => ArcIcons.User
    };

    public void Apply(LoginProbeResult result)
    {
        Nickname = result.Nickname;
        Error = result.Detail;
        CheckedAt = result.State is LoginState.Valid or LoginState.Invalid or LoginState.Unknown ? DateTime.Now : null;
        State = result.State;
    }

    public void RefreshLanguage()
    {
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(SourceLabel));
        OnPropertyChanged(nameof(Detail));
    }
}

/// <summary>
/// One place that knows whether each platform's cookie is signed in. The settings page checks a cookie shortly
/// after it is edited; the online lyrics page shows the result; a "needs login" error triggers a re-check.
/// </summary>
public partial class LoginStatusViewModel : ViewModelBase, IDisposable
{
    private readonly Func<SearchSourceEnum, string> _cookieOf;
    private readonly ILoginProbe _probe;
    private readonly TimeSpan _debounce;
    private readonly CancellationTokenSource?[] _pending = new CancellationTokenSource?[2];
    private readonly int[] _versions = new int[2];

    public AccountStatusViewModel NetEase { get; } = new(SearchSourceEnum.NET_EASE_MUSIC);
    public AccountStatusViewModel QQ { get; } = new(SearchSourceEnum.QQ_MUSIC);

    public LoginStatusViewModel(Func<SearchSourceEnum, string> cookieOf, ILoginProbe? probe = null, TimeSpan? debounce = null)
    {
        _cookieOf = cookieOf;
        _probe = probe ?? new NativeLoginProbe();
        _debounce = debounce ?? TimeSpan.FromMilliseconds(800);
        foreach (var account in new[] { NetEase, QQ }) ApplyLocal(account);
        LocalizationService.Observe(this, static vm => vm.OnLanguageChanged());
    }

    /// <summary>Only NetEase and QQ Music take a cookie; Soda Music is used signed out.</summary>
    public static bool HasAccount(SearchSourceEnum source) =>
        source is SearchSourceEnum.NET_EASE_MUSIC or SearchSourceEnum.QQ_MUSIC;

    public AccountStatusViewModel Get(SearchSourceEnum source) => source == SearchSourceEnum.NET_EASE_MUSIC ? NetEase : QQ;

    private static int Index(SearchSourceEnum source) => source == SearchSourceEnum.NET_EASE_MUSIC ? 0 : 1;

    private void OnLanguageChanged()
    {
        NetEase.RefreshLanguage();
        QQ.RefreshLanguage();
    }

    /// <summary>Sets the states that need no request: empty or missing the sign-in token.</summary>
    private bool ApplyLocal(AccountStatusViewModel account)
    {
        var cookie = CookieInspector.Normalize(_cookieOf(account.Source));
        if (cookie.Length == 0) { account.Apply(new(LoginState.NotConfigured)); return true; }
        if (!CookieInspector.HasLoginToken(account.Source, cookie)) { account.Apply(new(LoginState.Malformed)); return true; }
        return false;
    }

    /// <summary>Checks both platforms now; used at startup. Empty cookies cost no request.</summary>
    public Task CheckAllAsync() => Task.WhenAll(CheckAsync(SearchSourceEnum.NET_EASE_MUSIC), CheckAsync(SearchSourceEnum.QQ_MUSIC));

    [RelayCommand]
    private Task Check(SearchSourceEnum source) => CheckAsync(source);

    public async Task CheckAsync(SearchSourceEnum source)
    {
        if (!HasAccount(source)) return;
        var index = Index(source);
        _pending[index]?.Cancel();
        _pending[index] = null;
        var version = Interlocked.Increment(ref _versions[index]);
        var account = Get(source);
        if (ApplyLocal(account)) return;

        account.State = LoginState.Checking;
        var cookie = _cookieOf(source);
        LoginProbeResult result;
        try { result = await Task.Run(() => _probe.Probe(source, cookie)); }
        catch (Exception ex) { result = new(LoginState.Unknown, Detail: ex.Message); }

        // A newer edit or check started meanwhile: its result wins.
        if (version == Volatile.Read(ref _versions[index])) account.Apply(result);
    }

    /// <summary>Checks after the user stops typing, so each keystroke does not send a request.</summary>
    public void ScheduleCheck(SearchSourceEnum source)
    {
        if (!HasAccount(source)) return;
        var index = Index(source);
        _pending[index]?.Cancel();
        Interlocked.Increment(ref _versions[index]);
        var account = Get(source);
        if (ApplyLocal(account)) return;

        account.State = LoginState.Checking;
        var cancellation = new CancellationTokenSource();
        _pending[index] = cancellation;
        _ = Task.Delay(_debounce, cancellation.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled) _ = CheckAsync(source);
        }, CancellationToken.None, TaskContinuationOptions.None, SynchronizationContext.Current != null
            ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Default);
    }

    /// <summary>
    /// A request failed with "needs login". With a cookie filled in, that usually means it expired, so check it
    /// again and let the pages show the new state.
    /// </summary>
    public void ReportNeedLogin(SearchSourceEnum source)
    {
        if (!HasAccount(source)) return;
        var account = Get(source);
        if (account.State is LoginState.Valid or LoginState.Unknown) _ = CheckAsync(source);
    }

    public void Dispose()
    {
        foreach (var pending in _pending) pending?.Cancel();
    }
}
