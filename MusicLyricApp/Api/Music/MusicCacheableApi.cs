using System.Collections.Generic;
using System.Linq;
using MusicLyricApp.Bean;
using MusicLyricApp.Cache;
using MusicLyricApp.Utils;

namespace MusicLyricApp.Api.Music
{
    public abstract class MusicCacheableApi : IMusicApi
    {
        protected abstract SearchSourceEnum Source0();
        
        protected abstract ResultVo<PlaylistVo> GetPlaylistVo0(string playlistId);
        
        protected abstract ResultVo<AlbumVo> GetAlbumVo0(string albumId);

        protected abstract Dictionary<string, ResultVo<SongVo>> GetSongVo0(string[] songIds);
        
        protected abstract ResultVo<string> GetSongLink0(string songId);

        protected abstract ResultVo<LyricVo> GetLyricVo0(string id, string displayId, bool isVerbatim);

        protected abstract  ResultVo<SearchResultVo> Search0(string keyword, SearchTypeEnum searchType);

        public SearchSourceEnum Source()
        {
            return Source0();
        }

        public ResultVo<PlaylistVo> GetPlaylistVo(string playlistId)
        {
            return GlobalCache.Process(Source(), CacheType.PLAYLIST_VO, playlistId, () => GetPlaylistVo0(playlistId));
        }

        public ResultVo<AlbumVo> GetAlbumVo(string albumId)
        {
            return GlobalCache.Process(Source(), CacheType.ALBUM_VO, albumId, () => GetAlbumVo0(albumId));
        }

        public Dictionary<string, ResultVo<SongVo>> GetSongVo(string[] songIds)
        {
            var result = GlobalCache.BatchQuery<SongVo>(Source(), CacheType.SONG_VO, songIds, 
                    out var notHitKeys).ToDictionary(pair => pair.Key, pair => new ResultVo<SongVo>(pair.Value));

            foreach(var pair in GetSongVo0(notHitKeys))
            {
                var songId = pair.Key;
                var resultVo = pair.Value;
                
                if (resultVo.IsSuccess())
                {
                    GlobalCache.DoCache(Source(), CacheType.SONG_VO, songId, resultVo.Data);
                }
                
                result[songId] = pair.Value;
            }

            return result;
        }

        public ResultVo<string> GetSongLink(string songId)
        {
            return GlobalCache.Process(Source(), CacheType.SONG_LINK, songId, () => GetSongLink0(songId));
        }

        public ResultVo<LyricVo> GetLyricVo(string id, string displayId, bool isVerbatim)
        {
            ResultVo<LyricVo> CacheFunc() => GetLyricVo0(id, displayId, isVerbatim);

            return GlobalCache.Process(Source(), CacheType.LYRIC_VO, GlobalUtils.GetSongKey(displayId, isVerbatim), CacheFunc);
        }

        public ResultVo<SearchResultVo> Search(string keyword, SearchTypeEnum searchType)
        {
            var cacheKey = Source0() + "_" + searchType + "_" + keyword;

            ResultVo<SearchResultVo> CacheFunc() => Search0(keyword, searchType);

            return GlobalCache.Process(Source(), CacheType.SEARCH_RESULT_VO, cacheKey, CacheFunc);
        }
    }
}