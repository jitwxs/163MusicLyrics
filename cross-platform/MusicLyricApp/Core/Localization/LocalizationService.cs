using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Globalization;
using System.Reflection;
using Avalonia.Markup.Xaml;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace MusicLyricApp.Core.Localization;

/// <summary>When an observer runs during a language switch.</summary>
public enum LanguageChangePhase
{
    /// <summary>Text held by view models and option objects. Runs first.</summary>
    Content,

    /// <summary>Controls that read that text and cache it (Select labels, table columns). Runs after every
    /// <see cref="Content"/> observer, so it always sees the new text.</summary>
    View,
}

/// <summary>Runtime localization for UI resources and user-facing messages.</summary>
/// <remarks>
/// Code that shows localized text it computed itself follows a switch in one of two ways:
/// <list type="bullet">
/// <item>An option or list item derives from <see cref="LocalizedObject"/>; its bindings refresh by themselves.</item>
/// <item>Anything else calls <see cref="Observe{T}"/>. It is held weakly, so a view model that is thrown away (the
/// settings page builds a new one on each visit) stops being called and is not kept alive.</item>
/// </list>
/// A switch runs all <see cref="LanguageChangePhase.Content"/> observers, then all
/// <see cref="LanguageChangePhase.View"/> observers, then updates <c>{loc:Loc}</c> bindings, each exactly once.
/// Do not subscribe to <see cref="PropertyChanged"/> directly: it has no order and keeps subscribers alive.
/// </remarks>
public sealed class LocalizationService : INotifyPropertyChanged
{
    private static readonly IReadOnlyDictionary<string, string> English = LoadResource("en-US");

    private static readonly IReadOnlyDictionary<string, string> Chinese = LoadResource("zh-CN");

    private static readonly IReadOnlyDictionary<string, string> TraditionalChinese = LoadResource("zh-TW");


    private static IReadOnlyDictionary<string, string> LoadResource(string culture)
    {
        var resourceName = $"MusicLyricApp.Resources.Localization.{culture}.json";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream == null) return new Dictionary<string, string>();
        return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
               ?? new Dictionary<string, string>();
    }

    public static LocalizationService Instance { get; } = new();
    private string _language = "zh-CN";
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Language => _language;

    private IReadOnlyDictionary<string, string> CurrentResources => _language switch
    {
        "en-US" => English,
        "zh-TW" => TraditionalChinese,
        _ => Chinese
    };

    public string this[string key] => CurrentResources.TryGetValue(key, out var value)
        ? value : Chinese.TryGetValue(key, out var chinese) ? chinese : key;

    public static string NormalizeLanguage(string? language) => language?.ToLowerInvariant() switch
    {
        "en-us" => "en-US",
        "zh-tw" => "zh-TW",
        _ => "zh-CN"
    };

    public void SetLanguage(string? language)
    {
        var normalized = NormalizeLanguage(language);
        if (_language == normalized) return;
        _language = normalized;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(normalized);
        NotifyObservers(LanguageChangePhase.Content);
        NotifyObservers(LanguageChangePhase.View);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
    }

    private sealed record Observer(WeakReference<object> Owner, Action<object> Refresh, LanguageChangePhase Phase);

    private readonly List<Observer> _observers = [];

    /// <summary>
    /// Calls <paramref name="refresh"/> with <paramref name="owner"/> after each language switch, for as long as the
    /// owner is alive. Pass a <c>static</c> lambda that works on its argument (<c>static vm => vm.Refresh()</c>): a
    /// delegate bound to the owner would keep it alive, so it is rejected.
    /// </summary>
    public static void Observe<T>(T owner, Action<T> refresh, LanguageChangePhase phase = LanguageChangePhase.Content)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (ReferenceEquals(refresh.Target, owner))
        {
            throw new ArgumentException("Pass a static lambda that uses its argument; a delegate bound to the owner keeps it alive.",
                nameof(refresh));
        }

        var observers = Instance._observers;
        // Language switches are rare, so dead entries are also dropped as the list grows.
        if (observers.Count > 0 && observers.Count % 256 == 0) observers.RemoveAll(o => !o.Owner.TryGetTarget(out _));
        observers.Add(new Observer(new WeakReference<object>(owner), o => refresh((T)o), phase));
    }

    private void NotifyObservers(LanguageChangePhase phase)
    {
        _observers.RemoveAll(o => !o.Owner.TryGetTarget(out _));
        // A snapshot: a refresh may create objects that observe too, and those already use the new language.
        foreach (var observer in _observers.Where(o => o.Phase == phase).ToList())
        {
            if (observer.Owner.TryGetTarget(out var owner)) observer.Refresh(owner);
        }
    }

    public static string Get(string key) => Instance[key];

    public static string Format(string key, params object[] args) => string.Format(Get(key), args);

}

public sealed class LocExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;
    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new Binding(nameof(LocalizationService.Language))
        {
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay,
            Converter = new LocalizationValueConverter(Key)
        };
}

internal sealed class LocalizationValueConverter(string key) : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        LocalizationService.Get(key);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
