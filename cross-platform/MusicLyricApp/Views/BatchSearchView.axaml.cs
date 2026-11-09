using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.ViewModels;
using TKCode.Arc;
using TKCode.Arc.DesignTokens;

namespace MusicLyricApp.Views;

/// <summary>
/// Download manager page. The Arc table owns selection; each row's <see cref="BatchSearchItemViewModel.IsSelected"/>
/// mirrors it so the view model's bulk commands keep working on "the selected rows".
/// </summary>
public partial class BatchSearchView : UserControl
{
    private BatchSearchViewModel? _viewModel;
    private bool _syncingSelection;

    public BatchSearchView()
    {
        AvaloniaXamlLoader.Load(this);
        Table.SelectionChanged += OnTableSelectionChanged;
        LocalizationService.Instance.PropertyChanged += (_, _) => RebuildColumns();
        RebuildColumns();
    }

    public Table Table => this.FindControl<Table>("BatchTable")!;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel != null)
        {
            _viewModel.Items.CollectionChanged -= OnItemsChanged;
            foreach (var item in _viewModel.Items) item.PropertyChanged -= OnItemPropertyChanged;
        }

        _viewModel = DataContext as BatchSearchViewModel;

        if (_viewModel != null)
        {
            // Deleting also clears the songs' local cache, so ask first, anchored to this page's window.
            _viewModel.ConfirmDeleteAsync = count => DialogHelper.ConfirmOnAsync(this,
                LocalizationService.Get("Dialog.DeleteSongs.Title"),
                LocalizationService.Format("Dialog.DeleteSongs.Message", count),
                LocalizationService.Get("Dialog.DeleteSongs.Action"),
                isDanger: true);
            _viewModel.Items.CollectionChanged += OnItemsChanged;
            foreach (var item in _viewModel.Items) item.PropertyChanged += OnItemPropertyChanged;
        }

        RebuildColumns();
        PushSelectionToTable();
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (BatchSearchItemViewModel item in e.OldItems) item.PropertyChanged -= OnItemPropertyChanged;
        }

        if (e.NewItems != null)
        {
            foreach (BatchSearchItemViewModel item in e.NewItems) item.PropertyChanged += OnItemPropertyChanged;
        }

        PushSelectionToTable();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BatchSearchItemViewModel.IsSelected))
        {
            PushSelectionToTable();
        }
    }

    /// <summary>View model to table: commands such as "select songs by id" flip IsSelected on rows.</summary>
    private void PushSelectionToTable()
    {
        if (_syncingSelection || _viewModel == null)
        {
            return;
        }

        var selected = _viewModel.Items.Where(i => i.IsSelected).Cast<object>().ToList();
        var current = Table.SelectedItems?.Cast<object>().ToList() ?? [];
        if (selected.SequenceEqual(current))
        {
            return;
        }

        _syncingSelection = true;
        try
        {
            Table.SelectedItems = selected;
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    /// <summary>Table to view model: the user ticked rows, the header checkbox, or cleared the selection.</summary>
    private void OnTableSelectionChanged(object? sender, DataTableSelectionChangedEventArgs e)
    {
        if (_syncingSelection || _viewModel == null)
        {
            return;
        }

        _syncingSelection = true;
        try
        {
            var selected = e.SelectedItems.ToHashSet();
            foreach (var item in _viewModel.Items)
            {
                item.IsSelected = selected.Contains(item);
            }
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private void RebuildColumns()
    {
        Table.Columns.Clear();
        foreach (var column in CreateColumns())
        {
            Table.Columns.Add(column);
        }
    }

    private IEnumerable<DataTableColumn> CreateColumns()
    {
        static DataTableColumn Text(string key, Func<BatchSearchItemViewModel, object?> value, GridLength width) =>
            new(LocalizationService.Get(key), item => item is BatchSearchItemViewModel row ? value(row) : null)
            {
                Width = width
            };

        yield return Text("UI.SongSource", x => x.SongSource, new GridLength(96));
        yield return Text("UI.SongId", x => x.SongId, new GridLength(104));
        yield return Text("UI.SongName", x => x.SongName, new GridLength(3, GridUnitType.Star));
        yield return Text("UI.BlurGrid.Artist", x => x.Singer, new GridLength(2, GridUnitType.Star));
        yield return Text("UI.BlurGrid.Album", x => x.Album, new GridLength(2, GridUnitType.Star));

        // Status and its reason share one column so the name, artist and album keep readable widths.
        yield return new DataTableColumn(LocalizationService.Get("UI.Extra01"), item => (item as BatchSearchItemViewModel)?.Status)
        {
            Width = new GridLength(2, GridUnitType.Star),
            CellTemplate = new FuncDataTemplate<BatchSearchItemViewModel>((_, _) => BuildStatusCell())
        };

        yield return new DataTableColumn(LocalizationService.Get("Batch.Actions"), _ => null)
        {
            Width = new GridLength(104),
            IsSortable = false,
            CellTemplate = new FuncDataTemplate<BatchSearchItemViewModel>((_, _) => BuildActionsCell())
        };
    }

    /// <summary>One badge per state; tone and words together, so status never relies on colour alone.</summary>
    private static Control BuildStatusCell()
    {
        static Badge Badge(BadgeTone tone, string visibleWhen) => new()
        {
            Size = BadgeSize.Small,
            Tone = tone,
            VerticalAlignment = VerticalAlignment.Center,
            [!ContentControl.ContentProperty] = new Binding(nameof(BatchSearchItemViewModel.Status)),
            [!IsVisibleProperty] = new Binding(visibleWhen)
        };

        var badges = new Panel { HorizontalAlignment = HorizontalAlignment.Left };
        badges.Children.Add(Badge(BadgeTone.Info, nameof(BatchSearchItemViewModel.IsReady)));
        badges.Children.Add(Badge(BadgeTone.Danger, nameof(BatchSearchItemViewModel.IsFailed)));
        badges.Children.Add(Badge(BadgeTone.Success, nameof(BatchSearchItemViewModel.IsSaved)));
        badges.Children.Add(Badge(BadgeTone.Warning, nameof(BatchSearchItemViewModel.IsSkipped)));

        var cell = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { badges, BuildErrorText() } };
        ArcTokens.Bind(cell, StackPanel.SpacingProperty, ArcDesignTokenKind.Space1);
        return cell;
    }

    /// <summary>Why a row failed or was skipped, under its badge; hidden when there is nothing to say.</summary>
    private static Control BuildErrorText()
    {
        var text = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 2,
            TextTrimming = TextTrimming.CharacterEllipsis,
            [!TextBlock.TextProperty] = new Binding(nameof(BatchSearchItemViewModel.Error)),
            [!ToolTip.TipProperty] = new Binding(nameof(BatchSearchItemViewModel.Error)),
            [!IsVisibleProperty] = new Binding(nameof(BatchSearchItemViewModel.HasError))
        };
        ArcTokens.Bind(text, TextBlock.ForegroundProperty, ArcDesignTokenKind.Danger);
        ArcTokens.Bind(text, TextBlock.FontSizeProperty, ArcDesignTokenKind.TypeSM);
        return text;
    }

    /// <summary>Per-row shortcuts that the old right-click menu offered.</summary>
    private Control BuildActionsCell()
    {
        TKCode.Arc.Button Action(Geometry icon, string labelKey, string commandName)
        {
            var label = LocalizationService.Get(labelKey);
            var button = new TKCode.Arc.Button
            {
                Variant = ButtonVariant.Ghost,
                Size = ControlSize.Small,
                Content = new ArcGlyph { Data = icon },
                [!Avalonia.Controls.Button.CommandParameterProperty] = new Binding(".")
            };
            ToolTip.SetTip(button, label);
            Avalonia.Automation.AutomationProperties.SetName(button, label);
            button[!Avalonia.Controls.Button.CommandProperty] = new Binding($"DataContext.{commandName}")
            {
                Source = this
            };
            return button;
        }

        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                Action(ArcIcons.Download, "Batch.SaveRow", nameof(BatchSearchViewModel.ExecuteContextSaveCommand)),
                Action(ArcIcons.Eye, "UI.ViewDetails", nameof(BatchSearchViewModel.ExecuteViewDetailCommand))
            }
        };
        ArcTokens.Bind(panel, StackPanel.SpacingProperty, ArcDesignTokenKind.Space1);
        return panel;
    }
}
