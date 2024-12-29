using System.Collections.Generic;
using System.Linq;
using MusicLyricApp.Utils;

namespace MusicLyricApp.Bean
{
    /// <summary>
    /// 搜索接口结果
    /// </summary>
    public class SearchResult
    {
        /* SearchType = SONG */
        public Song[] Songs { get; set; }
        
        public long SongCount { get; set; }
        
        /* SearchType = ALBUM */
        
        public Album[] Albums { get; set; }
        
        public long AlbumCount { get; set; }
        
        /* SearchType = PLAYLIST */
        
        public SimplePlaylist[] Playlists { get; set; }
        
        public long PlaylistCount { get; set; }
        
        /* ----- */
        
        public SearchResultVo Convert(SearchTypeEnum searchType)
        {
            var vo = new SearchResultVo
            {
                SearchType = searchType,
                SearchSource = SearchSourceEnum.NET_EASE_MUSIC
            };

            switch (searchType)
            {
                case SearchTypeEnum.SONG_ID:
                    if (SongCount > 0)
                    {
                        foreach (var song in Songs)
                        {
                            vo.SongVos.Add(new SearchResultVo.SongSearchResultVo
                            {
                                DisplayId = song.Id,
                                Title = song.Name,
                                AuthorName = song.Ar.Select(e => e.Name).ToArray(),
                                AlbumName = song.Al.Name,
                                Duration = song.Dt
                            });
                        }
                    }
                    break;
                case SearchTypeEnum.ALBUM_ID:
                    if (AlbumCount > 0)
                    {
                        foreach (var album in Albums)
                        {
                            vo.AlbumVos.Add(new SearchResultVo.AlbumSearchResultVo
                            {
                                DisplayId = album.Id.ToString(),
                                AlbumName = album.Name,
                                AuthorName = album.Artists.Select(e => e.Name).ToArray(),
                                SongCount = album.Size,
                                PublishTime = GlobalUtils.FormatDate(album.PublishTime)
                            });
                        }
                    }
                    break;
                case SearchTypeEnum.PLAYLIST_ID:
                    if (PlaylistCount > 0)
                    {
                        foreach (var playlist in Playlists)
                        {
                            vo.PlaylistVos.Add(new SearchResultVo.PlaylistResultVo()
                            {
                                DisplayId = playlist.Id,
                                PlaylistName = playlist.Name,
                                AuthorName = playlist.Creator.Nickname,
                                Description = playlist.Description,
                                PlayCount = playlist.PlayCount,
                                SongCount = playlist.TrackCount,
                            });
                        }
                    }
                    break;
            }

            return vo;
        }
    }

    public class SongUrls
    {
        public Datum[] Data { get; set; }
        public long Code { get; set; }
    }
    
    public class Datum
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public long Br { get; set; }
        public long Size { get; set; }
        public string Md5 { get; set; }
        public long Code { get; set; }
        public long Expi { get; set; }
        public string Type { get; set; }
        public double Gain { get; set; }
        public long Fee { get; set; }
        public object Uf { get; set; }
        public long Payed { get; set; }
        public long Flag { get; set; }
        public bool CanExtend { get; set; }
    }

    public class LyricResult
    {
        public bool Sgc { get; set; }
        public bool Sfy { get; set; }
        public bool Qfy { get; set; }
        public bool Nolyric { get; set; }
        public bool Uncollected { get; set; }
        public LyricUser TransUser { get; set; }
        public LyricUser LyricUser { get; set; }
        public Lrc Lrc { get; set; }
        public Klyric Klyric { get; set; }
        public Lrc Tlyric { get; set; }
        public long Code { get; set; }

        public LyricVo ToVo()
        {
            var lyricVo = new LyricVo
            {
                SearchSource = SearchSourceEnum.NET_EASE_MUSIC
            };

            if (Lrc != null)
            {
                lyricVo.SetLyric(Lrc.Lyric);
            }

            if (Tlyric != null)
            {
                lyricVo.SetTranslateLyric(Tlyric.Lyric);
            }

            return lyricVo;
        }
    }
    
    public class LyricUser
    {
        public long Id { get; set; }
        public long Status { get; set; }
        public long Demand { get; set; }
        public long Userid { get; set; }
        public string Nickname { get; set; }
        public long Uptime { get; set; }
    }

    public class Klyric
    {
        public long Version { get; set; }
    }

    public class Lrc
    {
        public long Version { get; set; }
        public string Lyric { get; set; }
    }

    /// <summary>
    /// 歌单接口返回结果
    /// </summary>
    public class PlaylistResult
    {
        public long Code { get; set; }
        
        public string Urls { get; set; }
        
        public string RelatedVideos { get; set; }
        
        /// <summary>
        /// 歌单作者信息
        /// </summary>
        public Playlist Playlist { get; set; }

        /// <summary>
        /// 歌单歌曲权限信息
        /// </summary>
        public Privilege[] Privileges { get; set; }

        public PlaylistVo Convert(SimpleSongVo[] simpleSongVos)
        {
            var creator = Playlist.Creator;
            return new PlaylistVo
            {
                Name = Playlist.Name,
                AuthorName = creator == null ? "" : creator.Nickname,
                Description = Playlist.Description,
                SimpleSongVos = simpleSongVos
            };
        }
    }
    
    /// <summary>
    /// 专辑接口返回结果
    /// </summary>
    public class AlbumResult
    {
        public Song[] Songs { get; set; }
        public long Code { get; set; }
        public Album Album { get; set; }

        public AlbumVo Convert()
        {
            return new AlbumVo
            {
                Name = Album.Name,
                Company = Album.Company,
                Desc = Album.Description,
                SimpleSongVos = Songs.Select(e => e.ConvertSimple()).ToArray(),
                TimePublic = GlobalUtils.FormatDate(Album.PublishTime)
            };
        }
    }

    /// <summary>
    /// 模糊搜索歌单返回的结果，仅有部分属性
    /// </summary>
    public class SimplePlaylist
    {
        public string Id { get; set; }
        
        /// <summary>
        /// 歌单名
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 歌单封面 URL
        /// </summary>
        public string CoverImgUrl { get; set; }
        
        /// <summary>
        /// 创建者 ID
        /// </summary>
        public long UserId { get; set; }
        
        /// <summary>
        /// 创建者
        /// </summary>
        public Creator Creator { get; set; }
        
        /// <summary>
        /// 歌单描述
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// 播放数量
        /// </summary>
        public long PlayCount { get; set; }
        
        /// <summary>
        /// 歌曲数量
        /// </summary>
        public long TrackCount { get; set; }
    }

    /// <summary>
    /// 精确搜索歌单返回的结果
    /// </summary>
    public class Playlist : SimplePlaylist
    {
        /// <summary>
        /// 歌单名
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 歌单创建者
        /// </summary>
        public Creator Creator { get; set; }
        
        /// <summary>
        /// 歌单封面 ID
        /// </summary>
        public long CoverImgId { get; set; }
        
        /// <summary>
        /// 歌单创建时间
        /// </summary>
        public long CreateTime { get; set; }
        
        /// <summary>
        /// 订阅数量
        /// </summary>
        public long SubscribedCount { get; set; }
        
        /// <summary>
        /// 分享数量
        /// </summary>
        public long ShareCount { get; set; }
        
        /// <summary>
        /// 评论数量
        /// </summary>
        public long CommentCount { get; set; }
        
        /// <summary>
        /// 歌单歌曲列表信息
        /// </summary>
        public SimpleTrack[] TrackIds { get; set; }
    }

    public class SimpleTrack
    {
        public long Id { get; set; }
        
        public long Uid { get; set; }
    }

    /// <summary>
    /// 专辑
    /// </summary>
    public class Album
    {
        public bool Paid { get; set; }
        public bool OnSale { get; set; }
        public long PicId { get; set; }
        public object[] Alias { get; set; }
        public string CommentThreadId { get; set; }
        public long PublishTime { get; set; }
        public string Company { get; set; }
        public long CopyrightId { get; set; }
        public string PicUrl { get; set; }
        public Artist Artist { get; set; }
        public object BriefDesc { get; set; }
        public string Tags { get; set; }
        public Artist[] Artists { get; set; }
        public long Status { get; set; }
        public string Description { get; set; }
        public object SubType { get; set; }
        public string BlurPicUrl { get; set; }
        public long CompanyId { get; set; }
        public long Pic { get; set; }
        public string Name { get; set; }
        public long Id { get; set; }
        public string Type { get; set; }
        /// <summary>
        /// 歌曲数量
        /// </summary>
        public long Size { get; set; }
        public string PicIdStr { get; set; }
        public Info Info { get; set; }
    }

    public class Song
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public long Pst { get; set; }
        public long T { get; set; }
        public List<Ar> Ar { get; set; }
        public List<object> Alia { get; set; }
        public long Pop { get; set; }
        public long St { get; set; }
        public string Rt { get; set; }
        public long Fee { get; set; }
        public long V { get; set; }
        public object Crbt { get; set; }
        public string Cf { get; set; }
        public Al Al { get; set; }
        /// <summary>
        /// 时长，单位ms
        /// </summary>
        public long Dt { get; set; }
        public H H { get; set; }
        public H M { get; set; }
        public H L { get; set; }
        public object A { get; set; }
        public string Cd { get; set; }
        public long No { get; set; }
        public object RtUrl { get; set; }
        public long Ftype { get; set; }
        public List<object> RtUrls { get; set; }
        public object Rurl { get; set; }
        public long Rtype { get; set; }
        public long Mst { get; set; }
        public long Cp { get; set; }
        public long Mv { get; set; }
        /// <summary>
        /// 时间戳，eg 1657900800000
        /// </summary>
        public long PublishTime { get; set; }
        public Privilege Privilege { get; set; }

        public SimpleSongVo ConvertSimple()
        {
            return new SimpleSongVo
            {
                Id = Id,
                DisplayId = Id,
                Name = Name,
                Singer = Ar.Select(e => e.Name).ToArray()
            };
        }
    }

    public class Info
    {
        public CommentThread CommentThread { get; set; }
        public object LatestLikedUsers { get; set; }
        public bool Liked { get; set; }
        public object Comments { get; set; }
        public long ResourceType { get; set; }
        public long ResourceId { get; set; }
        public long CommentCount { get; set; }
        public long LikedCount { get; set; }
        public long ShareCount { get; set; }
        public string ThreadId { get; set; }
    }

    public class CommentThread
    {
        public string Id { get; set; }
        public ResourceInfo ResourceInfo { get; set; }
        public long ResourceType { get; set; }
        public long CommentCount { get; set; }
        public long LikedCount { get; set; }
        public long ShareCount { get; set; }
        public long HotCount { get; set; }
        public object LatestLikedUsers { get; set; }
        public long ResourceId { get; set; }
        public long ResourceOwnerId { get; set; }
        public string ResourceTitle { get; set; }
    }

    public class Artist
    {
        public long Img1V1Id { get; set; }
        public long TopicPerson { get; set; }
        public long PicId { get; set; }
        public object BriefDesc { get; set; }
        public long AlbumSize { get; set; }
        public string Img1V1Url { get; set; }
        public string PicUrl { get; set; }
        public List<string> Alias { get; set; }
        public string Trans { get; set; }
        public long MusicSize { get; set; }
        public string Name { get; set; }
        public long Id { get; set; }
        public long PublishTime { get; set; }
        public long MvSize { get; set; }
        public bool Followed { get; set; }
    }

    /// <summary>
    ///  创建者
    /// </summary>
    public class Creator
    {
        /// <summary>
        /// 用户 ID
        /// </summary>
        public long UserId { get; set; }
        
        /// <summary>
        /// 昵称
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// 个人介绍
        /// </summary>
        public string Signature { get; set; }
        
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// 头像地址
        /// </summary>
        public string AvatarUrl { get; set; }
    }

    public class ResourceInfo
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Name { get; set; }
        public object ImgUrl { get; set; }
        public Creator Creator { get; set; }
    }

    public class Privilege
    {
        public long Id { get; set; }
        public long Fee { get; set; }
        public long Payed { get; set; }
        public long St { get; set; }
        public long Pl { get; set; }
        public long Dl { get; set; }
        public long Sp { get; set; }
        public long Cp { get; set; }
        public long Subp { get; set; }
        public bool Cs { get; set; }
        public long Maxbr { get; set; }
        public long Fl { get; set; }
        public bool Toast { get; set; }
        public long Flag { get; set; }
    }

    public class DetailResult
    {
        public Song[] Songs { get; set; }
        public Privilege[] Privileges { get; set; }
        public long Code { get; set; }
    }


    public class Al
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string PicUrl { get; set; }
        public List<object> Tns { get; set; }
        public long Pic { get; set; }
    }

    public class Ar
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public List<object> Tns { get; set; }
        public List<object> Alias { get; set; }
    }

    public class H
    {
        public long Br { get; set; }
        public long Fid { get; set; }
        public long Size { get; set; }
        public double Vd { get; set; }
    }
}
