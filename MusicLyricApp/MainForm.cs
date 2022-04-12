using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MusicLyricApp.Api;
using MusicLyricApp.Bean;
using MusicLyricApp.Cache;
using MusicLyricApp.Exception;
using MusicLyricApp.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace MusicLyricApp
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

        private IMusicApiV2 _api;

        private SettingForm _settingForm;

        private SettingBean _settingBean;

        private UpgradeForm _upgradeForm;

        public MainForm()
        {
            // 禁止多开
            var instance = new Mutex(true, "MutexName", out var isNotRunning);
            if (!isNotRunning)
            {
                Environment.Exit(1);
            }
            
            InitializeComponent();
            InitialConfig();
        }

        private void InitialConfig()
        {
            // 1、加载配置
            if (File.Exists(Constants.SettingPath))
            {
                var text = File.ReadAllText(Constants.SettingPath);
                _settingBean = text.ToEntity<SettingBean>();
            }
            else
            {
                _settingBean = new SettingBean();
            }
            
            // 2、配置应用
            var paramConfig = _settingBean.Config.RememberParam ? _settingBean.Param : new PersistParamBean();
            OutputName_ComboBox.SelectedIndex = (int) paramConfig.OutputFileNameType;
            OutputEncoding_ComboBox.SelectedIndex = (int) paramConfig.Encoding;
            LrcType_ComboBox.SelectedIndex = (int) paramConfig.ShowLrcType;
            SearchSource_ComboBox.SelectedIndex = (int) paramConfig.SearchSource;
            SearchType_ComboBox.SelectedIndex = (int) paramConfig.SearchType;
            Dot_TextBox.SelectedIndex = (int) paramConfig.DotType;
            OutputFormat_CombBox.SelectedIndex = (int) paramConfig.OutputFileFormat;
            LrcMergeSeparator_TextBox.Text = paramConfig.LrcMergeSeparator;
            
            // 3、自动检查更新
            if (_settingBean.Config.AutoCheckUpdate)
            {
                ThreadPool.QueueUserWorkItem(p => CheckLatestVersion(false));
            }
        }

        /// <summary>
        /// 读取搜索框并重新加载配置
        /// </summary>
        private void ReloadConfig()
        {
            var ids = Search_Text.Text.Trim().Split(',');
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
            _globalSearchInfo.LrcMergeSeparator = LrcMergeSeparator_TextBox.Text;

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
                    
                    lyricVo.Duration = songVo.Duration;
                        
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
            SongName_TextBox.Text = result.SongVo.Name;
            Singer_TextBox.Text = result.SongVo.Singer;
            Album_TextBox.Text = result.SongVo.Album;
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

        /// <summary>
        /// 搜索按钮，点击事件
        /// </summary>
        private async void Search_Btn_Click(object sender, EventArgs e)
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
                    Search_Text.Text, _globalSearchInfo.SearchType, ex.Message);
                MessageBox.Show(ex.Message, "提示");
            }
            catch (System.Exception ex)
            {
                _logger.Error(ex);
                MessageBox.Show(ErrorMsg.SYSTEM_ERROR, "错误");
            }
        }

        /// <summary>
        /// 获取直链按钮，点击事件
        /// </summary>
        private void SongLink_Btn_Click(object sender, EventArgs e)
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
            // 没有搜索结果
            if (!_globalSaveVoMap.TryGetValue(songId, out var saveVo))
            {
                MessageBox.Show(ErrorMsg.MUST_SEARCH_BEFORE_SAVE, "提示");
                return;
            }

            // 没有歌词内容
            if (saveVo.LyricVo.IsEmpty())
            {
                MessageBox.Show(ErrorMsg.LRC_NOT_EXIST, "提示");
                return;
            }
            
            var saveDialog = new SaveFileDialog();
            try
            {
                saveDialog.FileName = GlobalUtils.GetOutputName(saveVo.SongVo, _globalSearchInfo);
                saveDialog.Filter = _outputFormatEnum.ToDescription();
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    using (var sw = new StreamWriter(saveDialog.FileName, false,
                               GlobalUtils.GetEncoding(_globalSearchInfo.Encoding)))
                    {
                        sw.Write(LyricUtils.GetOutputContent(saveVo.LyricVo, _globalSearchInfo));
                        sw.Flush();
                    }

                    MessageBox.Show(string.Format(ErrorMsg.SAVE_COMPLETE, 1, 1), "提示");
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
            var skipCount = 0;
            var success = new HashSet<string>();
            
            try
            {
                saveDialog.FileName = "直接选择保存路径即可，无需修改此处内容";
                saveDialog.Filter = _outputFormatEnum.ToDescription();
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    var localFilePath = saveDialog.FileName;
                    // 获取文件后缀
                    var fileSuffix = localFilePath.Substring(localFilePath.LastIndexOf("."));
                    //获取文件路径，不带文件名 
                    var filePath = localFilePath.Substring(0, localFilePath.LastIndexOf("\\"));

                    
                    foreach (var item in _globalSaveVoMap)
                    {
                        var saveVo = item.Value;
                        var lyricVo = saveVo.LyricVo;
                        if (lyricVo.IsEmpty())
                        {
                            skipCount++;
                            continue;
                        }

                        var path = filePath + '/' + GlobalUtils.GetOutputName(saveVo.SongVo, _globalSearchInfo) + fileSuffix;
                        using( var sw = new StreamWriter(path, false, GlobalUtils.GetEncoding(_globalSearchInfo.Encoding)))
                        {
                            sw.Write(LyricUtils.GetOutputContent(lyricVo, _globalSearchInfo));
                            sw.Flush();
                            success.Add(item.Key);
                        }
                    }
                }
            }
            catch (System.Exception ew)
            {
                _logger.Error(ew, "批量保存失败");
                MessageBox.Show("批量保存失败，错误信息：\n" + ew.Message);
            }
            
            MessageBox.Show(string.Format(ErrorMsg.SAVE_COMPLETE, success.Count, skipCount), "提示");

            // 输出日志
            var log = new StringBuilder();
            foreach (var songId in _globalSearchInfo.SongIds)
            {
                log
                    .Append($"ID: {songId}, Result: {(success.Contains(songId) ? "success" : "failure")}")
                    .Append(Environment.NewLine);
            }
            UpdateLrcTextBox(log.ToString());
        }

        /**
         * 保存按钮点击事件
         */
        private void Save_Btn_Click(object sender, EventArgs e)
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

        /**
         * 更新前端歌词
         */
        private void UpdateLrcTextBox(string replace)
        {
            if (replace != string.Empty)
            {
                Console_TextBox.Text = replace;
            }
            else
            {
                // 根据最新配置，更新输出歌词
                if (_globalSaveVoMap != null && _globalSaveVoMap.Count == 1)
                {
                    // only loop one times
                    foreach (var lyricVo in _globalSaveVoMap.Values.Select(saveVo => saveVo.LyricVo))
                    {
                        Console_TextBox.Text = lyricVo.IsEmpty() ? ErrorMsg.LRC_NOT_EXIST : LyricUtils.GetOutputContent(lyricVo, _globalSearchInfo);
                    }
                }
            }
        }

        /**
         * 清空前端内容
         */
        private void CleanTextBox()
        {
            Console_TextBox.Text = string.Empty;
            SongName_TextBox.Text = string.Empty;
            Singer_TextBox.Text = string.Empty;
            Album_TextBox.Text = string.Empty;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 记住最终的参数配置
            if (_settingBean.Config.RememberParam)
            {
                _settingBean.Param.Update(_globalSearchInfo);
            }
            
            // 配置持久化
            File.WriteAllText(Constants.SettingPath, _settingBean.ToJson(), Encoding.UTF8);
        }

        private void MainForm_MouseEnter(object sender, EventArgs e)
        {
            if (_settingBean.Config.AutoReadClipboard)
            {
                Search_Text.Text = Clipboard.GetText();
            }
        }

        /// <summary>
        /// 搜索来源下拉框，属性变更事件
        /// </summary>
        private void SearchSource_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _searchSourceEnum = (SearchSourceEnum)SearchSource_ComboBox.SelectedIndex;

            ReloadConfig();
            UpdateLrcTextBox(string.Empty);
        }

        /// <summary>
        /// 搜索类型下拉框，属性变更事件
        /// </summary>
        private void SearchType_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _searchTypeEnum = (SearchTypeEnum)SearchType_ComboBox.SelectedIndex;

            ReloadConfig();
            UpdateLrcTextBox(string.Empty);
        }

        /// <summary>
        /// LRC歌词类型，属性变更事件
        /// </summary>
        private void LrcType_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _showLrcTypeEnum = (ShowLrcTypeEnum)LrcType_ComboBox.SelectedIndex;

            if (_showLrcTypeEnum == ShowLrcTypeEnum.MERGE_ORIGIN ||
                _showLrcTypeEnum == ShowLrcTypeEnum.MERGE_TRANSLATE)
            {
                LrcMergeSeparator_TextBox.ReadOnly = false;
                LrcMergeSeparator_TextBox.BackColor = Color.White;
            }
            else
            {
                LrcMergeSeparator_TextBox.Text = null;
                LrcMergeSeparator_TextBox.ReadOnly = true;
                LrcMergeSeparator_TextBox.BackColor = Color.FromArgb(240, 240, 240);
            }

            ReloadConfig();
            UpdateLrcTextBox(string.Empty);
        }

        /// <summary>
        /// LRC歌词合并符，内容变更事件
        /// </summary>
        private void LrcMergeSeparator_TextBox_TextChanged(object sender, EventArgs e)
        {
            ReloadConfig();
            UpdateLrcTextBox(string.Empty);
        }

        /// <summary>
        /// 小数位配置，变更事件
        /// </summary>
        private void Dot_TextBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _dotTypeEnum = (DotTypeEnum)Dot_TextBox.SelectedIndex;
            ReloadConfig();
            UpdateLrcTextBox(string.Empty);
        }

        /// <summary>
        /// 控制台，键盘事件
        /// </summary>
        private void Console_TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 全选操作
            if (e.Control && e.KeyCode == Keys.A)
            {
                ((TextBox)sender).SelectAll();
            }
        }

        /// <summary>
        /// 配置下拉框，属性变更事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Config_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(sender is ComboBox input))
            {
                throw new MusicLyricException(ErrorMsg.SYSTEM_ERROR);
            }
            
            if (input == OutputEncoding_ComboBox)
            {
                _outputEncodingEnum = (OutputEncodingEnum)input.SelectedIndex;
            } 
            else if (input == OutputFormat_CombBox)
            {
                _outputFormatEnum = (OutputFormatEnum)input.SelectedIndex;
            }
            else if (input == OutputName_ComboBox)
            {
                _outputFilenameTypeEnum = (OutputFilenameTypeEnum)input.SelectedIndex;
            }
            
            ReloadConfig();
        }

        /// <summary>
        /// 上方菜单元素，点击事件
        /// </summary>
        private async void Top_MItem_Click(object sender, EventArgs e)
        {
            if (!(sender is ToolStripMenuItem input))
            {
                throw new MusicLyricException(ErrorMsg.SYSTEM_ERROR);
            }
            
            if (input == Home_MItem)
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
            else if (input == Wiki_MItem)
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
            else if (input == Setting_MItem)
            {
                if (_settingForm == null || _settingForm.IsDisposed)
                {
                    _settingForm = new SettingForm(_settingBean.Config);
                    _settingForm.Location = new Point(Left + Constants.SettingFormOffset, Top + Constants.SettingFormOffset);
                    _settingForm.StartPosition = FormStartPosition.Manual;
                    _settingForm.Show();
                }
                else
                {
                    _settingForm.Activate();
                }
            }
            else if (input == Issue_MItem)
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
            else if (input == CheckVersion_MItem)
            {
                ThreadPool.QueueUserWorkItem(p => CheckLatestVersion(true));
            }
        }

        private bool _inCheckVersion;

        private bool _showMessageIfNotExistLatestVersion;

        private void CheckLatestVersion(bool showMessageIfNotExistLatestVersion)
        {
            _showMessageIfNotExistLatestVersion = showMessageIfNotExistLatestVersion;
            
            if (_inCheckVersion)
            {
                return;
            }

            _inCheckVersion = true;

            try
            {
                // support https
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var jsonStr = HttpUtils.HttpGetAsync(
                    "https://api.github.com/repos/jitwxs/163MusicLyrics/releases/latest", 
                    "application/json", 
                    new Dictionary<string, string>
                    {
                        { "Accept", "application/vnd.github.v3+json" },
                        { "User-Agent", BaseNativeApi.Useragent }
                    }).Result;

                var info = JsonConvert.DeserializeObject<GitHubInfo>(jsonStr);
                if (info == null)
                {
                    MessageBox.Show(ErrorMsg.GET_LATEST_VERSION_FAILED, "提示");
                    return;
                }

                if (info.Message != null && info.Message.Contains("API rate limit"))
                {
                    MessageBox.Show(ErrorMsg.API_RATE_LIMIT, "提示");
                    return;
                }

                string bigV = info.TagName.Substring(1, 2), smallV = info.TagName.Substring(3);
                string curBigV = Constants.Version.Substring(1, 2), curSmallV = Constants.Version.Substring(3);

                if (bigV.CompareTo(curBigV) == 1 || (bigV.CompareTo(curBigV) == 0 && smallV.CompareTo(curSmallV) == 1))
                {
                    void Action()
                    {
                        if (_upgradeForm == null || _upgradeForm.IsDisposed)
                        {
                            _upgradeForm = new UpgradeForm(info);
                            _upgradeForm.Location = new Point(Left + Constants.SettingFormOffset, Top + Constants.SettingFormOffset);
                            _upgradeForm.StartPosition = FormStartPosition.Manual;
                            _upgradeForm.Show();
                        }
                        else
                        {
                            _upgradeForm.Activate();
                        }
                    }
                    Invoke((Action)Action);
                }
                else if (_showMessageIfNotExistLatestVersion)
                {
                    MessageBox.Show(ErrorMsg.THIS_IS_LATEST_VERSION, "提示");
                }
            }
            catch (System.Exception ex)
            {
                _logger.Error(ex);
                MessageBox.Show(ErrorMsg.NETWORK_ERROR, "提示");
            }
            finally
            {
                _inCheckVersion = false;
            }
        }
    }
}