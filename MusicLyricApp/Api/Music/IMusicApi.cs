using System.Collections.Generic;
using MusicLyricApp.Bean;

namespace MusicLyricApp.Api.Music
{
    public interface IMusicApi
    {
        SearchSourceEnum Source();

        /// <summary>
        /// 获取歌单信息
        /// </summary>
        /// <param name="playlistId">歌单ID</param>
        ResultVo<PlaylistVo> GetPlaylistVo(string playlistId);

        /// <summary>
        /// 获取专辑信息
        /// </summary>
        /// <param name="albumId">专辑ID</param>
        ResultVo<AlbumVo> GetAlbumVo(string albumId);

        /// <summary>
        /// 获取歌曲信息
        /// </summary>
        /// <param name="songIds">歌曲ID列表</param>
        /// <returns>songId, songVo</returns>
        Dictionary<string, ResultVo<SongVo>> GetSongVo(string[] songIds);

        /// <summary>
        /// 获取歌曲链接
        /// </summary>
        /// <param name="songId"></param>
        /// <returns></returns>
        ResultVo<string> GetSongLink(string songId);

        /// <summary>
        /// 获取歌词信息
        /// </summary>
        /// <param name="id">歌曲服务商内部 ID</param>
        /// <param name="displayId">歌曲服务商显示 ID</param>
        /// <param name="isVerbatim">是否尝试获取逐字歌词</param>
        /// <returns></returns>
        ResultVo<LyricVo> GetLyricVo(string id, string displayId, bool isVerbatim);

        /// <summary>
        /// 获取搜索结果
        /// </summary>
        /// <param name="keyword">关键词</param>
        /// <param name="searchType">搜索类型</param>
        /// <returns></returns>
        ResultVo<SearchResultVo> Search(string keyword, SearchTypeEnum searchType);
    }
}