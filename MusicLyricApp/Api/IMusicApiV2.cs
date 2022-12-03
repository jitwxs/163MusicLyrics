using System.Collections.Generic;
using MusicLyricApp.Bean;

namespace MusicLyricApp.Api
{
    public interface IMusicApiV2
    {
        SearchSourceEnum Source();
        
        /// <summary>
        /// 根据专辑ID获取歌曲ID列表
        /// </summary>
        /// <param name="albumId">专辑ID</param>
        /// <returns>歌曲ID列表</returns>
        IEnumerable<string> GetSongIdsFromAlbum(string albumId);

        /// <summary>
        /// 获取歌曲信息
        /// </summary>
        /// <param name="songIds">歌曲ID列表</param>
        /// <returns></returns>
        Dictionary<string, ResultVo<SongVo>> GetSongVo(string[] songIds);
        
        /// <summary>
        /// 获取歌词信息
        /// </summary>
        /// <param name="songVo">歌曲信息</param>
        /// <param name="isVerbatim">是否尝试获取逐字歌词</param>
        /// <returns></returns>
        LyricVo GetLyricVo(SongVo songVo, bool isVerbatim);

        /// <summary>
        /// 搜索功能
        /// </summary>
        /// <param name="keyword">关键词</param>
        /// <param name="searchType">搜索类型</param>
        /// <returns></returns>
        ResultVo<SearchResultVo> Search(string keyword, SearchTypeEnum searchType);
    }
}