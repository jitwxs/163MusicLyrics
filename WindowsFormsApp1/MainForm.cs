using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using 网易云歌词提取;

namespace WindowsFormsApp1
{
    public partial class MainForm : Form
    {
        NeteaseMusicAPI api = null;
        Dictionary<string, SaveVO> globalSaveVOMap = null;
        SearchInfo globalSearchInfo = new SearchInfo();

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

        const string VERSION = "v3.2";

        public MainForm()
        {
            InitializeComponent();
            
            comboBox_output_name.SelectedIndex = 0;
            comboBox_output_encode.SelectedIndex = 0;
            comboBox_diglossia_lrc.SelectedIndex = 0;
            comboBox_search_type.SelectedIndex = 0;
            comboBox_dot.SelectedIndex = 0;

            api = new NeteaseMusicAPI();
        }

        private void ReloadConfig()
        {
            globalSearchInfo.SearchIds = search_id_text.Text.Trim().Split(',');
            globalSearchInfo.SerchType = search_type_enum;
            globalSearchInfo.OutputFileNameType = output_filename_type_enum;
            globalSearchInfo.ShowLrcType = show_lrc_type_enum;
            globalSearchInfo.Encoding = output_encoding_enum;
            globalSearchInfo.DotType = dot_type_enum;
            globalSearchInfo.LrcMergeSeparator = splitTextBox.Text;
        }

        private SongVO RequestSongVO(long songId, out string errorMsg)
        {
            SongUrls songUrls = api.GetSongsUrl(new long[] { songId });
            DetailResult detailResult = api.GetDetail(songId);

            return NeteaseMusicUtils.GetSongVO(songUrls, detailResult, out errorMsg);
        }

        private LyricVO RequestLyricVO(long songId, SearchInfo searchInfo, out string errorMsg)
        {
            LyricResult lyricResult = api.GetLyric(songId);
            return NeteaseMusicUtils.GetLyricVO(lyricResult, searchInfo, out errorMsg);
        }

        // 单个歌曲搜索
        private void SingleSearchBySongId(string songIdStr)
        {
            // 1、参数校验
            long songId = NeteaseMusicUtils.CheckInputId(songIdStr, SEARCH_TYPE_ENUM.SONG_ID, out string inputErrorMsg);
            if (inputErrorMsg != ErrorMsg.SUCCESS)
            {
                MessageBox.Show(inputErrorMsg, "提示");
                return;
            }

            // 2、获取歌曲内容
            SaveVO result;
            if (NeteaseMusicResultCache.Contains(songId))
            {
                result = NeteaseMusicResultCache.Get(songId);
            }
            else
            {
                SongVO songVO = RequestSongVO(songId, out string songErrorMsg);
                if (songErrorMsg != ErrorMsg.SUCCESS)
                {
                    MessageBox.Show(songErrorMsg, "提示");
                    return;
                }
                
                LyricVO lyricVO = RequestLyricVO(songId, globalSearchInfo, out string lyricErrorMsg);
                if (lyricErrorMsg != ErrorMsg.SUCCESS)
                {
                    MessageBox.Show(lyricErrorMsg, "提示");
                    return;
                }

                result = new SaveVO(songIdStr, songVO, lyricVO);
                NeteaseMusicResultCache.Put(songId, result);
            }

            // 3、加入结果集
            globalSaveVOMap.Add(songIdStr, NeteaseMusicResultCache.Get(songId));

            // 4、前端设置
            textBox_song.Text = result.songVO.Name;
            textBox_singer.Text = result.songVO.Singer;
            textBox_album.Text = result.songVO.Album;
            UpdateLrcTextBox("");
        }

        // 批量歌曲搜索
        private void BatchSearch(string[] ids)
        {
            Dictionary<string, string> resultMaps = new Dictionary<string, string>();
            foreach (string songIdStr in ids)
            {
                // 1、参数校验
                long songId = NeteaseMusicUtils.CheckInputId(songIdStr, SEARCH_TYPE_ENUM.SONG_ID, out string inputErrorMsg);
                if (inputErrorMsg != ErrorMsg.SUCCESS)
                {
                    resultMaps.Add(songIdStr, inputErrorMsg);
                    continue;
                }

                // 2、获取歌曲内容
                SaveVO result;
                if (NeteaseMusicResultCache.Contains(songId))
                {
                    result = NeteaseMusicResultCache.Get(songId);
                }
                else
                {
                    SongVO songVO = RequestSongVO(songId, out string songErrorMsg);
                    if (songErrorMsg != ErrorMsg.SUCCESS)
                    {
                        resultMaps.Add(songIdStr, songErrorMsg);
                        continue;
                    }

                    LyricVO lyricVO = RequestLyricVO(songId, globalSearchInfo, out string lyricErrorMsg);
                    if (lyricErrorMsg != ErrorMsg.SUCCESS)
                    {
                        resultMaps.Add(songIdStr, lyricErrorMsg);
                        continue;
                    }

                    result = new SaveVO(songIdStr, songVO, lyricVO);
                    NeteaseMusicResultCache.Put(songId, result);
                }

                // 3、加入结果集
                globalSaveVOMap.Add(songIdStr, NeteaseMusicResultCache.Get(songId));
                resultMaps.Add(songIdStr, ErrorMsg.SEARCH_RESULT_STAGE);
            }

            // 输出日志
            StringBuilder log = new StringBuilder();
            log.Append("---Total：" + resultMaps.Count + ", Success：" + globalSaveVOMap.Count + ", Failure：" + (resultMaps.Count - globalSaveVOMap.Count) + "---\r\n");
            foreach (KeyValuePair<string, string> kvp in resultMaps)
            {
                log.Append("ID: " + kvp.Key + ", Result: " + kvp.Value + "\r\n");
            }
            UpdateLrcTextBox(log.ToString());
        }

        // 搜索按钮点击事件
        private void searchBtn_Click(object sender, EventArgs e)
        {
            ReloadConfig();
            UpdateLrcTextBox("  ");

            SEARCH_TYPE_ENUM type = globalSearchInfo.SerchType;
            if(type == SEARCH_TYPE_ENUM.SONG_ID)
            {
                string[] ids = globalSearchInfo.SearchIds;

                if(ids.Length < 1)
                {
                    MessageBox.Show(ErrorMsg.INPUT_ID_ILLEGAG, "提示");
                    return;
                }

                // 初始化结果 map
                globalSaveVOMap = new Dictionary<string, SaveVO>();

                if (ids.Length > 1)
                {
                    BatchSearch(ids);
                }
                else
                {
                    SingleSearchBySongId(ids[0]);
                }
            }
            else
            {
                MessageBox.Show(ErrorMsg.FUNCTION_NOT_SUPPORT, "提示");
                return;
            }
        }

        // 获取直链点击事件
        private void songUrlBtn_Click(object sender, EventArgs e)
        {
            if(globalSaveVOMap == null || globalSaveVOMap.Count == 0)
            {
                MessageBox.Show(ErrorMsg.MUST_SEARCH_BEFORE_COPY_SONG_URL, "提示");
                return;
            }

            if(globalSaveVOMap.Count > 1)
            {

                // 输出日志
                StringBuilder log = new StringBuilder();
                foreach (var songIdStr in globalSearchInfo.SearchIds)
                {
                    if (globalSaveVOMap.ContainsKey(songIdStr))
                    {
                        SaveVO saveVO = new SaveVO();
                        globalSaveVOMap.TryGetValue(songIdStr, out saveVO);

                        log.Append("ID: " + songIdStr + ", Links: " + saveVO.songVO.Links + "\r\n");
                    }
                    else
                    {
                        log.Append("ID: " + songIdStr + ", Links: failure\r\n");
                    }
                }
                UpdateLrcTextBox(log.ToString());
            } 
            else
            {
                // only loop one times
                foreach (var item in globalSaveVOMap)
                {
                    Clipboard.SetDataObject(item.Value.songVO.Links);
                    MessageBox.Show(ErrorMsg.SONG_URL_COPY_SUCESS, "提示");
                }
            }
        }

        // 单个保存
        private void SingleSave(string songIdStr)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            try
            {
                SaveVO saveVO = new SaveVO();
                if(!globalSaveVOMap.TryGetValue(songIdStr, out saveVO))
                {
                    MessageBox.Show(ErrorMsg.MUST_SEARCH_BEFORE_SAVE, "提示");
                    return;
                }

                string outputFileName = NeteaseMusicUtils.GetOutputName(saveVO.songVO, globalSearchInfo);
                if (outputFileName == null)
                {
                    MessageBox.Show(ErrorMsg.FILE_NAME_IS_EMPTY, "提示");
                    return;
                }
                else
                {
                    outputFileName = NeteaseMusicUtils.GetSafeFilename(outputFileName);
                }

                saveDialog.FileName = outputFileName;
                saveDialog.Filter = "lrc文件(*.lrc)|*.lrc|txt文件(*.txt)|*.txt";
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    StreamWriter sw = new StreamWriter(saveDialog.FileName, false, NeteaseMusicUtils.GetEncoding(globalSearchInfo.Encoding));
                    sw.Write(textBox_lrc.Text);
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

        // 批量保存
        private void BatchSave()
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            try
            {
                saveDialog.FileName = "直接选择保存路径即可，无需修改此处内容";
                saveDialog.Filter = "lrc文件(*.lrc)|*.lrc|txt文件(*.txt)|*.txt";
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {

                    string localFilePath = saveDialog.FileName.ToString();
                    // 获取文件后缀
                    string fileSuffix = localFilePath.Substring(localFilePath.LastIndexOf("."));
                    //获取文件路径，不带文件名 
                    string filePath = localFilePath.Substring(0, localFilePath.LastIndexOf("\\"));

                    foreach (var item in globalSaveVOMap)
                    {
                        string outputFileName = NeteaseMusicUtils.GetOutputName(item.Value.songVO, globalSearchInfo);
                        string path = filePath + "/" + NeteaseMusicUtils.GetSafeFilename(outputFileName) + fileSuffix;
                        StreamWriter sw = new StreamWriter(path, false, NeteaseMusicUtils.GetEncoding(globalSearchInfo.Encoding));
                        sw.Write(NeteaseMusicUtils.GetOutputLyric(item.Value.lyricVO.Lyric, item.Value.lyricVO.TLyric, globalSearchInfo));
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
            StringBuilder log = new StringBuilder();
            foreach (var songIdStr in globalSearchInfo.SearchIds)
            {
                if (globalSaveVOMap.ContainsKey(songIdStr))
                {
                    log.Append("ID: " + songIdStr + ", Result: success\r\n");
                } else
                {
                    log.Append("ID: " + songIdStr + ", Result: failure\r\n");
                }
            }
            UpdateLrcTextBox(log.ToString());
        }

        // 保存按钮点击事件
        private void saveBtn_Click(object sender, EventArgs e)
        {
            if (globalSaveVOMap == null || globalSaveVOMap.Count == 0)
            {
                MessageBox.Show(ErrorMsg.MUST_SEARCH_BEFORE_SAVE, "提示");
                return;
            }

            if (globalSaveVOMap.Count > 1)
            {
                BatchSave();
            }
            else
            {
                // only loop one times
                foreach (var item in globalSaveVOMap)
                {
                    SingleSave(item.Value.songId);
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
            
            if (show_lrc_type_enum == SHOW_LRC_TYPE_ENUM.MERGE_ORIGIN || show_lrc_type_enum == SHOW_LRC_TYPE_ENUM.MERGE_TRANSLATE)
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
            UpdateLrcTextBox("");
        }
        
        private void comboBox_search_type_SelectedIndexChanged(object sender, EventArgs e)
        {
            search_type_enum = (SEARCH_TYPE_ENUM)comboBox_search_type.SelectedIndex;
            
            ReloadConfig();
            UpdateLrcTextBox("");
        }

        private void comboBox_dot_SelectedIndexChanged(object sender, EventArgs e)
        {
            dot_type_enum = (DOT_TYPE_ENUM)comboBox_dot.SelectedIndex;
            ReloadConfig();
            UpdateLrcTextBox("");
        }

        // 项目主页item
        private void homeMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/jitwxs/163MusicLyrics");
        }

        // 最新版本item
        private void latestVersionMenuItem_Click(object sender, EventArgs e)
        {
            // support https
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Accept", "application/vnd.github.v3+json");
            headers.Add("User-Agent", NeteaseMusicAPI._USERAGENT);

            string jsonStr = HttpUtils.HttpGet("http://api.github.com/repos/jitwxs/163MusicLyrics/releases/latest", "application/json", headers);
            JObject obj = (JObject)JsonConvert.DeserializeObject(jsonStr);
            OutputLatestTag(obj["tag_name"]);
        }

        private void OutputLatestTag(JToken latestTag)
        {
            if (latestTag == null)
            {
                MessageBox.Show(ErrorMsg.GET_LATEST_VERSION_FAILED, "提示");
            } 
            else
            {
                string bigV = latestTag.ToString().Substring(1, 2), smallV = latestTag.ToString().Substring(3);
                string curBigV = VERSION.Substring(1, 2), curSmallV = VERSION.Substring(3);

                if(bigV.CompareTo(curBigV) == 1 || (bigV.CompareTo(curBigV) == 0 && smallV.CompareTo(curSmallV) == 1))
                {
                    Clipboard.SetDataObject("https://github.com/jitwxs/163MusicLyrics/releases");
                    MessageBox.Show(ErrorMsg.EXIST_LATEST_VERSION, "提示");
                }
                else
                {
                    MessageBox.Show(ErrorMsg.THIS_IS_LATEST_VERSION, "提示");
                }

            }
        }

        // 问题反馈item
        private void issueMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/jitwxs/163MusicLyrics/issues");
        }

        // 使用手册
        private void wikiItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/jitwxs/163MusicLyrics/wiki");
        }

        private void splitTextBox_TextChanged(object sender, EventArgs e)
        {
            ReloadConfig();
            UpdateLrcTextBox("");
        }

        // 更新前端歌词
        private void UpdateLrcTextBox(string replace)
        {
            if (replace != "")
            {
                textBox_lrc.Text = replace;
            }
            else
            {
                // 根据最新配置，更新输出歌词
                if (globalSaveVOMap != null && globalSaveVOMap.Count == 1)
                {
                    // only loop one times
                    foreach (var item in globalSaveVOMap)
                    {
                        string outputLyric = NeteaseMusicUtils.GetOutputLyric(item.Value.lyricVO.Lyric, item.Value.lyricVO.TLyric, globalSearchInfo);
                        textBox_lrc.Text = outputLyric == "" ? ErrorMsg.LRC_NOT_EXIST : outputLyric;
                    }
                }
            }
        }

        // 清空前端内容
        private void CleanTextBox()
        {
            textBox_lrc.Text = "";
            textBox_song.Text = "";
            textBox_singer.Text = "";
            textBox_album.Text = "";
        }
    }
}
