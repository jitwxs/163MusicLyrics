using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using MusicLyricApp.Core.Service;
using MusicLyricApp.ViewModels;

namespace MusicLyricApp.Views;

/// <summary>Lyrics search page: query, song details, playback, preview and save.</summary>
public partial class SearchView : UserControl
{
    private MainWindowViewModel? _viewModel;

    public SearchView()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<TextBox>("SearchTextBox")!.PointerEntered += SearchTextBox_PointerEntered;
        this.FindControl<BlurSearchView>("BlurResultsView")!.BackRequested += (_, _) =>
            _viewModel?.ExecuteCloseBlurResultsCommand.Execute(null);
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
            var client = new WebClient();
            NetworkClientFactory.ConfigureWebClient(client);
            var imageData = await client.DownloadDataTaskAsync(saveVo.SongVo.Pics);
            image.Source = new Bitmap(new MemoryStream(imageData));
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
