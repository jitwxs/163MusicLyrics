using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;
using MusicLyricApp.Api;
using MusicLyricApp.Bean;
using MusicLyricApp.Cache;
using MusicLyricApp.Exception;
using MusicLyricApp.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Application
{
    public partial class MainForm : Form
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, SaveVo> _globalSaveVoMap = new Dictionary<string, SaveVo>();

        private readonly SearchInfo _globalSearchInfo = new SearchInfo();

        // 输出文件编码
        private OutputEncodingEnum _outputEncodingEnum;

        // 搜索来源
        private SearchSourceEnum _searchSourceEnum;
        
        // 搜索类型
        private SearchTypeEnum _searchTypeEnum;

        // 强制两位类型
        private DotTypeEnum _dotTypeEnum;

        // 展示歌词类型
        private ShowLrcTypeEnum _showLrcTypeEnum;

        // 输出文件名类型
        private OutputFilenameTypeEnum _outputFilenameTypeEnum;

        // 输出文件类型
        private OutputFormatEnum _outputFormatEnum;

        public const string Version = "v4.1";

        private IMusicApiV2 _api;

        public MainForm()
        {
            InitializeComponent();

            comboBox_output_name.SelectedIndex = 0;
            comboBox_output_encode.SelectedIndex = 0;
            comboBox_diglossia_lrc.SelectedIndex = 0;
            comboBox_search_source.SelectedIndex = 0;
            comboBox_search_type.SelectedIndex = 0;
            comboBox_dot.SelectedIndex = 0;
            comboBox_output_format.SelectedIndex = 0;
        }

        /// <summary>
        /// 读取搜索框并重新加载配置
        /// </summary>
        private void ReloadConfig()
        {
            var ids = search_id_text.Text.Trim().Split(',');
            _globalSearchInfo.InputIds = new string[ids.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                _globalSearchInfo.InputIds[i] = ids[i].Trim();
            }

            _globalSearchInfo.SongIds.Clear();
            _globalSearchInfo.SearchSource = _searchSourceEnum;
            _globalSearchInfo.SearchType = _searchTypeEnum;
            _globalSearchInfo.OutputFileNameType = _outputFilenameTypeEnum;
            _globalSearchInfo.ShowLrcType = _showLrcTypeEnum;
            _globalSearchInfo.Encoding = _outputEncodingEnum;
            _globalSearchInfo.DotType = _dotTypeEnum;
            _globalSearchInfo.OutputFileFormat = _outputFormatEnum;
            _globalSearchInfo.LrcMergeSeparator = splitTextBox.Text;

            if (_searchSourceEnum == SearchSourceEnum.QQ_MUSIC)
            {
                _api = new QQMusicApiV2();
            }
            else
            {
                _api = new NetEaseMusicApiV2();
            }
        }

        /// <summary>
        /// 根据歌曲ID查询
        /// </summary>
        private void SearchBySongId(IEnumerable<string> songIds, out Dictionary<string, string> errorMsgDict)
        {
            errorMsgDict = new Dictionary<string, string>();

            // 1、优先加载缓存
            var requestId = new List<string>();

            foreach (var songId in songIds)
            {
                if (GlobalCache.ContainsSaveVo(songId))
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

            // 2、API 请求
            var songResp = _api.GetSongVo(requestId.ToArray(), out var songVoErrorMsg);
            foreach (var errorPair in songVoErrorMsg)
            {
                var songId = errorPair.Key;
                var errorMsg = errorPair.Value;

                if (errorMsg != ErrorMsg.SUCCESS)
                {
                    errorMsgDict.Add(songId, errorMsg);
                    continue;
                }
                
                try
                {
                    var songVo = songResp[songId];
                    var lyricVo = _api.GetLyricVo(songId);
                        
                    LyricUtils.FillingLyricVo(lyricVo, songVo, _globalSearchInfo);
                        
                    GlobalCache.PutSaveVo(songId, new SaveVo(songId, songVo, lyricVo));
                }
                catch (WebException ex)
                {
                    _logger.Error(ex, "SearchBySongId network error, delay: {Delay}", NetworkUtils.GetWebRoundtripTime(50));
                    errorMsg = ErrorMsg.NETWORK_ERROR;
                }
                catch (System.Exception ex)
                {
                    _logger.Error(ex, "SearchBySongId error, songId: {SongId}, message: {ErrorMsg}", songId, errorMsg);
                    errorMsg = ex.Message;
                }

                errorMsgDict.Add(songId, errorMsg);
            }
        }

        /// <summary>
        /// 初始化输入的歌曲 ID 列表
        /// </summary>
        private void InitInputSongIds()
        {
            var inputs = _globalSearchInfo.InputIds;
            if (inputs.Length < 1)
            {
                throw new MusicLyricException(ErrorMsg.INPUT_ID_ILLEGAL);
            }

            foreach (var input in inputs)
            {
                var id = GlobalUtils.CheckInputId(input, _globalSearchInfo.SearchSource, _globalSearchInfo.SearchType);
                switch (_globalSearchInfo.SearchType)
                {
                    case SearchTypeEnum.ALBUM_ID:
                        foreach (var songId in _api.GetSongIdsFromAlbum(id))
                        {
                            _globalSearchInfo.SongIds.Add(songId);
                        }

                        break;
                    case SearchTypeEnum.SONG_ID:
                        _globalSearchInfo.SongIds.Add(id);
                        break;
                    default:
                        throw new MusicLyricException(ErrorMsg.SYSTEM_ERROR);
                }
            }
        }

        /// <summary>
        /// 单个歌曲搜索
        /// </summary>
        /// <param name="songId">歌曲ID</param>
        /// <param name="errorMsg">错误信息</param>
        /// <exception cref="WebException"></exception>
        private void SingleSearch(string songId, out string errorMsg)
        {
            SearchBySongId(new[] { songId }, out var resultMaps);

            var message = resultMaps[songId];
            errorMsg = message;
            if (message != ErrorMsg.SUCCESS)
            {
                return;
            }

            var result = GlobalCache.GetSaveVo(songId);

            // 3、加入结果集
            _globalSaveVoMap.Add(songId, result);

            // 4、前端设置
            textBox_song.Text = result.SongVo.Name;
            textBox_singer.Text = result.SongVo.Singer;
            textBox_album.Text = result.SongVo.Album;
            UpdateLrcTextBox(string.Empty);
        }


        /// <summary>
        /// 批量歌曲搜索
        /// </summary>
        /// <param name="ids"></param>
        /// <exception cref="WebException"></exception>
        private void BatchSearch(IEnumerable<string> ids)
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
                    _globalSaveVoMap.Add(songId, GlobalCache.GetSaveVo(songId));
                }

                log.Append($"ID: {songId}, Result: {message}").Append(Environment.NewLine);
            }

            log.Append("---Total：" + resultMaps.Count + ", Success：" + _globalSaveVoMap.Count + ", Failure：" +
                       (resultMaps.Count - _globalSaveVoMap.Count) + "---").Append(Environment.NewLine);

            UpdateLrcTextBox(log.ToString());
        }

        /**
         * 搜索按钮点击事件
         */
        private async void searchBtn_Click(object sender, EventArgs e)
        {
            ReloadConfig();
            CleanTextBox();
            _globalSaveVoMap.Clear();

            try
            {
                InitInputSongIds();

                var songIds = _globalSearchInfo.SongIds;
                if (songIds.Count > 1)
                {
                    BatchSearch(songIds);
                }
                else
                {
                    // just loop once
                    foreach (var songId in songIds)
                    {
                        SingleSearch(songId, out var errorMsg);
                        if (errorMsg != ErrorMsg.SUCCESS)
                        {
                            MessageBox.Show(errorMsg, "提示");
                            return;
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Search Network error, delay: {Delay}", await NetworkUtils.GetWebRoundtripTimeAsync());
                MessageBox.Show(ErrorMsg.NETWORK_ERROR, "错误");
            }
            catch (MusicLyricException ex)
            {
                _logger.Error("Search Business failed, param: {SearchParam}, type: {SearchType}, message: {ErrorMsg}",
                    search_id_text.Text, _globalSearchInfo.SearchType, ex.Message);
                MessageBox.Show(ex.Message, "提示");
            }
            catch (System.Exception ex)
            {
                _logger.Error(ex);
                MessageBox.Show(ErrorMsg.SYSTEM_ERROR, "错误");
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
                foreach (var songId in _globalSearchInfo.SongIds)
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
                        MessageBox.Show(ErrorMsg.SONG_URL_COPY_SUCCESS, "提示");
                    }
                }
            }
        }

        /**
         * 单个保存
         */
        private void SingleSave(string songId)
        {
            var saveDialog = new SaveFileDialog();
            try
            {
                if (!_globalSaveVoMap.TryGetValue(songId, out var saveVo))
                {
                    MessageBox.Show(ErrorMsg.MUST_SEARCH_BEFORE_SAVE, "提示");
                    return;
                }

                var outputFileName = GlobalUtils.GetOutputName(saveVo.SongVo, _globalSearchInfo);
                if (outputFileName == null)
                {
                    MessageBox.Show(ErrorMsg.FILE_NAME_IS_EMPTY, "提示");
                    return;
                }
                else
                {
                    outputFileName = GlobalUtils.GetSafeFilename(outputFileName);
                }

                saveDialog.FileName = outputFileName;
                saveDialog.Filter = _outputFormatEnum.ToDescription();
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    using (var sw = new StreamWriter(saveDialog.FileName, false,
                               GlobalUtils.GetEncoding(_globalSearchInfo.Encoding)))
                    {
                        sw.Write(LyricUtils.GetOutputContent(saveVo.LyricVo, _globalSearchInfo));
                        sw.Flush();
                    }

                    MessageBox.Show(ErrorMsg.SAVE_SUCCESS, "提示");
                }
            }
            catch (System.Exception ew)
            {
                _logger.Error(ew, "单独保存歌词失败");
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
                saveDialog.Filter = _outputFormatEnum.ToDescription();
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
                        string outputFileName = GlobalUtils.GetOutputName(saveVo.SongVo, _globalSearchInfo);
                        string path = filePath + '/' + GlobalUtils.GetSafeFilename(outputFileName) + fileSuffix;
                        StreamWriter sw = new StreamWriter(path, false,
                            GlobalUtils.GetEncoding(_globalSearchInfo.Encoding));
                        sw.Write(LyricUtils.GetOutputContent(saveVo.LyricVo, _globalSearchInfo));
                        sw.Flush();
                        sw.Close();
                    }

                    MessageBox.Show(ErrorMsg.SAVE_SUCCESS, "提示");
                }
            }
            catch (System.Exception ew)
            {
                _logger.Error(ew, "批量保存失败");
                MessageBox.Show("批量保存失败，错误信息：\n" + ew.Message);
            }

            // 输出日志
            var log = new StringBuilder();
            foreach (var songId in _globalSearchInfo.SongIds)
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
            _outputFilenameTypeEnum = (OutputFilenameTypeEnum)comboBox_output_name.SelectedIndex;
            ReloadConfig();
        }

        private void comboBox_output_encode_SelectedIndexChanged(object sender, EventArgs e)
        {
            _outputEncodingEnum = (OutputEncodingEnum)comboBox_output_encode.SelectedIndex;
            ReloadConfig();
        }

        private void comboBox_diglossia_lrc_SelectedIndexChanged(object sender, EventArgs e)
        {
            _showLrcTypeEnum = (ShowLrcTypeEnum)comboBox_diglossia_lrc.SelectedIndex;

            if (_showLrcTypeEnum == ShowLrcTypeEnum.MERGE_ORIGIN ||
                _showLrcTypeEnum == ShowLrcTypeEnum.MERGE_TRANSLATE)
            {
                splitTextBox.ReadOnly = false;
                splitTextBox.BackColor = Color.White;
            }
            else
            {
                splitTextBox.Text = null;
                splitTextBox.ReadOnly = true;
                splitTextBox.BackColor = Color.FromArgb(240, 240, 240);
            }

            ReloadConfig();
            UpdateLrcTextBox(string.Empty);
        }
        
        private void comboBox_search_source_SelectedIndexChanged(object sender, EventArgs e)
        {
            _searchSourceEnum = (SearchSourceEnum)comboBox_search_source.SelectedIndex;

            ReloadConfig();
            UpdateLrcTextBox(string.Empty);
        }

        private void comboBox_search_type_SelectedIndexChanged(object sender, EventArgs e)
        {
            _searchTypeEnum = (SearchTypeEnum)comboBox_search_type.SelectedIndex;

            ReloadConfig();
            UpdateLrcTextBox(string.Empty);
        }

        private void comboBox_dot_SelectedIndexChanged(object sender, EventArgs e)
        {
            _dotTypeEnum = (DotTypeEnum)comboBox_dot.SelectedIndex;
            ReloadConfig();
            UpdateLrcTextBox(string.Empty);
        }

        /**
         * 项目主页item
         */
        private async void homeMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("https://github.com/jitwxs/163MusicLyrics");
            }
            catch (System.Exception ex)
            {
                _logger.Error(ex, "项目主页打开失败,网络延迟: {0}", await NetworkUtils.GetWebRoundtripTimeAsync());
                MessageBox.Show("项目主页打开失败", "错误");
            }
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
                { "User-Agent", BaseNativeApi.Useragent }
            };

            try
            {
                var jsonStr = HttpUtils.HttpGet("https://api.github.com/repos/jitwxs/163MusicLyrics/releases/latest",
                    "application/json", headers);
                var obj = (JObject)JsonConvert.DeserializeObject(jsonStr);
                OutputLatestTag(obj["tag_name"]);
            }
            catch (HttpRequestException ex)
            {
                _logger.Error(ex);
                MessageBox.Show("网络错误", "错误");
            }
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
            try
            {
                Process.Start("https://github.com/jitwxs/163MusicLyrics/issues");
            }
            catch (System.Exception ex)
            {
                _logger.Error(ex, "问题反馈网址打开失败");
                MessageBox.Show("问题反馈网址打开失败", "错误");
            }
        }

        /**
         * 使用手册
         */
        private void wikiItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("https://github.com/jitwxs/163MusicLyrics/wiki");
            }
            catch (System.Exception ex)
            {
                _logger.Error(ex, "使用手册网址打开失败");
                MessageBox.Show("使用手册网址打开失败", "错误");
            }
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
                        var outputContent = LyricUtils.GetOutputContent(saveVo.LyricVo, _globalSearchInfo);
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
            _outputFormatEnum = (OutputFormatEnum)comboBox_output_format.SelectedIndex;
            ReloadConfig();
        }
    }
}