using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using 网易云歌词提取;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        NeteaseMusicAPI api = null;
        SaveVO saveVO = null;

        // 输出文件编码
        string output_file_encoding = "UTF-8";
        // 搜索类型
        SEARCH_TYPE_ENUM search_type_enum;
        // 展示歌词类型
        SHOW_LRC_TYPE_ENUM show_lrc_type_enum;
        // 输出文件名类型
        OUTPUT_FILENAME_TYPE_ENUM output_filename_type_enum;

        public Form1()
        {
            InitializeComponent();
            
            comboBox_output_name.SelectedIndex = 0;
            comboBox_output_encode.SelectedIndex = 0;
            comboBox_diglossia_lrc.SelectedIndex = 0;
            comboBox_search_type.SelectedIndex = 0;

            api = new NeteaseMusicAPI();
        }

        private SearchInfo reloadConfig()
        {
            SearchInfo searchInfo = new SearchInfo();

            searchInfo.SearchId = search_id_text.Text.Trim();
            searchInfo.SerchType = search_type_enum;
            searchInfo.OutputFileNameType = output_filename_type_enum;
            searchInfo.ShowLrcType = show_lrc_type_enum;
            searchInfo.Encoding = output_file_encoding;
            searchInfo.Constraint2Dot = dotCheckBox.CheckState == CheckState.Checked;
            searchInfo.BatchSearch = batchSearchCheckBox.CheckState == CheckState.Checked;
            searchInfo.LrcMergeSeparator = splitTextBox.Text;

            return searchInfo;
        }

        private SongVO RequestSongVO(long songId)
        {
            SongUrls songUrls = api.GetSongsUrl(new long[] { songId });
            DetailResult detailResult = api.GetDetail(songId);

            return NeteaseMusicUtils.GetSongVO(songUrls, detailResult);
        }

        public LyricVO RequestLyricVO(long songId, SearchInfo searchInfo)
        {
            LyricResult lyricResult = api.GetLyric(songId);
            return NeteaseMusicUtils.GetLyricVO(lyricResult, searchInfo);
        }

        public static string GetSafeFilename(string arbitraryString)
        {
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            var replaceIndex = arbitraryString.IndexOfAny(invalidChars, 0);
            if (replaceIndex == -1) return arbitraryString;

            var r = new StringBuilder();
            var i = 0;

            do
            {
                r.Append(arbitraryString, i, replaceIndex - i);

                switch (arbitraryString[replaceIndex])
                {
                    case '"':
                        r.Append("''");
                        break;
                    case '<':
                        r.Append('\u02c2'); // '˂' (modifier letter left arrowhead)
                        break;
                    case '>':
                        r.Append('\u02c3'); // '˃' (modifier letter right arrowhead)
                        break;
                    case '|':
                        r.Append('\u2223'); // '∣' (divides)
                        break;
                    case ':':
                        r.Append('-');
                        break;
                    case '*':
                        r.Append('\u2217'); // '∗' (asterisk operator)
                        break;
                    case '\\':
                    case '/':
                        r.Append('\u2044'); // '⁄' (fraction slash)
                        break;
                    case '\0':
                    case '\f':
                    case '?':
                        break;
                    case '\t':
                    case '\n':
                    case '\r':
                    case '\v':
                        r.Append(' ');
                        break;
                    default:
                        r.Append('_');
                        break;
                }

                i = replaceIndex + 1;
                replaceIndex = arbitraryString.IndexOfAny(invalidChars, i);
            } while (replaceIndex != -1);

            r.Append(arbitraryString, i, arbitraryString.Length - i);

            return r.ToString();
        }

        // 搜索按钮点击事件
        private void searchBtn_Click(object sender, EventArgs e)
        {
            SearchInfo searchInfo = reloadConfig();

            SEARCH_TYPE_ENUM type = searchInfo.SerchType;
            if(type == SEARCH_TYPE_ENUM.SONG_ID)
            {
                string songIdStr = searchInfo.SearchId;
                if (songIdStr == "" || songIdStr == null || !NeteaseMusicUtils.CheckNum(songIdStr))
                {
                    MessageBox.Show("【警告】ID 为空或格式非法！", "提示");
                    return;
                }

                long songId = long.Parse(songIdStr);

                SongVO songVO = RequestSongVO(songId);
                if(songVO.Success)
                {
                    textBox_song.Text = songVO.Name;
                    textBox_singer.Text = songVO.Singer;
                    textBox_album.Text = songVO.Album;
                } 
                else
                {
                    MessageBox.Show("【错误】" + songVO.Message, "提示");
                    return;
                }
                
                LyricVO lyricVO = RequestLyricVO(songId, searchInfo);
                if(lyricVO.Success)
                {
                    textBox_lrc.Text = lyricVO.Output;
                } 
                else
                {
                    MessageBox.Show("【错误】" + songVO.Message, "提示");
                    return;
                }

                saveVO = new SaveVO(songVO, lyricVO, searchInfo);
            }
            else
            {
                MessageBox.Show("暂不支持", "提示");
                return;
            }
        }

        // 保存按钮点击事件
        private void saveBtn_Click(object sender, EventArgs e)
        {
            if (saveVO == null)
            {
                MessageBox.Show("请先点击搜索按钮", "提示");
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog();
            try
            {
                string outputFileName = NeteaseMusicUtils.GetOutputName(saveVO.songVO, saveVO.searchInfo);
                if (outputFileName == null)
                {
                    MessageBox.Show("命名格式发生错误！", "错误");
                    return;
                }
                else
                {
                    outputFileName = GetSafeFilename(outputFileName);
                }

                saveDialog.FileName = outputFileName;
                saveDialog.Filter = "lrc文件(*.lrc)|*.lrc|txt文件(*.txt)|*.txt";
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = saveDialog.FileName;
                    StreamWriter sw = new StreamWriter(fileName, false, Encoding.GetEncoding(saveVO.searchInfo.Encoding));
                    sw.Write(textBox_lrc.Text);
                    sw.Flush();
                    sw.Close();
                    MessageBox.Show("保存成功！", "提示");
                }
            }
            catch (Exception ew)
            {
                MessageBox.Show("保存失败！错误信息：\n" + ew.Message);
            }
        }

        /* 批量保存按钮点击事件
        private void batchBtn_Click(object sender, EventArgs e)
        {
            string[] ids = idsTextbox.Text.Split(',');
            Dictionary<string, string> resultMaps = new Dictionary<string, string>();
            Dictionary<string, string> failureMaps = new Dictionary<string, string>();

            // 准备数据
            foreach (string id in ids)
            {
                Song song = new Song();

                try
                {
                    if (id == "" || id == null || !CheckNum(id))
                    {
                        throw new Exception("ID不合法");
                    }

                    HttpStatus status = GetSongLrc(HttpHelper(string.Format(LRC_URL, id)), ref song);

                    if (status.GetCode() == HTTP_STATUS_SUCCESS)
                    {
                        // 双语歌词处理
                        string[] lrcResult = ParserDiglossiaLrc(ref song);
                        // 强制两位小数
                        if (dotCheckBox.CheckState == CheckState.Checked)
                        {
                            SetTimeStamp2Dot(ref lrcResult);
                        }

                        status = GetSongBasicInfo(HttpHelper(string.Format(SONG_INFO_URL, id, id)), ref song);
                        if (status.GetCode() == HTTP_STATUS_SUCCESS)
                        {
                            string outputFileName = GetOutputFileName(ref song);
                            resultMaps.Add(outputFileName, TrimLrc(lrcResult));
                        }
                        else
                        {
                            throw new Exception(status.GetMsg());
                        }
                    }
                    else
                    {
                        throw new Exception(status.GetMsg());
                    }
                }
                catch (Exception ew)
                {
                    failureMaps.Add(id, ew.Message);
                    Console.WriteLine(ew);
                }
            }

            // 输出歌词缓存日志
            StringBuilder log = new StringBuilder();
            log.Append("---Total：" + ids.Length + ", Success：" + resultMaps.Count + ", Failure：" + failureMaps.Count + "---\r\n");
            if (failureMaps.Count > 0)
            {
                foreach (KeyValuePair<string, string> kvp in failureMaps)
                {
                    log.Append("ID: " + kvp.Key + ", Reason: " + kvp.Value + "\r\n");
                }
            }
            textBox_lrc.Text = log.ToString();

            // 保存
            if (resultMaps.Count > 0)
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

                        foreach (KeyValuePair<string, string> kvp in resultMaps)
                        {
                            string path = filePath + "/" + GetSafeFilename(kvp.Key) + fileSuffix;
                            StreamWriter sw = new StreamWriter(path, false, Encoding.GetEncoding(outputFileEncode));
                            sw.Write(kvp.Value);
                            sw.Flush();
                            sw.Close();
                        }
                        MessageBox.Show("保存成功！", "提示");
                    }
                }
                catch (Exception ew)
                {
                    MessageBox.Show("批量保存失败，错误信息：\n" + ew.Message);
                }
            }
        } */

        private void comboBox_output_name_SelectedIndexChanged(object sender, EventArgs e)
        {
            output_filename_type_enum = (OUTPUT_FILENAME_TYPE_ENUM)(comboBox_output_name.SelectedIndex + 1);
        }

        private void comboBox_output_encode_SelectedIndexChanged(object sender, EventArgs e)
        {
            output_file_encoding = comboBox_output_encode.SelectedItem.ToString();
        }

        private void comboBox_diglossia_lrc_SelectedIndexChanged(object sender, EventArgs e)
        {
            show_lrc_type_enum = (SHOW_LRC_TYPE_ENUM)(comboBox_diglossia_lrc.SelectedIndex + 1);
            
            if (show_lrc_type_enum == SHOW_LRC_TYPE_ENUM.MERGE_ORIGIN || show_lrc_type_enum == SHOW_LRC_TYPE_ENUM.MERGE_TRANSLATE)
            {
                splitTextBox.ReadOnly = false;
            }
            else
            {
                splitTextBox.Text = null;
                splitTextBox.ReadOnly = true;
            }
        }
        
        private void comboBox_search_type_SelectedIndexChanged(object sender, EventArgs e)
        {
            search_type_enum = (SEARCH_TYPE_ENUM)(comboBox_search_type.SelectedIndex + 1);
        }
    }

}
