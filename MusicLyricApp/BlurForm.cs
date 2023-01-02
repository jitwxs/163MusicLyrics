using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using MusicLyricApp.Bean;

namespace MusicLyricApp
{
    public partial class BlurForm : MusicLyricForm
    {
        private readonly MainForm _mainForm;

        private string[] _ids;

        public BlurForm(SearchResultVo searchResultVo, MainForm mainForm)
        {
            _mainForm = mainForm;

            InitializeComponent();

            InitialDataGridView(searchResultVo);
        }

        private void InitialDataGridView(SearchResultVo searchResultVo)
        {
            var table = new DataTable();

            var source = searchResultVo.SearchSource.ToDescription();

            switch (searchResultVo.SearchType)
            {
                case SearchTypeEnum.SONG_ID:
                    // Add columns.
                    table.Columns.Add("歌曲", typeof(string));
                    table.Columns.Add("歌手 ", typeof(string));
                    table.Columns.Add("专辑", typeof(string));
                    table.Columns.Add("时长", typeof(string));
                    table.Columns.Add("平台", typeof(string));

                    // Add rows.
                    var songVos = searchResultVo.SongVos;
                    _ids = new string[songVos.Count];

                    for (var i = 0; i < songVos.Count; i++)
                    {
                        var songVo = songVos[i];
                        var duration = new LyricTimestamp(songVo.Duration).PrintTimestamp("mm:ss", DotTypeEnum.DOWN);

                        _ids[i] = songVo.DisplayId;
                        table.Rows.Add(songVo.Title, string.Join(",", songVo.AuthorName), songVo.AlbumName, 
                            duration, source);
                    }
                    break;
                case SearchTypeEnum.ALBUM_ID:
                    // Add columns.
                    table.Columns.Add("专辑", typeof(string));
                    table.Columns.Add("歌手 ", typeof(string));
                    table.Columns.Add("歌曲数量", typeof(string));
                    table.Columns.Add("发行时间", typeof(string));
                    table.Columns.Add("平台", typeof(string));

                    // Add rows.
                    var albumVos = searchResultVo.AlbumVos;
                    _ids = new string[albumVos.Count];

                    for (var i = 0; i < albumVos.Count; i++)
                    {
                        var albumVo = albumVos[i];

                        _ids[i] = albumVo.DisplayId;
                        table.Rows.Add(albumVo.AlbumName, string.Join(",", albumVo.AuthorName), albumVo.SongCount, 
                            albumVo.PublishTime, source);
                    }
                    break;
                case SearchTypeEnum.PLAYLIST_ID:
                    // Add columns.
                    table.Columns.Add("歌单名", typeof(string));
                    table.Columns.Add("作者名 ", typeof(string));
                    table.Columns.Add("描述", typeof(string));
                    table.Columns.Add("歌曲数量", typeof(string));
                    table.Columns.Add("播放量", typeof(string));
                    table.Columns.Add("平台", typeof(string));
                    
                    // Add rows.
                    var playlistVos = searchResultVo.PlaylistVos;
                    _ids = new string[playlistVos.Count];

                    for (var i = 0; i < playlistVos.Count; i++)
                    {
                        var playlistVo = playlistVos[i];

                        _ids[i] = playlistVo.DisplayId;
                        table.Rows.Add(playlistVo.PlaylistName, playlistVo.AuthorName, playlistVo.Description, 
                            playlistVo.SongCount, playlistVo.PlayCount, source);
                    }
                    break;
            }

            Blur_DataGridView.DataSource = table;
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