using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using MusicLyricApp.Core.Utils;

namespace MusicLyricApp.ViewModels;

/// <summary>
/// One enum setting picked from a Select (bind <see cref="Items"/> and <see cref="Selected"/>) or a segmented
/// control (bind <see cref="SelectedValue"/>). Each change is passed to the owner's <c>apply</c>, which writes the
/// setting and raises whatever the owner's listeners expect.
/// </summary>
public sealed partial class EnumChoice<T> : ObservableObject where T : struct, Enum
{
    private readonly Action<T> _apply;

    public EnumChoice(Action<T> apply)
    {
        _apply = apply;
    }

    /// <summary>Every value of the enum, labelled in the UI language.</summary>
    public ObservableCollection<EnumDisplayHelper.EnumDisplayItem<T>> Items { get; } =
        EnumDisplayHelper.GetEnumDisplayCollection<T>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Value), nameof(SelectedValue))]
    private EnumDisplayHelper.EnumDisplayItem<T>? _selected;

    /// <summary>The chosen value; setting it selects the matching option.</summary>
    public T Value
    {
        get => Selected?.Value ?? default;
        set
        {
            if (Selected == null || !Equals(Selected.Value, value))
            {
                Selected = Items.First(item => Equals(item.Value, value));
            }
        }
    }

    /// <summary><see cref="Value"/> as an object, for segmented controls.</summary>
    public object? SelectedValue
    {
        get => Selected?.Value;
        set
        {
            if (value is T typed) Value = typed;
        }
    }

    partial void OnSelectedChanged(EnumDisplayHelper.EnumDisplayItem<T>? value)
    {
        if (value != null) _apply(value.Value);
    }
}
