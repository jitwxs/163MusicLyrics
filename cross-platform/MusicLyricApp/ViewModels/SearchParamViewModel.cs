using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;

namespace MusicLyricApp.ViewModels;

public partial class SearchParamViewModel : ViewModelBase
{
    // 1-5. Choices; each change is written to the saved parameters and raised under the choice's name.
    public EnumChoice<SearchSourceEnum> SearchSource { get; }

    public EnumChoice<ShowLrcTypeEnum> LrcType { get; }

    public EnumChoice<SearchTypeEnum> SearchType { get; }

    public EnumChoice<OutputFormatEnum> OutputFormat { get; }

    public EnumChoice<OutputEncodingEnum> OutputEncoding { get; }

    public SearchParamViewModel()
    {
        SearchSource = new(value => { _paramBean.SearchSource = value; OnPropertyChanged(nameof(SearchSource)); });
        LrcType = new(value => { _paramBean.ShowLrcType = value; OnPropertyChanged(nameof(LrcType)); });
        SearchType = new(value => { _paramBean.SearchType = value; OnPropertyChanged(nameof(SearchType)); });
        OutputFormat = new(value => { _paramBean.OutputFileFormat = value; OnPropertyChanged(nameof(OutputFormat)); });
        OutputEncoding = new(value => { _paramBean.Encoding = value; OnPropertyChanged(nameof(OutputEncoding)); });
    }

    // 6. 歌词合并符
    [ObservableProperty] private string _lrcMergeSeparator = "";

    partial void OnLrcMergeSeparatorChanged(string value)
    {
        _paramBean.LrcMergeSeparator = value ?? string.Empty;
    }

    // 7. 搜索内容
    [ObservableProperty] private string _searchText = "";

    /// <summary>
    /// 实际处理的歌曲 ID 列表
    /// </summary>
    public readonly List<InputSongId> SongIds = [];

    private PersistParamBean _paramBean = new();

    public void Bind(PersistParamBean paramBean)
    {
        _paramBean = paramBean;

        SearchSource.Value = _paramBean.SearchSource;
        LrcType.Value = _paramBean.ShowLrcType;
        SearchType.Value = _paramBean.SearchType;
        OutputFormat.Value = _paramBean.OutputFileFormat;
        OutputEncoding.Value = _paramBean.Encoding;
        LrcMergeSeparator = _paramBean.LrcMergeSeparator;
    }
}