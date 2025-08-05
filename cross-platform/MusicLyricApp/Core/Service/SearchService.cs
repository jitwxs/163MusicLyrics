using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;
using NLog;
using SearchParamViewModel = MusicLyricApp.ViewModels.SearchParamViewModel;

namespace MusicLyricApp.Core.Service;

public class SearchService(SettingBean settingBean) : ISearchService
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly Dictionary<SearchSourceEnum, IMusicApi> _api = new()
    {
        { SearchSourceEnum.QQ_MUSIC, new QQMusicApi(() => settingBean.Config.QQMusicCookie) },
        { SearchSourceEnum.NET_EASE_MUSIC, new NetEaseMusicApi(() => settingBean.Config.NetEaseCookie) },
        { SearchSourceEnum.SODA_MUSIC, new SodaMusicApi() }
    };

    public IMusicApi GetMusicApi(SearchSourceEnum searchSource)
    {
        return _api[searchSource];
    }

    public void InitSongIds(SearchParamViewModel searchParam, SettingBean settingBean)
    {
        var inputs = DealWithSearchText(searchParam);

        if (inputs.Length < 1)
        {
            throw new MusicLyricException(ErrorMsgConst.SEARCH_RESULT_EMPTY);
        }

        searchParam.SongIds.Clear();

        foreach (var input in inputs)
        {
            var searchSong =
                GlobalUtils.CheckInputId(input, searchParam.SelectedSearchSource, searchParam.SelectedSearchType);
            var musicApi = GetMusicApi(searchSong.SearchSource);

            switch (searchSong.SearchType)
            {
                case SearchTypeEnum.ALBUM_ID:
                    foreach (var simpleSongVo in musicApi.GetAlbumVo(searchSong.QueryId).Assert().Data
                                 .SimpleSongVos)
                    {
                        searchParam.SongIds.Add(new InputSongId(simpleSongVo.DisplayId, searchSong));
                    }

                    break;
                case SearchTypeEnum.PLAYLIST_ID:
                    foreach (var simpleSongVo in musicApi.GetPlaylistVo(searchSong.QueryId).Assert().Data
                                 .SimpleSongVos)
                    {
                        searchParam.SongIds.Add(new InputSongId(simpleSongVo.DisplayId, searchSong));
                    }

                    break;
                case SearchTypeEnum.SONG_ID:
                    // 汽水音乐直接用原始构造，其他保持不变
                    if (searchSong.SearchSource == SearchSourceEnum.SODA_MUSIC)
                        searchParam.SongIds.Add(new InputSongId(searchSong.QueryId, searchSong.SearchSource, searchSong.SearchType));
                    else
                        searchParam.SongIds.Add(new InputSongId(searchSong.QueryId, searchSong));
                    break;
                default:
                    throw new MusicLyricException(ErrorMsgConst.SYSTEM_ERROR);
            }
        }
    }

    public Dictionary<string, ResultVo<SaveVo>> SearchSongs(List<InputSongId> inputSongIds, SettingBean settingBean)
    {
        var isVerbatimLyric = settingBean.Config.EnableVerbatimLyric;

        var resultDict = new Dictionary<string, ResultVo<SaveVo>>();

        var songDict = new Dictionary<SearchSourceEnum, List<string>>();
        var songIndexDict = new Dictionary<string, int>();

        for (var i = 0; i < inputSongIds.Count; i++)
        {
            var searchSource = inputSongIds[i].SearchSource;
            var songId = inputSongIds[i].SongId;

            if (!songDict.ContainsKey(searchSource))
            {
                songDict.Add(searchSource, []);
            }

            songDict[searchSource].Add(songId);
            songIndexDict[songId] = i + 1;
        }

        foreach (var searchSourcePair in songDict)
        {
            var musicApi = GetMusicApi(searchSourcePair.Key);

            var songResp = musicApi.GetSongVo(searchSourcePair.Value.ToArray());

            foreach (var (songId, resultVo) in songResp)
            {
                ResultVo<SaveVo> songResult;

                try
                {
                    var songVo = resultVo.Assert().Data;

                    var lyricVo = musicApi.GetLyricVo(songVo.Id, songVo.DisplayId, isVerbatimLyric).Assert().Data;
                    lyricVo.Duration = songVo.Duration;

                    var index = songIndexDict[songId];

                    songResult = new ResultVo<SaveVo>(new SaveVo(index, songVo, lyricVo));
                }
                catch (WebException ex)
                {
                    _logger.Error(ex, "SearchBySongId network error, songId: {SongId}, delay: {Delay}", songId,
                        NetworkUtils.GetWebRoundtripTime(50));
                    songResult = ResultVo<SaveVo>.Failure(ErrorMsgConst.NETWORK_ERROR);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "SearchBySongId error, songId: {SongId}, message: {ErrorMsg}", songId,
                        ex.Message);
                    songResult = ResultVo<SaveVo>.Failure(ex.Message);
                }

                resultDict.Add(songId, songResult);
            }
        }

        return resultDict;
    }


    public async Task<bool> RenderSearchResult(
        SearchParamViewModel searchParam,
        SearchResultViewModel searchResult,
        SettingBean settingBean,
        Dictionary<string, ResultVo<SaveVo>> resDict)
    {
        searchResult.SaveVoMap.Clear();

        if (searchParam.SongIds.Count == 1)
        {
            var songId = searchParam.SongIds.First().SongId;
            var saveVo = resDict[songId].Assert().Data;

            searchResult.SaveVoMap.Add(songId, saveVo);

            // render UI
            var lyricVo = saveVo.LyricVo;

            searchResult.ConsoleOutput = lyricVo.IsEmpty()
                ? ErrorMsgConst.LRC_NOT_EXIST
                : GlobalUtils.MergeStr(await LyricUtils.GetOutputContent(lyricVo, settingBean));
            searchResult.Singer = string.Join(settingBean.Config.SingerSeparator, saveVo.SongVo.Singer);
            searchResult.Album = saveVo.SongVo.Album;
            searchResult.SongName = saveVo.SongVo.Name;
        }
        else
        {
            searchResult.ResetConsoleOutput(RenderUtils.RenderSearchResult(resDict, searchResult.SaveVoMap));
        }

        return true;
    }

    public List<SearchResultVo> BlurSearch(SearchParamViewModel searchParam, SettingBean settingBean)
    {
        var keyword = searchParam.SearchText.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            throw new MusicLyricException(ErrorMsgConst.INPUT_CONENT_EMPLT);
        }

        var resultVoList = new List<SearchResultVo>();

        var searchType = settingBean.Param.SearchType;
        if (settingBean.Config.AggregatedBlurSearch)
        {
            foreach (var searchSource in Enum.GetValues<SearchSourceEnum>())
            {
                var one = GetMusicApi(searchSource).Search(keyword, searchType);
                if (one.IsSuccess())
                {
                    resultVoList.Add(one.Data);
                }
            }
        }
        else
        {
            resultVoList.Add(GetMusicApi(settingBean.Param.SearchSource).Search(keyword, searchType).Assert().Data);
        }

        resultVoList.RemoveAll(one => one.IsEmpty());
                    
        if (resultVoList.Count == 0)
        {
            throw new MusicLyricException(ErrorMsgConst.SEARCH_RESULT_EMPTY);
        }

        return resultVoList;
    }

    private static string[] DealWithSearchText(SearchParamViewModel searchParamView)
    {
        var inputText = searchParamView.SearchText.Trim();
        var inputStrList = new List<string>();

        // 判断是否是目录
        if (Directory.Exists(inputText))
        {
            var searchSource = searchParamView.SelectedSearchSource;
            var searchType = searchParamView.SelectedSearchType;

            var directoryInfo = new DirectoryInfo(inputText);
            foreach (var info in directoryInfo.GetFileSystemInfos())
            {
                if (info is DirectoryInfo)
                {
                    // 文件夹，跳过处理，不做递归
                    continue;
                }
                else
                {
                    var name = info.Name;

                    if (!string.IsNullOrWhiteSpace(info.Extension) && name.EndsWith(info.Extension))
                    {
                        name = name.Remove(name.Length - info.Extension.Length);
                    }

                    name = name.Trim();

                    try
                    {
                        // check filename is legal param
                        GlobalUtils.CheckInputId(name, searchSource, searchType);
                        inputStrList.Add(name);
                    }
                    catch (MusicLyricException ignore)
                    {
                    }
                }
            }
        }
        else
        {
            // 不是目录，认为是实际的 ID
            foreach (var name in inputText.Split(','))
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                inputStrList.Add(name.Trim());
            }
        }

        return inputStrList.ToArray();
    }
}