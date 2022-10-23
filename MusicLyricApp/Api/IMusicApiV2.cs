using System.Collections.Generic;
using MusicLyricApp.Bean;

namespace MusicLyricApp.Api
{
    public interface IMusicApiV2
    {
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
        /// <param name="songId">歌曲ID</param>
        /// <returns></returns>
        LyricVo GetLyricVo(string songId);
    }
}