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

            switch (searchResultVo.SearchType)
            {
                case SearchTypeEnum.SONG_ID:
                    // Add columns.
                    table.Columns.Add("歌曲名", typeof(string));
                    table.Columns.Add("作者名 ", typeof(string));
                    table.Columns.Add("专辑名", typeof(string));
                    table.Columns.Add("时长", typeof(string));

                    // Add rows.
                    var songVos = searchResultVo.SongVos;
                    _ids = new string[songVos.Count];

                    for (var i = 0; i < songVos.Count; i++)
                    {
                        var songVo = songVos[i];
                        var duration = new LyricTimestamp(songVo.Duration).PrintTimestamp("mm:ss", DotTypeEnum.DOWN);

                        _ids[i] = songVo.DisplayId;
                        table.Rows.Add(songVo.SongName, string.Join(",", songVo.AuthorName), songVo.AlbumName,
                            duration);
                    }

                    break;
                case SearchTypeEnum.ALBUM_ID:
                    // Add columns.
                    table.Columns.Add("专辑名", typeof(string));
                    table.Columns.Add("作者名 ", typeof(string));
                    table.Columns.Add("发行公司", typeof(string));

                    // Add rows.
                    var albumVos = searchResultVo.AlbumVos;
                    _ids = new string[albumVos.Count];

                    for (var i = 0; i < albumVos.Count; i++)
                    {
                        var albumVo = albumVos[i];

                        _ids[i] = albumVo.DisplayId;
                        table.Rows.Add(albumVo.AlbumName, string.Join(",", albumVo.AuthorName), albumVo.Company);
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
                _mainForm.Search_Btn_Click(sender, e);
                Close();
            }
        }
    }
}