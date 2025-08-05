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
    
    [ObservableProperty]
    private EnumDisplayHelper.EnumDisplayItem<SearchSourceEnum> _selectedSearchSourceItem;

    public SearchSourceEnum SelectedSearchSource => SelectedSearchSourceItem?.Value ?? default;
    
    partial void OnSelectedSearchSourceItemChanged(
        EnumDisplayHelper.EnumDisplayItem<SearchSourceEnum>? oldValue,
        EnumDisplayHelper.EnumDisplayItem<SearchSourceEnum>? newValue)
    {
        if (newValue != null) _paramBean.SearchSource = newValue.Value;
    }
    
    // 2. 歌词格式
    public ObservableCollection<EnumDisplayHelper.EnumDisplayItem<ShowLrcTypeEnum>> LrcTypes { get; } =
        EnumDisplayHelper.GetEnumDisplayCollection<ShowLrcTypeEnum>();
    
    [ObservableProperty]
    private EnumDisplayHelper.EnumDisplayItem<ShowLrcTypeEnum> _selectedLrcTypeItem;

    public ShowLrcTypeEnum SelectedLrcType => SelectedLrcTypeItem?.Value ?? default;
    
    partial void OnSelectedLrcTypeItemChanged(
        EnumDisplayHelper.EnumDisplayItem<ShowLrcTypeEnum>? oldValue,
        EnumDisplayHelper.EnumDisplayItem<ShowLrcTypeEnum>? newValue)
    {
        if (newValue != null) _paramBean.ShowLrcType = newValue.Value;
    }
    
    // 3. 搜索类型
    public ObservableCollection<EnumDisplayHelper.EnumDisplayItem<SearchTypeEnum>> SearchTypes { get; } =
        EnumDisplayHelper.GetEnumDisplayCollection<SearchTypeEnum>();
    
    [ObservableProperty]
    private EnumDisplayHelper.EnumDisplayItem<SearchTypeEnum> _selectedSearchTypeItem;

    public SearchTypeEnum SelectedSearchType => SelectedSearchTypeItem?.Value ?? default;
    
    partial void OnSelectedSearchTypeItemChanged(
        EnumDisplayHelper.EnumDisplayItem<SearchTypeEnum>? oldValue,
        EnumDisplayHelper.EnumDisplayItem<SearchTypeEnum>? newValue)
    {
        if (newValue != null) _paramBean.SearchType = newValue.Value;
    }
    
    // 4. 输出格式
    public ObservableCollection<EnumDisplayHelper.EnumDisplayItem<OutputFormatEnum>> OutputFormats { get; } =
        EnumDisplayHelper.GetEnumDisplayCollection<OutputFormatEnum>();
    
    [ObservableProperty]
    private EnumDisplayHelper.EnumDisplayItem<OutputFormatEnum> _selectedOutputFormatItem;

    public OutputFormatEnum SelectedOutputFormat => SelectedOutputFormatItem?.Value ?? default;
    
    partial void OnSelectedOutputFormatItemChanged(
        EnumDisplayHelper.EnumDisplayItem<OutputFormatEnum>? oldValue,
        EnumDisplayHelper.EnumDisplayItem<OutputFormatEnum>? newValue)
    {
        if (newValue != null) _paramBean.OutputFileFormat = newValue.Value;
    }
    
    // 5. 输出编码
    public ObservableCollection<EnumDisplayHelper.EnumDisplayItem<OutputEncodingEnum>> OutputEncodings { get; } =
        EnumDisplayHelper.GetEnumDisplayCollection<OutputEncodingEnum>();
    
    [ObservableProperty]
    private EnumDisplayHelper.EnumDisplayItem<OutputEncodingEnum> _selectedOutputEncodingItem;

    public OutputEncodingEnum SelectedOutputEncoding => SelectedOutputEncodingItem?.Value ?? default;
    
    partial void OnSelectedOutputEncodingItemChanged(
        EnumDisplayHelper.EnumDisplayItem<OutputEncodingEnum>? oldValue,
        EnumDisplayHelper.EnumDisplayItem<OutputEncodingEnum>? newValue)
    {
        if (newValue != null) _paramBean.Encoding = newValue.Value;
    }
    
    // 6. 歌词合并符
    [ObservableProperty] private string _lrcMergeSeparator = "";
    
    partial void OnLrcMergeSeparatorChanged(string? oldValue, string newValue)
    {
        _paramBean.LrcMergeSeparator = newValue;
    }
    
    // 7. 搜索内容
    [ObservableProperty] private string _searchText = "";
    
    /// <summary>
    /// 实际处理的歌曲 ID 列表
    /// </summary>
    public readonly List<InputSongId> SongIds = [];

    private PersistParamBean _paramBean;

    public void Bind(PersistParamBean paramBean)
    {
        _paramBean = paramBean;
        
        SelectedSearchSourceItem = SearchSources.FirstOrDefault(item => Equals(item.Value, _paramBean.SearchSource)) ?? SearchSources.First();
        SelectedLrcTypeItem = LrcTypes.FirstOrDefault(item => Equals(item.Value, _paramBean.ShowLrcType)) ?? LrcTypes.First();
        SelectedSearchTypeItem = SearchTypes.FirstOrDefault(item => Equals(item.Value, _paramBean.SearchType)) ?? SearchTypes.First();
        SelectedOutputFormatItem = OutputFormats.FirstOrDefault(item => Equals(item.Value, _paramBean.OutputFileFormat)) ?? OutputFormats.First();
        SelectedOutputEncodingItem = OutputEncodings.FirstOrDefault(item => Equals(item.Value, _paramBean.Encoding)) ?? OutputEncodings.First();
        LrcMergeSeparator = _paramBean.LrcMergeSeparator;
    }
}