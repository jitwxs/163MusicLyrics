using System.Collections.Generic;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.ViewModels.Messages;

namespace MusicLyricApp.Views;

public class BatchSearchWindow : Window
{
    private readonly BatchSearchViewModel _viewModel;

    public BatchSearchWindow(BatchSearchViewModel viewModel)
    {
        LocalizationService.BindWindowTitle(this, "Window.BatchSearch.Title");
        Width = 1100;
        Height = 720;

        _viewModel = viewModel;
        DataContext = _viewModel;
        Content = new BatchSearchView();
        Icon = Constants.GetIcon("search-result");

        WeakReferenceMessenger.Default.Register<CloseWindowMessage>(this, (r, m) =>
        {
            if (m.Value == "BatchSearchWindow")
            {
                Close();
            }
        });
        Closed += (_, _) => WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    public void AddResults(Dictionary<string, ResultVo<SaveVo>> resDict, List<InputSongId> inputSongIds, string? inputText = null)
    {
        _viewModel.AddSearchResults(resDict, inputSongIds, inputText);
    }
}
