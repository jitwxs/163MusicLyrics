using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using MusicLyricApp.Bean;
using MusicLyricApp.Utils;

namespace MusicLyricApp
{
    public partial class BlurForm : MusicLyricForm
    {
        private readonly MainForm _mainForm;

        private string[] _ids;

        public BlurForm(List<SearchResultVo> searchResList, MainForm mainForm)
        {
            _mainForm = mainForm;

            InitializeComponent();

            InitialDataGridView(searchResList);
        }

        private void InitialDataGridView(List<SearchResultVo> searchResList)
        {
            var table = new DataTable();

            var searchType = searchResList[0].SearchType;
            switch (searchType)
            {
                case SearchTypeEnum.SONG_ID:
                    // Add columns.
                    table.Columns.Add("歌曲", typeof(string));
                    table.Columns.Add("歌手 ", typeof(string));
                    table.Columns.Add("专辑", typeof(string));
                    table.Columns.Add("时长", typeof(string));
                    table.Columns.Add("平台", typeof(string));
                    break;
                case SearchTypeEnum.ALBUM_ID:
                    // Add columns.
                    table.Columns.Add("专辑", typeof(string));
                    table.Columns.Add("歌手 ", typeof(string));
                    table.Columns.Add("歌曲数量", typeof(string));
                    table.Columns.Add("发行时间", typeof(string));
                    table.Columns.Add("平台", typeof(string));
                    break;
                case SearchTypeEnum.PLAYLIST_ID:
                    // Add columns.
                    table.Columns.Add("歌单名", typeof(string));
                    table.Columns.Add("作者名 ", typeof(string));
                    table.Columns.Add("描述", typeof(string));
                    table.Columns.Add("歌曲数量", typeof(string));
                    table.Columns.Add("播放量", typeof(string));
                    table.Columns.Add("平台", typeof(string));
                    break;
            }
            
            var idList = new List<string>();

            foreach (var searchResultVo in searchResList)
            {
                var searchSource = searchResultVo.SearchSource;
                var idPrefix = GlobalUtils.SearchSourceKeywordDict[searchSource] + "/" + GlobalUtils.SearchTypeKeywordDict[searchSource][searchType];
                
                switch (searchType)
                {
                    case SearchTypeEnum.SONG_ID:
                        // Add rows.
                        foreach (var songVo in searchResultVo.SongVos)
                        {
                            var duration = new LyricTimestamp(songVo.Duration).PrintTimestamp("mm:ss", DotTypeEnum.DOWN);

                            idList.Add(idPrefix + songVo.DisplayId);
                            table.Rows.Add(songVo.Title, string.Join(",", songVo.AuthorName), songVo.AlbumName, 
                                duration, searchSource.ToDescription());
                        }
                        break;
                    case SearchTypeEnum.ALBUM_ID:
                        // Add rows.
                        foreach (var albumVo in searchResultVo.AlbumVos)
                        {
                            idList.Add(idPrefix + albumVo.DisplayId);
                            table.Rows.Add(albumVo.AlbumName, string.Join(",", albumVo.AuthorName), albumVo.SongCount, 
                                albumVo.PublishTime, searchSource.ToDescription());
                        }
                        break;
                    case SearchTypeEnum.PLAYLIST_ID:
                        // Add rows.
                        foreach (var playlistVo in searchResultVo.PlaylistVos)
                        {
                            idList.Add(idPrefix + playlistVo.DisplayId);
                            table.Rows.Add(playlistVo.PlaylistName, playlistVo.AuthorName, playlistVo.Description, 
                                playlistVo.SongCount, playlistVo.PlayCount, searchSource.ToDescription());
                        }
                        break;
                }
            }

            Blur_DataGridView.DataSource = table;
            _ids = idList.ToArray();
        }

        private void BlurSearch_DataGridView_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            // 鼠标右击事件
            if (e.Button == MouseButtons.Right)
                foreach (DataGridViewRow row in Blur_DataGridView.Rows)
                    if (row.Selected)
                    {
                        Blur_ContextMenuStrip.Show(MousePosition.X, MousePosition.Y);
                        break;
                    }
        }

        private void Blur_Download_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selectIds = new List<string>();

            foreach (DataGridViewRow row in Blur_DataGridView.Rows)
                if (row.Selected)
                    selectIds.Add(_ids[row.Index]);

            // 存在选中的数据，跳转会主窗口
            if (selectIds.Count > 0)
            {
                _mainForm.Search_Text.Text = string.Join(",", selectIds);
                _mainForm.Search_Btn_Click("Search_Btn", e);
                Close();
            }
        }
    }
}