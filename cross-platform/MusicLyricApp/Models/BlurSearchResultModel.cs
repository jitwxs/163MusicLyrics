using CommunityToolkit.Mvvm.ComponentModel;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Localization;

namespace MusicLyricApp.Models;

public abstract partial class BlurSearchResultBase : LocalizedObject
{
    private SearchSourceEnum? _source;
    [ObservableProperty] private string? _id;
    [ObservableProperty] private string? _title;
    [ObservableProperty] private string? _author;

    /// <summary>Name of the source in the current UI language.</summary>
    public string? SearchSource => _source is { } source ? MusicSources.Get(source).Name : null;

    public void SetSource(SearchSourceEnum source)
    {
        _source = source;
        OnPropertyChanged(nameof(SearchSource));
    }
}

public partial class BlurSongSearchResult : BlurSearchResultBase
{
    [ObservableProperty] private string? _album;
    [ObservableProperty] private string? _duration;

    public BlurSongSearchResult(SearchSourceEnum searchSource, SearchResultVo.SongSearchResultVo resultVo)
    {
        var idPrefix = MusicSources.Get(searchSource).LinkPrefix(SearchTypeEnum.SONG_ID);

        Id = idPrefix + resultVo.DisplayId;
        Title = resultVo.Title;
        Author = string.Join(",", resultVo.AuthorName);
        Album = resultVo.AlbumName;
        Duration = new LyricTimestamp(resultVo.Duration).PrintTimestamp("mm:ss", DotTypeEnum.DOWN);
        SetSource(searchSource);
    }
}

public partial class BlurAlbumSearchResult : BlurSearchResultBase
{
    [ObservableProperty] private string? _songCount;
    [ObservableProperty] private string? _publishTime;

    public BlurAlbumSearchResult(SearchSourceEnum searchSource, SearchResultVo.AlbumSearchResultVo resultVo)
    {
        var idPrefix = MusicSources.Get(searchSource).LinkPrefix(SearchTypeEnum.ALBUM_ID);

        Id = idPrefix + resultVo.DisplayId;
        Title = resultVo.AlbumName;
        Author = string.Join(",", resultVo.AuthorName);
        SongCount = resultVo.SongCount.ToString();
        PublishTime = resultVo.PublishTime;
        SetSource(searchSource);
    }
}

public partial class BlurPlaylistSearchResult : BlurSearchResultBase
{
    [ObservableProperty] private string? _description;
    [ObservableProperty] private string? _songCount;
    [ObservableProperty] private string? _playCount;

    public BlurPlaylistSearchResult(SearchSourceEnum searchSource, SearchResultVo.PlaylistResultVo resultVo)
    {
        var idPrefix = MusicSources.Get(searchSource).LinkPrefix(SearchTypeEnum.PLAYLIST_ID);

        Id = idPrefix + resultVo.DisplayId;
        Title = resultVo.PlaylistName;
        Author = resultVo.AuthorName;
        Description = resultVo.Description;
        SongCount = resultVo.SongCount.ToString();
        PlayCount = resultVo.PlayCount.ToString();
        SetSource(searchSource);
    }
}
