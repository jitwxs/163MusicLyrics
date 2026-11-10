using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;
using MusicLyricApp.Views;

namespace MusicLyricAppTest.Regression;

/// <summary>
/// The one way code follows a UI language switch: <see cref="LocalizationService.Observe{T}"/> (weak, ordered in
/// phases) or <see cref="LocalizedObject"/> for options and items.
/// </summary>
public class LanguageSwitchRegressionTests
{
    private static void WithLanguage(string language, Action action)
    {
        var original = LocalizationService.Instance.Language;
        var culture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            LocalizationService.Instance.SetLanguage(language);
            action();
        }
        finally
        {
            LocalizationService.Instance.SetLanguage(original);
            System.Globalization.CultureInfo.CurrentUICulture = culture;
        }
    }

    private sealed class Probe
    {
        public List<string> Calls { get; } = [];

        public void Refresh(Probe _) => Calls.Add("bound");
    }

    [AvaloniaFact]
    public void ContentObserversRunBeforeViewObserversWhateverTheOrderTheySubscribedIn() => WithLanguage("zh-CN", () =>
    {
        var probe = new Probe();
        // The view subscribes first, like the window does before the settings page exists.
        LocalizationService.Observe(probe, static p => p.Calls.Add("view"), LanguageChangePhase.View);
        LocalizationService.Observe(probe, static p => p.Calls.Add("content"));

        LocalizationService.Instance.SetLanguage("en-US");

        // Once each, content first.
        Assert.Equal(["content", "view"], probe.Calls);
        GC.KeepAlive(probe);
    });

    [AvaloniaFact]
    public void ObserversDoNotKeepTheirOwnerAlive() => WithLanguage("zh-CN", () =>
    {
        var calls = new StrongBox<int>();
        var owner = Subscribe(calls);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        LocalizationService.Instance.SetLanguage("en-US");
        Assert.False(owner.TryGetTarget(out _));
        Assert.Equal(0, calls.Value);
    });

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<object> Subscribe(StrongBox<int> calls)
    {
        var owner = new Tuple<StrongBox<int>>(calls);
        LocalizationService.Observe(owner, static o => o.Item1.Value++);
        return new WeakReference<object>(owner);
    }

    [Fact]
    public void ADelegateBoundToTheOwnerIsRejected()
    {
        var probe = new Probe();
        Assert.Throws<ArgumentException>(() => LocalizationService.Observe(probe, probe.Refresh));
    }

    [AvaloniaFact]
    public void LocalizedObjectBindingsFollowTheLanguage() => WithLanguage("zh-CN", () =>
    {
        var item = EnumDisplayHelper.GetEnumDisplayCollection<DotTypeEnum>().First();
        var text = new TextBlock { DataContext = item };
        text.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding(nameof(item.DisplayName)));
        var window = new Window { Content = text };
        try
        {
            window.Show();
            Assert.Equal("截位", text.Text);
            LocalizationService.Instance.SetLanguage("en-US");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Truncate", text.Text);
        }
        finally { window.Close(); }
    });

    [AvaloniaFact]
    public void DownloadManagerRowsFollowTheLanguage() => WithLanguage("zh-CN", () =>
    {
        var row = new BatchSearchItemViewModel { SourceEnum = SearchSourceEnum.QQ_MUSIC, State = BatchItemStateEnum.SAVED };
        var changed = new List<string?>();
        row.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        Assert.Equal(("QQ音乐", "保存成功"), (row.SongSource, row.Status));

        LocalizationService.Instance.SetLanguage("en-US");
        Assert.Equal(("QQ Music", LocalizationService.Get("Status.Batch.Saved")), (row.SongSource, row.Status));
        Assert.NotEqual("保存成功", row.Status);
        Assert.Contains(string.Empty, changed);
    });

    /// <summary>Direct subscriptions have no order and keep their subscriber alive, so only the service uses them.</summary>
    [Fact]
    public void NothingSubscribesToTheLocalizationServiceDirectly()
    {
        var appRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(SourceFile())!, "..", "..", "MusicLyricApp"));
        Assert.True(Directory.Exists(appRoot), appRoot);
        var offenders = Directory.EnumerateFiles(appRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f => System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(f), @"LocalizationService\.Instance\.PropertyChanged\s*[+-]="))
            .Select(f => Path.GetRelativePath(appRoot, f));
        Assert.Empty(offenders);
    }

    private static string SourceFile([CallerFilePath] string path = "") => path;

    [AvaloniaFact]
    public void SelectsInSectionsNotShownYetFollowALanguageSwitch() => WithLanguage("en-US", () =>
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.ConfirmBeforeExit = false;
        ws.Settings.Config.Language = "en-US";
        ws.Storage().SaveConfig(ws.Settings);
        var window = new MainWindow(ws.Storage(), ws.Search(new(Moq.MockBehavior.Strict)));
        var vm = Assert.IsType<MainWindowViewModel>(window.DataContext);
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            // Open settings straight on Application: the Lyrics section exists but has never been laid out, so its
            // selects are not in the visual tree yet when the language changes.
            vm.CurrentPageKey = MainWindowViewModel.SettingsKeyPrefix + SettingViewModel.SectionApplication;
            Dispatcher.UIThread.RunJobs();
            var settings = vm.SettingViewModel!.SettingParamViewModel;
            settings.SelectedLanguage = settings.Languages.Single(l => l.Code == "zh-CN");
            Dispatcher.UIThread.RunJobs();

            vm.CurrentPageKey = MainWindowViewModel.SettingsKeyPrefix + SettingViewModel.SectionLyrics;
            Dispatcher.UIThread.RunJobs();
            var view = window.FindControl<SettingView>("SettingsPage")!;
            var selects = view.FindControl<StackPanel>("LyricsSection")!.GetVisualDescendants().OfType<TKCode.Arc.Select>().ToList();
            Assert.NotEmpty(selects);
            Assert.All(selects.Where(s => s.SelectedItem != null),
                select => Assert.Equal(select.SelectedItem!.ToString(), select.SelectedText));
        }
        finally { window.Close(); CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.UnregisterAll(vm); }
    });
}
