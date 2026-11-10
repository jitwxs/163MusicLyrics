using System;
using System.Collections.ObjectModel;
using System.Linq;
using MusicLyricApp.Core.Localization;

namespace MusicLyricApp.Core.Utils;

public static class EnumDisplayHelper
{
    /// <summary>An enum value shown in a Select; its text follows the UI language.</summary>
    public class EnumDisplayItem<T> : LocalizedObject where T : struct, Enum
    {
        public T Value { get; init; }

        public string Description => LocalizationService.Get($"Enum.{typeof(T).Name}.{Value}");

        public string DisplayName => Description;

        public override string ToString() => Description;
    }

    public static ObservableCollection<EnumDisplayItem<T>> GetEnumDisplayCollection<T>() where T : struct, Enum =>
        new(Enum.GetValues<T>().Select(e => new EnumDisplayItem<T> { Value = e }));
}
