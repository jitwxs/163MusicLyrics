using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Service;
using MusicLyricApp.ViewModels;
using TKCode.Arc;

namespace MusicLyricApp.Views;

/// <summary>Online lyrics page (在线歌词): query, song details, playback, preview and save.</summary>
public partial class SearchView : UserControl
{
    private MainWindowViewModel? _viewModel;

    public SearchView()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<TextBox>("SearchTextBox")!.PointerEntered += SearchTextBox_PointerEntered;
        this.FindControl<BlurSearchView>("BlurResultsView")!.BackRequested += (_, _) =>
            _viewModel?.ExecuteCloseBlurResultsCommand.Execute(null);

        ApplyShortcutTexts();
        LocalizationService.Observe(this, static view => view.ApplyShortcutTexts(), LanguageChangePhase.View);
    }

    /// <summary>Search page shortcuts only apply while focus is inside the page.</summary>
    private void BindShortcuts(MainWindowViewModel vm)
    {
        KeyBindings.Clear();
        void Bind(string id, System.Windows.Input.ICommand command)
        {
            foreach (var gesture in AppShortcuts.Get(id).AllGestures)
            {
                KeyBindings.Add(new KeyBinding { Gesture = gesture, Command = command });
            }
        }

        Bind(AppShortcuts.ExactSearch, vm.ExecuteExactSearchWithModeCommand);
        Bind(AppShortcuts.FuzzySearch, vm.ExecuteBlurSearchWithModeCommand);
        Bind(AppShortcuts.Save, vm.ExecuteSaveCommand);
    }

    /// <summary>Hints that name keys use the same platform-aware text as the shortcuts dialog.</summary>
    private void ApplyShortcutTexts()
    {
        var exact = AppShortcuts.Get(AppShortcuts.ExactSearch);
        var fuzzy = AppShortcuts.Get(AppShortcuts.FuzzySearch);
        var save = AppShortcuts.Get(AppShortcuts.Save);
        string Tip(string label, AppShortcut s) => LocalizationService.Format("Shortcuts.TooltipFormat", label, s.KeysText);

        this.FindControl<EmptyState>("SongEmptyState")!.Description =
            LocalizationService.Format("Search.EmptyDescription", exact.KeysText, fuzzy.KeysText);
        ToolTip.SetTip(this.FindControl<Control>("SearchButton")!, Tip(exact.Label, exact));
        ToolTip.SetTip(this.FindControl<Control>("FuzzySwitch")!, Tip(fuzzy.Label, fuzzy));
        ToolTip.SetTip(this.FindControl<Control>("SaveButton")!, Tip(save.Label, save));
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel != null)
        {
            _viewModel.SearchResultViewModel.PropertyChanged -= SearchResultOnPropertyChanged;
        }

        _viewModel = DataContext as MainWindowViewModel;

        if (_viewModel != null)
        {
            _viewModel.SearchResultViewModel.PropertyChanged += SearchResultOnPropertyChanged;
            BindShortcuts(_viewModel);
        }
    }

    private async void SearchResultOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel != null &&
            e.PropertyName == nameof(SearchResultViewModel.SongName) &&
            _viewModel.SearchResultViewModel.SaveVoMap.Count > 0)
        {
            await UpdateAlbumCover(_viewModel);
        }
    }

    private async Task UpdateAlbumCover(MainWindowViewModel viewModel)
    {
        var image = this.FindControl<Image>("AlbumCoverImage")!;
        var saveVo = viewModel.SearchResultViewModel.SaveVoMap.Values.First();
        if (string.IsNullOrWhiteSpace(saveVo.SongVo.Pics))
        {
            image.Source = null;
            return;
        }

        try
        {
            using var client = NetworkClientFactory.CreateHttpClient();
            var imageData = await client.GetByteArrayAsync(saveVo.SongVo.Pics);
            using var stream = new MemoryStream(imageData);
            image.Source = new Bitmap(stream);
        }
        catch (Exception)
        {
            image.Source = null;
        }
    }

    private async void SearchTextBox_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (_viewModel is not { SettingBean.Config.AutoReadClipboard: true })
        {
            return;
        }

        if (TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)
        {
            return;
        }

        var message = await clipboard.TryGetTextAsync();
        if (message != null)
        {
            _viewModel.SearchParamViewModel.SearchText = message;
        }
    }
}
