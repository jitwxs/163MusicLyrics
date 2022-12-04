using System.Collections.Generic;
using System.Linq;
using MusicLyricApp.Bean;
using MusicLyricApp.Cache;
using MusicLyricApp.Utils;

namespace MusicLyricApp.Api
{
    public abstract class MusicApiV2Cacheable : IMusicApiV2
    {
        protected abstract SearchSourceEnum Source0();
        
        protected abstract ResultVo<AlbumVo> GetAlbumVo0(string albumId);

        protected abstract Dictionary<string, ResultVo<SongVo>> GetSongVo0(string[] songIds);

        protected abstract ResultVo<LyricVo> GetLyricVo0(long id, string displayId, bool isVerbatim);

        protected abstract  ResultVo<SearchResultVo> Search0(string keyword, SearchTypeEnum searchType);

        public SearchSourceEnum Source()
        {
            return Source0();
        }

        public ResultVo<AlbumVo> GetAlbumVo(string albumId)
        {
            return GlobalCache.Process(CacheType.ALBUM_VO, albumId, e => GetAlbumVo0(albumId));
        }

        public Dictionary<string, ResultVo<SongVo>> GetSongVo(string[] songIds)
        {
            var result = GlobalCache.BatchQuery<string, SongVo>(CacheType.SONG_VO, songIds, out var notHitKeys)
                .ToDictionary(pair => pair.Key, pair => new ResultVo<SongVo>(pair.Value));

            foreach(var pair in GetSongVo0(notHitKeys))
            {
                var songId = pair.Key;
                var resultVo = pair.Value;
                
                if (resultVo.IsSuccess())
                {
                    GlobalCache.DoCache(CacheType.SONG_VO, songId, resultVo.Data);
                }
                
                result[songId] = pair.Value;
            }

            return result;
        }

        public ResultVo<LyricVo> GetLyricVo(long id, string displayId, bool isVerbatim)
        {
            ResultVo<LyricVo> CacheFunc(int e) => GetLyricVo0(id, displayId, isVerbatim);

            return GlobalCache.Process(CacheType.LYRIC_VO, GlobalUtils.GetSongKey(displayId, isVerbatim), CacheFunc);
        }

        public ResultVo<SearchResultVo> Search(string keyword, SearchTypeEnum searchType)
        {
            var cacheKey = Source0() + "_" + searchType + "_" + keyword;

            ResultVo<SearchResultVo> CacheFunc(int e) => Search0(keyword, searchType);

            return GlobalCache.Process(CacheType.SEARCH_RESULT_VO, cacheKey, CacheFunc);
        }
    }
}