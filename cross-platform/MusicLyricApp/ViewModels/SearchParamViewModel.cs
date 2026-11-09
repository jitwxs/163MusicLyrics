using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;

namespace MusicLyricApp.ViewModels;

public partial class SearchParamViewModel : ViewModelBase
{
    // 1. 音乐提供商
    public ObservableCollection<EnumDisplayHelper.EnumDisplayItem<SearchSourceEnum>> SearchSources { get; } =
        EnumDisplayHelper.GetEnumDisplayCollection<SearchSourceEnum>();

    [ObservableProperty] private EnumDisplayHelper.EnumDisplayItem<SearchSourceEnum>? _selectedSearchSourceItem;

    public SearchSourceEnum SelectedSearchSource => SelectedSearchSourceItem?.Value ?? default;

    partial void OnSelectedSearchSourceItemChanged(EnumDisplayHelper.EnumDisplayItem<SearchSourceEnum>? value)
    {
        if (value != null) _paramBean.SearchSource = value.Value;
        OnPropertyChanged(nameof(SearchSourceValue));
    }

    /// <summary>Enum view of <see cref="SelectedSearchSourceItem"/> for segmented controls.</summary>
    public object? SearchSourceValue
    {
        get => SelectedSearchSourceItem?.Value;
        set
        {
            if (value is SearchSourceEnum source && !Equals(SelectedSearchSourceItem?.Value, source))
            {
                SelectedSearchSourceItem = SearchSources.First(item => item.Value == source);
            }
        }
    }

    // 2. 歌词格式
    public ObservableCollection<EnumDisplayHelper.EnumDisplayItem<ShowLrcTypeEnum>> LrcTypes { get; } =
        EnumDisplayHelper.GetEnumDisplayCollection<ShowLrcTypeEnum>();

    [ObservableProperty] private EnumDisplayHelper.EnumDisplayItem<ShowLrcTypeEnum>? _selectedLrcTypeItem;

    public ShowLrcTypeEnum SelectedLrcType => SelectedLrcTypeItem?.Value ?? default;

    partial void OnSelectedLrcTypeItemChanged(EnumDisplayHelper.EnumDisplayItem<ShowLrcTypeEnum>? value)
    {
        if (value != null) _paramBean.ShowLrcType = value.Value;
    }

    // 3. 搜索类型
    public ObservableCollection<EnumDisplayHelper.EnumDisplayItem<SearchTypeEnum>> SearchTypes { get; } =
        EnumDisplayHelper.GetEnumDisplayCollection<SearchTypeEnum>();

    [ObservableProperty] private EnumDisplayHelper.EnumDisplayItem<SearchTypeEnum>? _selectedSearchTypeItem;

    public SearchTypeEnum SelectedSearchType => SelectedSearchTypeItem?.Value ?? default;

    partial void OnSelectedSearchTypeItemChanged(EnumDisplayHelper.EnumDisplayItem<SearchTypeEnum>? value)
    {
        if (value != null) _paramBean.SearchType = value.Value;
        OnPropertyChanged(nameof(SearchTypeValue));
    }

    /// <summary>Enum view of <see cref="SelectedSearchTypeItem"/> for segmented controls.</summary>
    public object? SearchTypeValue
    {
        get => SelectedSearchTypeItem?.Value;
        set
        {
            if (value is SearchTypeEnum type && !Equals(SelectedSearchTypeItem?.Value, type))
            {
                SelectedSearchTypeItem = SearchTypes.First(item => item.Value == type);
            }
        }
    }

    // 4. 输出格式
    public ObservableCollection<EnumDisplayHelper.EnumDisplayItem<OutputFormatEnum>> OutputFormats { get; } =
        EnumDisplayHelper.GetEnumDisplayCollection<OutputFormatEnum>();

    [ObservableProperty] private EnumDisplayHelper.EnumDisplayItem<OutputFormatEnum>? _selectedOutputFormatItem;

    public OutputFormatEnum SelectedOutputFormat => SelectedOutputFormatItem?.Value ?? default;

    partial void OnSelectedOutputFormatItemChanged(EnumDisplayHelper.EnumDisplayItem<OutputFormatEnum>? value)
    {
        if (value != null) _paramBean.OutputFileFormat = value.Value;
    }

    // 5. 输出编码
    public ObservableCollection<EnumDisplayHelper.EnumDisplayItem<OutputEncodingEnum>> OutputEncodings { get; } =
        EnumDisplayHelper.GetEnumDisplayCollection<OutputEncodingEnum>();

    [ObservableProperty] private EnumDisplayHelper.EnumDisplayItem<OutputEncodingEnum>? _selectedOutputEncodingItem;

    public OutputEncodingEnum SelectedOutputEncoding => SelectedOutputEncodingItem?.Value ?? default;

    partial void OnSelectedOutputEncodingItemChanged(EnumDisplayHelper.EnumDisplayItem<OutputEncodingEnum>? value)
    {
        if (value != null) _paramBean.Encoding = value.Value;
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

        SelectedSearchSourceItem = SearchSources.First(item => Equals(item.Value, _paramBean.SearchSource));
        SelectedLrcTypeItem = LrcTypes.First(item => Equals(item.Value, _paramBean.ShowLrcType));
        SelectedSearchTypeItem = SearchTypes.First(item => Equals(item.Value, _paramBean.SearchType));
        SelectedOutputFormatItem = OutputFormats.First(item => Equals(item.Value, _paramBean.OutputFileFormat));
        SelectedOutputEncodingItem = OutputEncodings.First(item => Equals(item.Value, _paramBean.Encoding));
        LrcMergeSeparator = _paramBean.LrcMergeSeparator;
    }
}