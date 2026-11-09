using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Avalonia.Markup.Xaml;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace MusicLyricApp.Core.Localization;

/// <summary>Runtime localization for UI resources and user-facing messages.</summary>
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
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
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
