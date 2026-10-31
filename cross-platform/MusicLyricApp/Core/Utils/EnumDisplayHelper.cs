using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using MusicLyricApp.Core.Localization;

namespace MusicLyricApp.Core.Utils;

public static class EnumDisplayHelper
{
    public class EnumDisplayItem<T> : INotifyPropertyChanged where T : Enum
    {
        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set
            {
                if (_description == value) return;
                _description = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
            }
        }
        public string DisplayName => Description;
        public T Value { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;

        public override string ToString() => Description;
    }

    public static ObservableCollection<EnumDisplayItem<T>> GetEnumDisplayCollection<T>() where T : Enum
    {
        var items = new ObservableCollection<EnumDisplayItem<T>>(
            Enum.GetValues(typeof(T))
                .Cast<T>()
                .Select(e => new EnumDisplayItem<T>
                {
                    Value = e,
                    Description = GetDescription(e)
                }));
        LocalizationService.Instance.PropertyChanged += (_, _) =>
        {
            foreach (var item in items) item.Description = GetDescription(item.Value);
        };
        return items;
    }

    private static string GetDescription<T>(T enumValue) where T : Enum
    {
        var name = enumValue.ToString();
        return LocalizationService.Get($"Enum.{enumValue.GetType().Name}.{name}");
    }
}
