using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace 网易云歌词提取
{
    public partial class MainForm : Form
    {
        private readonly NetEaseMusicApiWrapper _api;

        private readonly Dictionary<long, SaveVo> _globalSaveVoMap = new Dictionary<long, SaveVo>();

        private readonly SearchInfo _globalSearchInfo = new SearchInfo();

        // 输出文件编码
        OUTPUT_ENCODING_ENUM output_encoding_enum;

        // 搜索类型
        SEARCH_TYPE_ENUM search_type_enum;

        // 强制两位类型
        DOT_TYPE_ENUM dot_type_enum;

        // 展示歌词类型
        SHOW_LRC_TYPE_ENUM show_lrc_type_enum;

        // 输出文件名类型
        OUTPUT_FILENAME_TYPE_ENUM output_filename_type_enum;

        // 输出文件类型
        OUTPUT_FORMAT_ENUM output_format_enum;

        public const string Version = "v3.8";

        public MainForm()
        {
            InitializeComponent();

            comboBox_output_name.SelectedIndex = 0;
            comboBox_output_encode.SelectedIndex = 0;
            comboBox_diglossia_lrc.SelectedIndex = 0;
            comboBox_search_type.SelectedIndex = 0;
            comboBox_dot.SelectedIndex = 0;
            comboBox_output_format.SelectedIndex = 0;

            _api = new NetEaseMusicApiWrapper();
        }

        private void ReloadConfig()
        {
            var ids = search_id_text.Text.Trim().Split(',');
            _globalSearchInfo.InputIds = new string[ids.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                _globalSearchInfo.InputIds[i] = ids[i].Trim();
            }

            _globalSearchInfo.SONG_IDS.Clear();
            _globalSearchInfo.SearchType = search_type_enum;
            _globalSearchInfo.OutputFileNameType = output_filename_type_enum;
            _globalSearchInfo.ShowLrcType = show_lrc_type_enum;
            _globalSearchInfo.Encoding = output_encoding_enum;
            _globalSearchInfo.DotType = dot_type_enum;
            _globalSearchInfo.OutputFileFormat = output_format_enum;
            _globalSearchInfo.LrcMergeSeparator = splitTextBox.Text;
        }

        /**
         * 从专辑ID中获取歌曲ID列表
         */
        private List<long> RequestSongIdInAlbum(long albumId, out string errorMsg)
        {
            var songIds = new List<long>();

            errorMsg = ErrorMsg.SUCCESS;

            var albumResult = _api.GetAlbum(albumId);
            var set = albumResult.Songs.Select(song => song.Id);
            songIds.AddRange(set);

            return songIds;
        }

        /**
         * 获取歌曲信息
         */
        private Dictionary<long, SongVo> RequestSongVo(long[] songIds, out Dictionary<long, string> errorMsgs)
        {
            var datumDict = _api.GetDatum(songIds);
            var songDict = _api.GetSongs(songIds);

            errorMsgs = new Dictionary<long, string>();
            var result = new Dictionary<long, SongVo>();

            foreach (var pair in datumDict)
            {
                var songId = pair.Key;
                var datum = pair.Value;

                if (!songDict.TryGetValue(songId, out var song))
                {
                    errorMsgs[songId] = ErrorMsg.SONG_NOT_EXIST;
                    continue;
                }

                result[songId] = new SongVo
                {
                    Links = datum.Url,
                    Name = song.Name,
                    Singer = NetEaseMusicUtils.ContractSinger(song.Ar),
                    Album = song.Al.Name,
                    DateTime = song.Dt
                };
                errorMsgs[songId] = ErrorMsg.SUCCESS;
            }

            return result;
        }

        /**
         * 根据歌曲ID查询
         */
        private void SearchBySongId(IEnumerable<long> songIds, out Dictionary<long, string> errorMsgDict)
        {
            errorMsgDict = new Dictionary<long, string>();

            var requestId = new List<long>();

            foreach (var songId in songIds)
            {
                if (NetEaseMusicCache.ContainsSaveVo(songId))
                {
                    errorMsgDict.Add(songId, ErrorMsg.SUCCESS);
                }
                else
                {
                    requestId.Add(songId);
                }
            }

            if (requestId.Count == 0)
            {
                return;
            }

            var requestResult = RequestSongVo(requestId.ToArray(), out var songVoErrorMsg);
            foreach (var errorPair in songVoErrorMsg)
            {
                var songId = errorPair.Key;
                var errorMsg = errorPair.Value;

                if (errorMsg == ErrorMsg.SUCCESS)
                {
                    var songVo = requestResult[songId];
                    var lyricVo = NetEaseMusicUtils.GetLyricVo(_api.GetLyric(songId), songVo.DateTime, _globalSearchInfo, out errorMsg);
                    if (errorMsg == ErrorMsg.SUCCESS)
                    {
                        NetEaseMusicCache.PutSaveVo(songId, new SaveVo(songId, songVo, lyricVo));
                        errorMsgDict.Add(songId, ErrorMsg.SUCCESS);
                        continue;
                    }
                }
                
                errorMsgDict.Add(songId, errorMsg);
            }
        }

        /**
         * 获取要输入的歌曲 ID 列表
         */
        private void InitInputSongIds(out string errorMsg)
        {
            errorMsg = ErrorMsg.SUCCESS;

            var inputs = _globalSearchInfo.InputIds;
            if (inputs.Length < 1)
            {
                errorMsg = ErrorMsg.INPUT_ID_ILLEGAG;
                return;
            }

            foreach (var input in inputs)
            {
                var id = NetEaseMusicUtils.CheckInputId(input, out errorMsg);

                if (errorMsg != ErrorMsg.SUCCESS)
                {
                    return;
                }

                if (_globalSearchInfo.SearchType == SEARCH_TYPE_ENUM.ALBUM_ID)
                {
                    var songIds = RequestSongIdInAlbum(id, out errorMsg);
                    if (errorMsg == ErrorMsg.SUCCESS)
                    {
                        foreach (var songId in songIds)
                        {
                            _globalSearchInfo.SONG_IDS.Add(songId);
                        }
                    }
                }
                else
                {
                    _globalSearchInfo.SONG_IDS.Add(id);
                }
            }
        }

        /**
         * 单个歌曲搜索
         */
        private void SingleSearch(long songId)
        {
            SearchBySongId(new[] { songId }, out var resultMaps);

            var message = resultMaps[songId];
            if (message != ErrorMsg.SUCCESS)
            {
                MessageBox.Show(message, "提示");
                return;
            }

            var result = NetEaseMusicCache.GetSaveVo(songId);

            // 3、加入结果集
            _globalSaveVoMap.Add(songId, result);

            // 4、前端设置
            textBox_song.Text = result.SongVo.Name;
            textBox_singer.Text = result.SongVo.Singer;
            textBox_album.Text = result.SongVo.Album;
            UpdateLrcTextBox(string.Empty);
        }

        /**
         * 批量歌曲搜索
         */
        private void BatchSearch(IEnumerable<long> ids)
        {
            SearchBySongId(ids, out var resultMaps);

            // 输出日志
            var log = new StringBuilder();

            foreach (var kvp in resultMaps)
            {
                var songId = kvp.Key;
                var message = kvp.Value;

                if (message == ErrorMsg.SUCCESS)
                {
                    _globalSaveVoMap.Add(songId, NetEaseMusicCache.GetSaveVo(songId));
                }

                log.Append("ID: " + songId + ", Result: " + message).Append(Environment.NewLine);
            }

            log.Append("---Total：" + resultMaps.Count + ", Success：" + _globalSaveVoMap.Count + ", Failure：" +
                       (resultMaps.Count - _globalSaveVoMap.Count) + "---").Append(Environment.NewLine);

            UpdateLrcTextBox(log.ToString());
        }

        /**
         * 搜索按钮点击事件
         */
        private void searchBtn_Click(object sender, EventArgs e)
        {
            ReloadConfig();
            CleanTextBox();
            _globalSaveVoMap.Clear();

            InitInputSongIds(out var errorMsg);
            if (errorMsg != ErrorMsg.SUCCESS)
            {
                MessageBox.Show(errorMsg, "提示");
                return;
            }

            var songIds = _globalSearchInfo.SONG_IDS;
            if (songIds.Count > 1)
            {
                BatchSearch(songIds);
            }
            else
            {
                // just loop once
                foreach (var songId in songIds)
                {
                    SingleSearch(songId);
                }
            }
        }

        /**
         * 获取直链点击事件
         */
        private void songUrlBtn_Click(object sender, EventArgs e)
        {
            if (_globalSaveVoMap == null || _globalSaveVoMap.Count == 0)
            {
                MessageBox.Show(ErrorMsg.MUST_SEARCH_BEFORE_COPY_SONG_URL, "提示");
                return;
            }

            var log = new StringBuilder();
            if (_globalSaveVoMap.Count > 1)
            {
                // 输出日志
                foreach (var songId in _globalSearchInfo.SONG_IDS)
                {
                    _globalSaveVoMap.TryGetValue(songId, out var saveVo);

                    var link = "failure";
                    if (saveVo != null)
                    {
                        link = saveVo.SongVo.Links ?? "failure";
                    }

                    log.Append($"ID: {songId}, Links: {link}").Append(Environment.NewLine);
                }

                UpdateLrcTextBox(log.ToString());
            }
            else
            {
                // only loop one times
                foreach (var item in _globalSaveVoMap)
                {
                    var link = item.Value.SongVo.Links;
                    if (link == null)
                    {
                        MessageBox.Show(ErrorMsg.SONG_URL_GET_FAILED, "提示");
                    }
                    else
                    {
                        Clipboard.SetDataObject(link);
                        MessageBox.Show(ErrorMsg.SONG_URL_COPY_SUCESS, "提示");
                    }
                }
            }
        }

        /**
         * 单个保存
         */
        private void SingleSave(long songId)
        {
            var saveDialog = new SaveFileDialog();
            try
            {
                if (!_globalSaveVoMap.TryGetValue(songId, out var saveVo))
                {
                    MessageBox.Show(ErrorMsg.MUST_SEARCH_BEFORE_SAVE, "提示");
                    return;
                }

                var outputFileName = NetEaseMusicUtils.GetOutputName(saveVo.SongVo, _globalSearchInfo);
                if (outputFileName == null)
                {
                    MessageBox.Show(ErrorMsg.FILE_NAME_IS_EMPTY, "提示");
                    return;
                }
                else
                {
                    outputFileName = NetEaseMusicUtils.GetSafeFilename(outputFileName);
                }

                saveDialog.FileName = outputFileName;
                saveDialog.Filter = output_format_enum.ToDescription();
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    var sw = new StreamWriter(saveDialog.FileName, false, NetEaseMusicUtils.GetEncoding(_globalSearchInfo.Encoding));
                    sw.Write(NetEaseMusicUtils.GetOutputContent(saveVo.LyricVo, _globalSearchInfo));
                    sw.Flush();
                    sw.Close();
                    MessageBox.Show(ErrorMsg.SAVE_SUCCESS, "提示");
                }
            }
            catch (Exception ew)
            {
                MessageBox.Show("保存失败！错误信息：\n" + ew.Message);
            }
        }

        /**
         * 批量保存
         */
        private void BatchSave()
        {
            var saveDialog = new SaveFileDialog();
            try
            {
                saveDialog.FileName = "直接选择保存路径即可，无需修改此处内容";
                saveDialog.Filter = output_format_enum.ToDescription();
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    string localFilePath = saveDialog.FileName;
                    // 获取文件后缀
                    string fileSuffix = localFilePath.Substring(localFilePath.LastIndexOf("."));
                    //获取文件路径，不带文件名 
                    string filePath = localFilePath.Substring(0, localFilePath.LastIndexOf("\\"));

                    foreach (var item in _globalSaveVoMap)
                    {
                        var saveVo = item.Value;
                        string outputFileName = NetEaseMusicUtils.GetOutputName(saveVo.SongVo, _globalSearchInfo);
                        string path = filePath + "/" + NetEaseMusicUtils.GetSafeFilename(outputFileName) + fileSuffix;
                        StreamWriter sw = new StreamWriter(path, false, NetEaseMusicUtils.GetEncoding(_globalSearchInfo.Encoding));
                        sw.Write(NetEaseMusicUtils.GetOutputContent(saveVo.LyricVo, _globalSearchInfo));
                        sw.Flush();
                        sw.Close();
                    }
                    MessageBox.Show(ErrorMsg.SAVE_SUCCESS, "提示");
                }
            }
            catch (Exception ew)
            {
                MessageBox.Show("批量保存失败，错误信息：\n" + ew.Message);
            }

            // 输出日志
            var log = new StringBuilder();
            foreach (var songId in _globalSearchInfo.SONG_IDS)
            {
                log
                    .Append($"ID: {songId}, Result: {(_globalSaveVoMap.ContainsKey(songId) ? "success" : "failure")}")
                    .Append(Environment.NewLine);
            }

            UpdateLrcTextBox(log.ToString());
        }

        /**
         * 保存按钮点击事件
         */
        private void saveBtn_Click(object sender, EventArgs e)
        {
            if (_globalSaveVoMap == null || _globalSaveVoMap.Count == 0)
            {
                MessageBox.Show(ErrorMsg.MUST_SEARCH_BEFORE_SAVE, "提示");
                return;
            }

            if (_globalSaveVoMap.Count > 1)
            {
                BatchSave();
            }
            else
            {
                // only loop one times
                foreach (var item in _globalSaveVoMap)
                {
                    SingleSave(item.Value.SongId);
                }
            }
        }

        private void comboBox_output_name_SelectedIndexChanged(object sender, EventArgs e)
        {
            output_filename_type_enum = (OUTPUT_FILENAME_TYPE_ENUM)comboBox_output_name.SelectedIndex;
            ReloadConfig();
        }

        private void comboBox_output_encode_SelectedIndexChanged(object sender, EventArgs e)
        {
            output_encoding_enum = (OUTPUT_ENCODING_ENUM)comboBox_output_encode.SelectedIndex;
            ReloadConfig();
        }

        private void comboBox_diglossia_lrc_SelectedIndexChanged(object sender, EventArgs e)
        {
            show_lrc_type_enum = (SHOW_LRC_TYPE_ENUM)comboBox_diglossia_lrc.SelectedIndex;

            if (show_lrc_type_enum == SHOW_LRC_TYPE_ENUM.MERGE_ORIGIN ||
                show_lrc_type_enum == SHOW_LRC_TYPE_ENUM.MERGE_TRANSLATE)
            {
                splitTextBox.ReadOnly = false;
                splitTextBox.BackColor = System.Drawing.Color.White;
            }
            else
            {
                splitTextBox.Text = null;
                splitTextBox.ReadOnly = true;
                splitTextBox.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
            }

            ReloadConfig();
            UpdateLrcTextBox(string.Empty);
        }

        private void comboBox_search_type_SelectedIndexChanged(object sender, EventArgs e)
        {
            search_type_enum = (SEARCH_TYPE_ENUM)comboBox_search_type.SelectedIndex;

            ReloadConfig();
            UpdateLrcTextBox(string.Empty);
        }

        private void comboBox_dot_SelectedIndexChanged(object sender, EventArgs e)
        {
            dot_type_enum = (DOT_TYPE_ENUM)comboBox_dot.SelectedIndex;
            ReloadConfig();
            UpdateLrcTextBox(string.Empty);
        }

        /**
         * 项目主页item
         */
        private void homeMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/jitwxs/163MusicLyrics");
        }

        /**
         * 最新版本item
         */
        private void latestVersionMenuItem_Click(object sender, EventArgs e)
        {
            // support https
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var headers = new Dictionary<string, string>
            {
                { "Accept", "application/vnd.github.v3+json" },
                { "User-Agent", NetEaseMusicApi._USERAGENT }
            };

            var jsonStr = HttpUtils.HttpGet("http://api.github.com/repos/jitwxs/163MusicLyrics/releases/latest",
                "application/json", headers);
            var obj = (JObject)JsonConvert.DeserializeObject(jsonStr);
            OutputLatestTag(obj["tag_name"]);
        }

        private static void OutputLatestTag(JToken latestTag)
        {
            if (latestTag == null)
            {
                MessageBox.Show(ErrorMsg.GET_LATEST_VERSION_FAILED, "提示");
            }
            else
            {
                string bigV = latestTag.ToString().Substring(1, 2), smallV = latestTag.ToString().Substring(3);
                string curBigV = Version.Substring(1, 2), curSmallV = Version.Substring(3);

                if (bigV.CompareTo(curBigV) == 1 || (bigV.CompareTo(curBigV) == 0 && smallV.CompareTo(curSmallV) == 1))
                {
                    Clipboard.SetDataObject("https://github.com/jitwxs/163MusicLyrics/releases");
                    MessageBox.Show(string.Format(ErrorMsg.EXIST_LATEST_VERSION, latestTag), "提示");
                }
                else
                {
                    MessageBox.Show(ErrorMsg.THIS_IS_LATEST_VERSION, "提示");
                }
            }
        }

        /**
         * 问题反馈item
         */
        private void issueMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/jitwxs/163MusicLyrics/issues");
        }

        /**
         * 使用手册
         */
        private void wikiItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/jitwxs/163MusicLyrics/wiki");
        }

        private void splitTextBox_TextChanged(object sender, EventArgs e)
        {
            ReloadConfig();
            UpdateLrcTextBox(string.Empty);
        }

        /**
         * 更新前端歌词
         */
        private void UpdateLrcTextBox(string replace)
        {
            if (replace != string.Empty)
            {
                textBox_lrc.Text = replace;
            }
            else
            {
                // 根据最新配置，更新输出歌词
                if (_globalSaveVoMap != null && _globalSaveVoMap.Count == 1)
                {
                    // only loop one times
                    foreach (var saveVo in _globalSaveVoMap.Values)
                    {
                        var outputContent = NetEaseMusicUtils.GetOutputContent(saveVo.LyricVo, _globalSearchInfo);
                        textBox_lrc.Text = string.IsNullOrEmpty(outputContent) ? ErrorMsg.LRC_NOT_EXIST : outputContent;
                    }
                }
            }
        }

        /**
         * 清空前端内容
         */
        private void CleanTextBox()
        {
            textBox_lrc.Text = string.Empty;
            textBox_song.Text = string.Empty;
            textBox_singer.Text = string.Empty;
            textBox_album.Text = string.Empty;
        }

        private void textBox_lrc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                ((TextBox)sender).SelectAll();
            }
        }

        private void comboBox_output_format_SelectedIndexChanged(object sender, EventArgs e)
        {
            output_format_enum = (OUTPUT_FORMAT_ENUM)comboBox_output_format.SelectedIndex;
            ReloadConfig();
        }
    }
}