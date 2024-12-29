using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MusicLyricApp.Api.Music;
using MusicLyricApp.Bean;
using MusicLyricApp.Exception;
using MusicLyricApp.Utils;
using Newtonsoft.Json;
using NLog;

namespace MusicLyricApp
{
    public partial class MainForm : MusicLyricForm
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, SaveVo> _globalSaveVoMap = new Dictionary<string, SaveVo>();

        private readonly SearchInfo _globalSearchInfo = new SearchInfo();

        private Dictionary<SearchSourceEnum, IMusicApi> _api;

        private SettingForm _settingForm;

        private UpgradeForm _upgradeForm;

        private ShortcutForm _shortcutForm;

        private BlurForm _blurForm;
        
        private ScalingFormConfig _scalingFormConfig;
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        public MainForm()
        {
            // 禁止多开
            var instance = new Mutex(true, "MutexName", out var isNotRunning);
            if (!isNotRunning)
            {
                Environment.Exit(1);
            }
            
            InitializeComponent();
            AfterInitializeComponent();

            InitialConfig();

            TrySetHighDpiFont("Segoe UI");
        }

        private void InitialConfig()
        {
            // 1、加载配置
            SettingBean settingBean;
            if (File.Exists(Constants.SettingPath))
            {
                var text = File.ReadAllText(Constants.SettingPath);
                settingBean = text.ToEntity<SettingBean>();
            }
            else
            {
                settingBean = new SettingBean();
            }

            _globalSearchInfo.SettingBean = settingBean;
            _globalSearchInfo.SettingBeanBackup = settingBean.ToJson().ToEntity<SettingBean>();
            
            // 2、配置应用
            var paramConfig = settingBean.Config.RememberParam ? settingBean.Param : new PersistParamBean();
            
            OutputEncoding_ComboBox.SelectedIndex = (int) paramConfig.Encoding;
            LrcType_ComboBox.SelectedIndex = (int) paramConfig.ShowLrcType;
            SearchSource_ComboBox.SelectedIndex = (int) paramConfig.SearchSource;
            SearchType_ComboBox.SelectedIndex = (int) paramConfig.SearchType;
            OutputFormat_CombBox.SelectedIndex = (int) paramConfig.OutputFileFormat;
            LrcMergeSeparator_TextBox.Text = paramConfig.LrcMergeSeparator;
            
            // 3、自动检查更新
            if (settingBean.Config.AutoCheckUpdate)
            {
                ThreadPool.QueueUserWorkItem(p => CheckLatestVersion(false));
            }
            
            // 4、初始化 _api
            _api = new Dictionary<SearchSourceEnum, IMusicApi>
            {
                { SearchSourceEnum.QQ_MUSIC, new QQMusicApi(() => _globalSearchInfo.SettingBean.Config.QQMusicCookie) },
                { SearchSourceEnum.NET_EASE_MUSIC, new NetEaseMusicApi(() => _globalSearchInfo.SettingBean.Config.NetEaseCookie) }
            };
        }

        private void TrySetHighDpiFont(string fontName)
        {
            //缩放比例大于100%才更改字体
            if (DeviceDpi <= 96) return;

            Font font = null;
            try
            {
                font = new Font(fontName, 9F, FontStyle.Regular, GraphicsUnit.Point);
            }
            catch (System.Exception)
            {
                // ignored
            }

            if (font == null || !fontName.Equals(font.Name)) return;

            var fieldInfos =  GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var fieldInfo in fieldInfos)
            {
                var fieldType = fieldInfo.FieldType;
                if ("System.Windows.Forms".Equals(fieldType.Namespace))
                {
                    try
                    {
                        var propertyInfo = fieldType.GetProperty("Font");
                        var obj = fieldInfo.GetValue(this);
                        propertyInfo.SetValue(obj, font);
                    }
                    catch (System.Exception)
                    {
                        // ignored
                    }
                }
            }
        }

        /// <summary>
        /// 读取搜索框并重新加载配置
        /// </summary>
        private void ReloadConfig()
        {
            ReloadInputIdText();

            _globalSearchInfo.SongIds.Clear();
            _globalSearchInfo.SettingBean.Param.LrcMergeSeparator = LrcMergeSeparator_TextBox.Text;
        }

        private void ReloadInputIdText()
        {
            var inputText = Search_Text.Text.Trim();
            var inputStrList = new List<string>();
            
            // 判断是否是目录
            if (Directory.Exists(inputText))
            {
                var searchSource = _globalSearchInfo.SettingBean.Param.SearchSource;
                var searchType = _globalSearchInfo.SettingBean.Param.SearchType;
                
                var directoryInfo = new DirectoryInfo(inputText);
                foreach (var info in directoryInfo.GetFileSystemInfos())
                {
                    if (info is DirectoryInfo)
                    {
                        // 文件夹，跳过处理，不做递归
                        continue;
                    }
                    else
                    {
                        var name = info.Name;

                        if (!string.IsNullOrWhiteSpace(info.Extension) && name.EndsWith(info.Extension))
                        {
                            name = name.Remove(name.Length - info.Extension.Length);
                        }

                        name = name.Trim();

                        try
                        {
                            // check filename is legal param
                            GlobalUtils.CheckInputId(name, searchSource, searchType);
                            inputStrList.Add(name);
                        }
                        catch (MusicLyricException ignore)
                        {
                        }
                    }
                }
            }
            else
            {
                // 不是目录，认为是实际的 ID
                foreach (var name in Search_Text.Text.Trim().Split(','))
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }
                    inputStrList.Add(name.Trim());
                }
            }
            
            _globalSearchInfo.InputIds = inputStrList.ToArray();
        }

        /// <summary>
        /// 根据歌曲ID查询
        /// </summary>
        /// <returns>songId, result</returns>
        private Dictionary<string, ResultVo<SaveVo>> SearchBySongId(List<SearchInfo.InputSongId> inputSongIds, bool isVerbatimLyric)
        {
            var resultDict = new Dictionary<string, ResultVo<SaveVo>>();

            var songDict = new Dictionary<SearchSourceEnum, List<string>>();
            var songIndexDict = new Dictionary<string, int>();
            
            for (var i = 0; i < inputSongIds.Count; i++)
            {
                var searchSource = inputSongIds[i].SearchSource;
                var songId = inputSongIds[i].SongId;

                if (!songDict.ContainsKey(searchSource))
                {
                    songDict.Add(searchSource, new List<string>());
                }
                
                songDict[searchSource].Add(songId);
                songIndexDict[songId] = i + 1;
            }
            
            foreach (var searchSourcePair in songDict)
            {
                var songResp = _api[searchSourcePair.Key].GetSongVo(searchSourcePair.Value.ToArray());
                
                foreach (var pair in songResp)
                {
                    var songId = pair.Key;
                    ResultVo<SaveVo> songResult;
                
                    try
                    {
                        var songVo = pair.Value.Assert().Data;
                        
                        var lyricVo = _api[searchSourcePair.Key].GetLyricVo(songVo.Id, songVo.DisplayId, isVerbatimLyric).Assert().Data;
                        lyricVo.Duration = songVo.Duration;

                        var index = songIndexDict[songId];

                        songResult = new ResultVo<SaveVo>(new SaveVo(index, songVo, lyricVo));
                    }
                    catch (WebException ex)
                    {
                        _logger.Error(ex, "SearchBySongId network error, songId: {SongId}, delay: {Delay}", songId, NetworkUtils.GetWebRoundtripTime(50));
                        songResult = ResultVo<SaveVo>.Failure(ErrorMsg.NETWORK_ERROR);
                    }
                    catch (System.Exception ex)
                    {
                        _logger.Error(ex, "SearchBySongId error, songId: {SongId}, message: {ErrorMsg}", songId, ex.Message);
                        songResult = ResultVo<SaveVo>.Failure(ex.Message);
                    }

                    resultDict.Add(songId, songResult);
                }
            }

            return resultDict;
        }

        /// <summary>
        /// 初始化输入的歌曲 ID 列表
        /// </summary>
        private List<SearchInfo.InputSongId> InitInputSongIds()
        {
            var inputs = _globalSearchInfo.InputIds;
            if (inputs.Length < 1)
            {
                throw new MusicLyricException(ErrorMsg.SEARCH_RESULT_EMPTY);
            }

            foreach (var input in inputs)
            {
                var searchSource = _globalSearchInfo.SettingBean.Param.SearchSource;
                var searchType = _globalSearchInfo.SettingBean.Param.SearchType;
                
                var baseInputSongId = GlobalUtils.CheckInputId(input, searchSource, searchType);
                
                switch (searchType)
                {
                    case SearchTypeEnum.ALBUM_ID:
                        foreach (var simpleSongVo in _api[searchSource].GetAlbumVo(baseInputSongId.QueryId).Assert().Data.SimpleSongVos)
                        {
                            _globalSearchInfo.SongIds.Add(new SearchInfo.InputSongId(simpleSongVo.DisplayId, baseInputSongId));
                        }
                        break;
                    case SearchTypeEnum.PLAYLIST_ID:
                        foreach (var simpleSongVo in _api[searchSource].GetPlaylistVo(baseInputSongId.QueryId).Assert().Data.SimpleSongVos)
                        {
                            _globalSearchInfo.SongIds.Add(new SearchInfo.InputSongId(simpleSongVo.DisplayId, baseInputSongId));
                        }
                        break;
                    case SearchTypeEnum.SONG_ID:
                        _globalSearchInfo.SongIds.Add(new SearchInfo.InputSongId(baseInputSongId.QueryId, baseInputSongId));
                        break;
                    default:
                        throw new MusicLyricException(ErrorMsg.SYSTEM_ERROR);
                }
            }
            
            return _globalSearchInfo.SongIds;
        }

        /// <summary>
        /// 单个歌曲搜索
        /// </summary>
        /// <param name="songIdDict">歌曲ID</param>
        private void SingleSearch(List<SearchInfo.InputSongId> songIdDict)
        {
            var isVerbatimLyric = _globalSearchInfo.SettingBean.Config.EnableVerbatimLyric;
            var singerSeparator = _globalSearchInfo.SettingBean.Config.SingerSeparator;

            var resDict = SearchBySongId(songIdDict, isVerbatimLyric);
            var songId = songIdDict.First().SongId;

            var result = resDict[songId].Assert().Data;
            
            // 加入结果集
            _globalSaveVoMap.Add(songId, result);

            // 前端设置
            SongName_TextBox.Text = result.SongVo.Name;
            Singer_TextBox.Text = string.Join(singerSeparator, result.SongVo.Singer);
            Album_TextBox.Text = result.SongVo.Album;
            UpdateLrcTextBox(string.Empty);
        }

        /// <summary>
        /// 批量歌曲搜索
        /// </summary>
        private void BatchSearch(List<SearchInfo.InputSongId> ids)
        {
            var isVerbatimLyric = _globalSearchInfo.SettingBean.Config.EnableVerbatimLyric;
            var resultMaps = SearchBySongId(ids, isVerbatimLyric);

            // 输出日志
            var log = new StringBuilder();

            foreach (var kvp in resultMaps)
            {
                var songId = kvp.Key;
                var resultVo = kvp.Value;

                log.Append($"{songId}");
                
                if (resultVo.IsSuccess())
                {
                    var saveVo = resultVo.Data;
                    _globalSaveVoMap.Add(songId, saveVo);
                    
                    log.Append($" => {saveVo.SongVo.Name}");
                }

                log
                    .Append($" => {resultVo.ErrorMsg}")
                    .Append(Environment.NewLine);
            }

            log
                .Append(Environment.NewLine)
                .Append($"累计 {resultMaps.Count} 成功 {_globalSaveVoMap.Count} 失败 {resultMaps.Count - _globalSaveVoMap.Count}")
                .Append(Environment.NewLine);

            UpdateLrcTextBox(log.ToString());
        }

        /// <summary>
        /// 搜索事件
        /// </summary>
        public async void Search_Btn_Click(object sender, EventArgs e)
        {
            Button input;
            if (sender is Button button)
            {
                input = button;
            }
            else
            {
                input = (Button) Controls.Find((string) sender, true)[0];
            }

            try
            {
                // 精确搜索
                if (input == Search_Btn)
                {
                    ReloadConfig();
                    CleanTextBox();
                    _globalSaveVoMap.Clear();
                    
                    var songIds = InitInputSongIds();
                    if (songIds.Count == 0)
                    {
                        throw new MusicLyricException(ErrorMsg.SEARCH_RESULT_EMPTY);
                    }
                    if (songIds.Count > 1)
                    {
                        BatchSearch(songIds);
                    }
                    else
                    { 
                        SingleSearch(songIds);
                    }
                } 
                // 模糊搜索
                else if (input == Blur_Search_Btn)
                {
                    var keyword = Search_Text.Text.Trim();
                    if (string.IsNullOrWhiteSpace(keyword))
                    {
                        throw new MusicLyricException(ErrorMsg.INPUT_CONENT_EMPLT);
                    }

                    var resultVoList = new List<SearchResultVo>();

                    var searchType = _globalSearchInfo.SettingBean.Param.SearchType;
                    if (_globalSearchInfo.SettingBean.Config.AggregatedBlurSearch)
                    {
                        foreach (SearchSourceEnum searchSource in Enum.GetValues(typeof(SearchSourceEnum)))
                        {
                            var one = _api[searchSource].Search(keyword, searchType);
                            if (one.IsSuccess())
                            {
                                resultVoList.Add(one.Data);
                            }
                        }
                    }
                    else
                    {
                        resultVoList.Add(_api[_globalSearchInfo.SettingBean.Param.SearchSource].Search(keyword, searchType).Assert().Data);
                    }

                    resultVoList.RemoveAll(one => one.IsEmpty());
                    
                    if (resultVoList.Count == 0)
                    {
                        throw new MusicLyricException(ErrorMsg.SEARCH_RESULT_EMPTY);
                    }
                    else
                    {
                        FormUtils.OpenForm(_blurForm, () => _blurForm = new BlurForm(resultVoList, this), this);
                    }
                }
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Search Network error, btn: {Btn}, delay: {Delay}", 
                    input.Name, await NetworkUtils.GetWebRoundtripTimeAsync());
                MessageBox.Show(ErrorMsg.NETWORK_ERROR, "网络错误");
            }
            catch (MusicLyricException ex)
            {
                _logger.Error("Search Business failed, btn: {Btn}, param: {SearchParam}, type: {SearchType}, message: {ErrorMsg}",
                    input.Name, Search_Text.Text, _globalSearchInfo.SettingBean.Param.SearchType, ex.Message);
                MessageBox.Show(ex.Message, "搜索错误");
            }
            catch (System.Exception ex)
            {
                _logger.Error(ex);
                MessageBox.Show(ErrorMsg.SYSTEM_ERROR, "系统错误");
            }
        }
        
        /// <summary>
        /// 获取歌曲链接按钮，点击事件
        /// </summary>
        private void SongLink_Btn_Click(object sender, EventArgs e)
        {
            if (_globalSaveVoMap == null || _globalSaveVoMap.Count == 0)
            {
                MessageBox.Show(ErrorMsg.MUST_SEARCH_BEFORE_GET_SONG_URL, "提示");
                return;
            }

            if (_globalSaveVoMap.Count > 1)
            {
                var csv = new CsvBean();
                
                csv.AddColumn("id");
                csv.AddColumn("songLink");
    
                foreach (var pair in _globalSearchInfo.SongIds)
                {
                    var songId = pair.SongId;

                    _globalSaveVoMap.TryGetValue(songId, out var saveVo);

                    csv.AddData(songId);
                    csv.AddData(_api[pair.SearchSource].GetSongLink(songId).Data);
                    csv.NextLine();
                }

                UpdateLrcTextBox(csv.ToString());
            }
            else
            {
                var link = _api[_globalSearchInfo.SettingBean.Param.SearchSource].GetSongLink(_globalSaveVoMap.Keys.First());
                if (link.IsSuccess())
                {
                    Clipboard.SetDataObject(link.Data);
                    MessageBox.Show(ErrorMsg.SONG_URL_GET_SUCCESS, "提示");
                }
                else
                {
                    MessageBox.Show(ErrorMsg.SONG_URL_GET_FAILED, "提示");
                }
            }
        }

        /// <summary>
        /// 获取歌曲封面按钮，点击事件
        /// </summary>
        private void SongPic_Btn_Click(object sender, EventArgs e)
        {
            if (_globalSaveVoMap == null || _globalSaveVoMap.Count == 0)
            {
                MessageBox.Show(ErrorMsg.MUST_SEARCH_BEFORE_GET_SONG_PIC, "提示");
                return;
            }

            if (_globalSaveVoMap.Count > 1)
            {
                var csv = new CsvBean();
                
                csv.AddColumn("id");
                csv.AddColumn("picLink");
                
                foreach (var songId in _globalSearchInfo.SongIds.Select(inputSongId => inputSongId.SongId))
                {
                    _globalSaveVoMap.TryGetValue(songId, out var saveVo);

                    csv.AddData(songId);
                    csv.AddData(saveVo == null ? string.Empty : saveVo.SongVo.Pics);
                    csv.NextLine();
                }

                UpdateLrcTextBox(csv.ToString());
            }
            else
            {
                // only loop one times
                foreach (var pic in _globalSaveVoMap.Select(item => item.Value.SongVo.Pics))
                {
                    if (pic == null)
                    {
                        MessageBox.Show(ErrorMsg.SONG_PIC_GET_FAILED, "提示");
                    }
                    else
                    {
                        Clipboard.SetDataObject(pic);
                        MessageBox.Show(ErrorMsg.SONG_PIC_GET_SUCCESS, "提示");
                    }
                }
            }
        }
        
        /**
         * 单个保存
         */
        private async void SingleSave()
        {
            var saveVo = _globalSaveVoMap.Values.First();

            // 没有歌词内容
            if (saveVo.LyricVo.IsEmpty())
            {
                MessageBox.Show(ErrorMsg.LRC_NOT_EXIST, "提示");
                return;
            }

            var config = _globalSearchInfo.SettingBean.Config;
            
            // 纯音乐跳过
            if (saveVo.LyricVo.IsPureMusic() && config.IgnorePureMusicInSave)
            {
                MessageBox.Show(ErrorMsg.PURE_MUSIC_IGNORE_SAVE, "提示");
                return;
            }
            
            var saveDialog = new SaveFileDialog();
            saveDialog.FileName = GlobalUtils.GetOutputName(saveVo, config.OutputFileNameFormat, config.SingerSeparator);
            saveDialog.Filter = _globalSearchInfo.SettingBean.Param.OutputFileFormat.ToDescription();

            if (saveDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            try
            {
                await WriteToFile(saveDialog.FileName, saveVo.LyricVo);

                MessageBox.Show(string.Format(ErrorMsg.SAVE_COMPLETE, 1, 0), "提示");
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
        private async void BatchSave()
        {
            var saveDialog = new SaveFileDialog();
            saveDialog.FileName = "直接选择保存路径即可，无需修改此处内容";
            saveDialog.Filter = _globalSearchInfo.SettingBean.Param.OutputFileFormat.ToDescription();

            if (saveDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            var config = _globalSearchInfo.SettingBean.Config;
            
            // 保存
            var skipCount = 0;
            var success = new HashSet<string>();
            
            try
            {
                var localFilePath = saveDialog.FileName;
                // 获取文件后缀
                var fileSuffix = GlobalUtils.GetSuffix(localFilePath);
                //获取文件路径，不带文件名 
                var filePath = localFilePath.Substring(0, localFilePath.LastIndexOf("\\", StringComparison.Ordinal));

                if (!await BatchSaveForUrl(filePath, success))
                {
                    foreach (var item in _globalSaveVoMap)
                    {
                        var saveVo = item.Value;
                        var lyricVo = saveVo.LyricVo;
                        if (lyricVo.IsEmpty() || (lyricVo.IsPureMusic() && config.IgnorePureMusicInSave))
                        {
                            skipCount++;
                            continue;
                        }

                        var path = filePath + '/' + GlobalUtils.GetOutputName(saveVo, config.OutputFileNameFormat, config.SingerSeparator) + fileSuffix;

                        await WriteToFile(path, lyricVo);
                        success.Add(item.Key);
                    }
                }
                
                MessageBox.Show(string.Format(ErrorMsg.SAVE_COMPLETE, success.Count, skipCount), "提示");
            }
            catch (System.Exception ew)
            {
                _logger.Error(ew, "批量保存失败");
                MessageBox.Show("批量保存失败，错误信息：\n" + ew.Message);
            }

            // 输出日志
            var log = new StringBuilder();
            foreach (var songId in _globalSearchInfo.SongIds.Select(inputSongId => inputSongId.SongId))
            {
                log
                    .Append($"{songId} => {(success.Contains(songId) ? "成功" : "跳过")}")
                    .Append(Environment.NewLine);
            }
            UpdateLrcTextBox(log.ToString());
        }
        
        private async Task<bool> BatchSaveForUrl(string filePath, ISet<string> success)
        {
            var csvBean = CsvBean.Deserialization(Console_TextBox.Text);

            int idIndex = -1, urlIndex = -1;
            
            for (var i = 0; i < csvBean.Title.Count; i++)
            {
                if (csvBean.Title[i].Contains("Link"))
                {
                    urlIndex = i;
                }
                else if (csvBean.Title[i].Equals("id"))
                {
                    idIndex = i;
                }
            }

            if (idIndex == -1 || urlIndex == -1)
            {
                return false;
            }

            var config = _globalSearchInfo.SettingBean.Config;

            foreach (var line in csvBean.Lines)
            {
                var url = line[urlIndex];
                if (string.IsNullOrWhiteSpace(url))
                {
                    continue;
                }
                
                var id = line[idIndex];
                if (!_globalSaveVoMap.TryGetValue(id, out var saveVo))
                {
                    continue;
                }
                
                var path = filePath + '/' + GlobalUtils.GetOutputName(saveVo, config.OutputFileNameFormat, config.SingerSeparator) + GlobalUtils.GetSuffix(url);

                if (await HttpUtils.DownloadFile(url, path))
                {
                    success.Add(id);
                }
            }

            return true;
        }

        private async Task WriteToFile(string path, LyricVo lyricVo)
        {
            var encoding = GlobalUtils.GetEncoding(_globalSearchInfo.SettingBean.Param.Encoding);

            var res = await LyricUtils.GetOutputContent(lyricVo, _globalSearchInfo);

            if (res.Count == 1)
            {
                using (var sw = new StreamWriter(path, false, encoding))
                {
                    await sw.WriteAsync(res[0]);
                    await sw.FlushAsync();
                }
            }
            else
            {
                var doxIndex = path.LastIndexOf(".", StringComparison.Ordinal);
                var suffix = path.Substring(doxIndex);
                path = path.Substring(0, doxIndex);
                
                for (var i = 0; i < res.Count; i++)
                {
                    using (var sw = new StreamWriter(path + " - " + i + suffix, false, encoding))
                    {
                        await sw.WriteAsync(res[i]);
                        await sw.FlushAsync();
                    }
                }
            }
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
                SingleSave();
            }
        }

        /**
         * 更新前端歌词
         */
        private async void UpdateLrcTextBox(string replace)
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
                    var lyricVo = _globalSaveVoMap.Values.First().LyricVo;
                    if (lyricVo.IsEmpty())
                    {
                        Console_TextBox.Text = ErrorMsg.LRC_NOT_EXIST;
                    }
                    else
                    {
                        Console_TextBox.Text = GlobalUtils.MergeStr(await LyricUtils.GetOutputContent(lyricVo, _globalSearchInfo));
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
            // 如果参数不记住，需要回滚
            if (!_globalSearchInfo.SettingBean.Config.RememberParam)
            {
                _globalSearchInfo.SettingBean.Param = _globalSearchInfo.SettingBeanBackup.Param;
            }
            
            // 配置持久化
            File.WriteAllText(Constants.SettingPath, _globalSearchInfo.SettingBean.ToJson(), Encoding.UTF8);
        }

        private void Search_Text_MouseEnter(object sender, EventArgs e)
        {
            if (GetActiveWindow() == Handle)
            {
                if (_globalSearchInfo.SettingBean.Config.AutoReadClipboard)
                {
                    Search_Text.Text = Clipboard.GetText();
                }
            }
        }

        /// <summary>
        /// 搜索来源下拉框，属性变更事件
        /// </summary>
        private void SearchSource_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _globalSearchInfo.SettingBean.Param.SearchSource = (SearchSourceEnum)SearchSource_ComboBox.SelectedIndex;

            ReloadConfig();
            UpdateLrcTextBox(string.Empty);
        }

        /// <summary>
        /// 搜索类型下拉框，属性变更事件
        /// </summary>
        private void SearchType_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _globalSearchInfo.SettingBean.Param.SearchType = (SearchTypeEnum)SearchType_ComboBox.SelectedIndex;

            ReloadConfig();
            UpdateLrcTextBox(string.Empty);
        }

        /// <summary>
        /// LRC歌词类型，属性变更事件
        /// </summary>
        private void LrcType_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (_globalSearchInfo.SettingBean.Param.ShowLrcType = (ShowLrcTypeEnum)LrcType_ComboBox.SelectedIndex)
            {
                case ShowLrcTypeEnum.MERGE:
                    LrcMergeSeparator_TextBox.ReadOnly = false;
                    LrcMergeSeparator_TextBox.BackColor = Color.White;
                    break;
                default: 
                    LrcMergeSeparator_TextBox.Text = null;
                    LrcMergeSeparator_TextBox.ReadOnly = true;
                    LrcMergeSeparator_TextBox.BackColor = Color.FromArgb(240, 240, 240);
                    break;
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
        /// 窗体键盘事件
        /// </summary>
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                // 保存操作
                Save_Btn_Click(sender, e);
            } else if (e.Control && e.KeyCode == Keys.Enter)
            {
                // 模糊搜索
                Search_Btn_Click(Blur_Search_Btn, e);
            } else if (e.KeyCode == Keys.Enter)
            {
                // 精确搜索
                Search_Btn_Click(Search_Btn, e);
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
                _globalSearchInfo.SettingBean.Param.Encoding = (OutputEncodingEnum)input.SelectedIndex;
            } 
            else if (input == OutputFormat_CombBox)
            {
                _globalSearchInfo.SettingBean.Param.OutputFileFormat = (OutputFormatEnum)input.SelectedIndex;
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
                FormUtils.OpenForm(_settingForm, () => _settingForm = new SettingForm(_globalSearchInfo.SettingBean), this);
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
            else if (input == ShortCut_MItem)
            {
                FormUtils.OpenForm(_shortcutForm, () => _shortcutForm = new ShortcutForm(), this);
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
                        FormUtils.OpenForm(_upgradeForm, () => _upgradeForm = new UpgradeForm(info), this);
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

        private void MainForm_Resize(object sender, EventArgs e)
        {
            _scalingFormConfig?.SetControls(this);
        }
    }
}