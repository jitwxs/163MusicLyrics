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
        SaveVO globalSaveVO = null;
        SearchInfo globalSearchInfo = new SearchInfo();

        // 输出文件编码
        string output_file_encoding = "UTF-8";
        // 搜索类型
        SEARCH_TYPE_ENUM search_type_enum;
        // 展示歌词类型
        SHOW_LRC_TYPE_ENUM show_lrc_type_enum;
        // 输出文件名类型
        OUTPUT_FILENAME_TYPE_ENUM output_filename_type_enum;

        const string VERSION = "v3.0";

        public MainForm()
        {
            InitializeComponent();
            
            comboBox_output_name.SelectedIndex = 0;
            comboBox_output_encode.SelectedIndex = 0;
            comboBox_diglossia_lrc.SelectedIndex = 0;
            comboBox_search_type.SelectedIndex = 0;

            api = new NeteaseMusicAPI();
        }

        private void ReloadConfig()
        {
            globalSearchInfo.SearchId = search_id_text.Text.Trim();
            globalSearchInfo.SerchType = search_type_enum;
            globalSearchInfo.OutputFileNameType = output_filename_type_enum;
            globalSearchInfo.ShowLrcType = show_lrc_type_enum;
            globalSearchInfo.Encoding = output_file_encoding;
            globalSearchInfo.Constraint2Dot = dotCheckBox.CheckState == CheckState.Checked;
            globalSearchInfo.BatchSearch = batchSearchCheckBox.CheckState == CheckState.Checked;
            globalSearchInfo.LrcMergeSeparator = splitTextBox.Text;
        }

        private SongVO RequestSongVO(long songId)
        {
            SongUrls songUrls = api.GetSongsUrl(new long[] { songId });
            DetailResult detailResult = api.GetDetail(songId);

            return NeteaseMusicUtils.GetSongVO(songUrls, detailResult);
        }

        private LyricVO RequestLyricVO(long songId, SearchInfo searchInfo)
        {
            LyricResult lyricResult = api.GetLyric(songId);
            return NeteaseMusicUtils.GetLyricVO(lyricResult, searchInfo);
        }

        // 单个歌曲搜索
        private void SingleSearch()
        {
            string songIdStr = globalSearchInfo.SearchId;
            if (songIdStr == "" || songIdStr == null || !NeteaseMusicUtils.CheckNum(songIdStr))
            {
                MessageBox.Show(ErrorMsg.INPUT_ID_ILLEGAG, "提示");
                return;
            }

            long songId = long.Parse(songIdStr);
            if(NeteaseMusicResultCache.Contains(songId))
            {
                globalSaveVO = NeteaseMusicResultCache.Get(songId);
            } 
            else
            {
                SongVO songVO = RequestSongVO(songId);
                if (!songVO.Success)
                {
                    MessageBox.Show(songVO.Message, "提示");
                    return;
                }
                
                LyricVO lyricVO = RequestLyricVO(songId, globalSearchInfo);
                if (!lyricVO.Success)
                {
                    MessageBox.Show(songVO.Message, "提示");
                    return;
                }

                globalSaveVO = new SaveVO(songVO, lyricVO);
                NeteaseMusicResultCache.Put(songId, globalSaveVO);
            }

            // 前端设置
            textBox_song.Text = globalSaveVO.songVO.Name;
            textBox_singer.Text = globalSaveVO.songVO.Singer;
            textBox_album.Text = globalSaveVO.songVO.Album;
            UpdateLrcTextBox("");
        }

        // 批量歌曲搜索
        private void BatchSearch()
        {
            string[] ids = globalSearchInfo.SearchId.Split(',');

            int successCount = 0;
            Dictionary<string, string> resultMaps = new Dictionary<string, string>();
            foreach (string songIdStr in ids)
            {
                if (songIdStr == "" || songIdStr == null || !NeteaseMusicUtils.CheckNum(songIdStr))
                {
                    resultMaps.Add(songIdStr, ErrorMsg.INPUT_ID_ILLEGAG);
                    continue;
                }

                long songId = long.Parse(songIdStr);
                if (!NeteaseMusicResultCache.Contains(songId))
                {
                    SongVO songVO = RequestSongVO(songId);
                    if (!songVO.Success)
                    {
                        resultMaps.Add(songIdStr, songVO.Message);
                        continue;
                    }

                    LyricVO lyricVO = RequestLyricVO(songId, globalSearchInfo);
                    if (!lyricVO.Success)
                    {
                        resultMaps.Add(songIdStr, lyricVO.Message);
                        continue;
                    }

                    NeteaseMusicResultCache.Put(songId, new SaveVO(songVO, lyricVO));
                }

                successCount++;
                resultMaps.Add(songIdStr, ErrorMsg.SEARCH_RESULT_STAGE);
            }

            // 输出日志
            StringBuilder log = new StringBuilder();
            log.Append("---Total：" + resultMaps.Count + ", Success：" + successCount + ", Failure：" + (resultMaps.Count - successCount) + "---\r\n");
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
                if(globalSearchInfo.BatchSearch)
                {
                    BatchSearch();
                }
                else
                {
                    SingleSearch();
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
            if (globalSearchInfo.BatchSearch) 
            {
                // 数据准备
                string[] ids = globalSearchInfo.SearchId.Split(',');
                Dictionary<string, string> resultMaps = new Dictionary<string, string>();
                int successCount = 0;
                foreach (string songIdStr in ids)
                {
                    if (songIdStr == "" || songIdStr == null || !NeteaseMusicUtils.CheckNum(songIdStr))
                    {
                        resultMaps.Add(songIdStr, ErrorMsg.INPUT_ID_ILLEGAG);
                        continue;
                    }

                    long songId = long.Parse(songIdStr);
                    if (!NeteaseMusicResultCache.Contains(songId))
                    {
                        resultMaps.Add(songIdStr, ErrorMsg.MUST_SEARCH_BEFORE_SAVE);
                        continue;
                    }

                    successCount++;
                    resultMaps.Add(songIdStr, NeteaseMusicResultCache.Get(songId).songVO.Links);
                }

                // 输出日志
                StringBuilder log = new StringBuilder();
                log.Append("---Total：" + resultMaps.Count + ", Success：" + successCount + ", Failure：" + (resultMaps.Count - successCount) + "---\r\n");
                foreach (KeyValuePair<string, string> kvp in resultMaps)
                {
                    log.Append("ID: " + kvp.Key + ", Result: " + kvp.Value + "\r\n");
                }
                UpdateLrcTextBox(log.ToString());
            }
            else
            {
                if (globalSaveVO == null)
                {
                    MessageBox.Show(ErrorMsg.MUST_SEARCH_BEFORE_COPY_SONG_URL, "提示");
                    return;
                }

                Clipboard.SetDataObject(globalSaveVO.songVO.Links);
                MessageBox.Show(ErrorMsg.SONG_URL_COPY_SUCESS, "提示");
            }
        }

        private void SingleSave()
        {
            if (globalSaveVO == null)
            {
                MessageBox.Show(ErrorMsg.MUST_SEARCH_BEFORE_SAVE, "提示");
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog();
            try
            {
                string outputFileName = NeteaseMusicUtils.GetOutputName(globalSaveVO.songVO, globalSearchInfo);
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
                    string fileName = saveDialog.FileName;
                    StreamWriter sw = new StreamWriter(fileName, false, Encoding.GetEncoding(globalSearchInfo.Encoding));
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

        private void BatchSave()
        {
            // 数据准备
            string[] ids = globalSearchInfo.SearchId.Split(',');
            Dictionary<string, string> resultMaps = new Dictionary<string, string>();
            Dictionary<long, SaveVO> successMaps = new Dictionary<long, SaveVO>();
            foreach (string songIdStr in ids)
            {
                if (songIdStr == "" || songIdStr == null || !NeteaseMusicUtils.CheckNum(songIdStr))
                {
                    resultMaps.Add(songIdStr, ErrorMsg.INPUT_ID_ILLEGAG);
                    continue;
                }

                long songId = long.Parse(songIdStr);
                if (!NeteaseMusicResultCache.Contains(songId))
                {
                    resultMaps.Add(songIdStr, ErrorMsg.MUST_SEARCH_BEFORE_SAVE);
                    continue;
                }

                resultMaps.Add(songIdStr, ErrorMsg.SAVE_SUCCESS);
                successMaps.Add(songId, NeteaseMusicResultCache.Get(songId));
            }

            // 弹窗保存
            if (successMaps.Count > 0)
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

                        foreach (KeyValuePair<long, SaveVO> kvp in successMaps)
                        {
                            string outputFileName = NeteaseMusicUtils.GetOutputName(kvp.Value.songVO, globalSearchInfo);
                            string path = filePath + "/" + NeteaseMusicUtils.GetSafeFilename(outputFileName) + fileSuffix;
                            StreamWriter sw = new StreamWriter(path, false, Encoding.GetEncoding(globalSearchInfo.Encoding));
                            sw.Write(NeteaseMusicUtils.GetOutputLyric(kvp.Value.lyricVO.Lyric, kvp.Value.lyricVO.TLyric, globalSearchInfo));
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
            }

            // 输出日志
            StringBuilder log = new StringBuilder();
            log.Append("---Total：" + resultMaps.Count + ", Success：" + successMaps.Count + ", Failure：" + (resultMaps.Count - successMaps.Count) + "---\r\n");
            foreach (KeyValuePair<string, string> kvp in resultMaps)
            {
                log.Append("ID: " + kvp.Key + ", Result: " + kvp.Value + "\r\n");
            }
            UpdateLrcTextBox(log.ToString());
        }

        // 保存按钮点击事件
        private void saveBtn_Click(object sender, EventArgs e)
        {
            if (globalSearchInfo.BatchSearch)
            {
                BatchSave();
            }
            else
            {
                SingleSave();
            }
        }

        private void comboBox_output_name_SelectedIndexChanged(object sender, EventArgs e)
        {
            output_filename_type_enum = (OUTPUT_FILENAME_TYPE_ENUM)comboBox_output_name.SelectedIndex;
            ReloadConfig();
        }

        private void comboBox_output_encode_SelectedIndexChanged(object sender, EventArgs e)
        {
            output_file_encoding = comboBox_output_encode.SelectedItem.ToString();
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
            System.Diagnostics.Process.Start("https://www.jitwxs.cn/wiki/163-music-lyrics");
        }

        private void batchSearchCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ReloadConfig();
            CleanTextBox();
        }

        private void dotCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ReloadConfig();
            UpdateLrcTextBox("");
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
                if (!globalSearchInfo.BatchSearch && globalSaveVO != null)
                {
                    // 根据最新配置，更新输出歌词
                    string outputLyric = NeteaseMusicUtils.GetOutputLyric(globalSaveVO.lyricVO.Lyric, globalSaveVO.lyricVO.TLyric, globalSearchInfo);
                    textBox_lrc.Text = outputLyric == "" ? ErrorMsg.LRC_NOT_EXIST : outputLyric;
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
