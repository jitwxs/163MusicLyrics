using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private static string HTTP_STATUS_SUCCESS = "200";
        private static string HTTP_STATUS_NOT_FOUND = "404";
        private static string HTTP_STATUS_INTERNAL_ERROR = "500";

        // 获取歌词 URL
        public static string LRC_URL = "http://music.163.com/api/song/lyric?os=pc&id={0}&lv=-1&kv=-1&tv=-1";
        // 获取歌曲信息 URL
        public static string SONG_INFO_URL = "http://music.163.com/api/song/detail/?id={0}&ids=[{1}]";

        // 输出文件名类型
        int outputFileNameType = 0;
        // 双语歌词类型
        int diglossiaLrcType = 0;
        // 每次点击搜索后，当前的 Song 对象
        private Song currentSong = null;

        public Form1()
        {
            InitializeComponent();
            comboBox_output_name.SelectedIndex = 0;
            comboBox_diglossia_lrc.SelectedIndex = 0;
        }

        // GET请求服务器
        public string HttpHelper(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 2000;
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }

        // 获取歌曲名、歌手名
        public HttpStatus GetSongBasicInfo(string jsonStr, ref Song song)
        {
            JObject obj = JObject.Parse(jsonStr);
            HttpStatus httpStatus = GetHttpStatus(obj);

            if(httpStatus.GetCode() == HTTP_STATUS_SUCCESS)
            {
                JToken songName = obj["songs"][0]["name"];
                if (songName != null)
                {
                    song.SetName(songName.ToString());
                } else
                {
                    Console.WriteLine("歌曲名为空");
                }

                try
                {
                    List<string> singerList = new List<string>();

                    var names = from staff in obj["songs"][0]["artists"].Children() select (string)staff["name"];
                    foreach (var name in names)
                    {
                        singerList.Add(name);
                    }

                    song.SetSinger(string.Join(",", singerList.ToArray()));
                }
                catch (Exception ew)
                {
                    Console.WriteLine("获取歌曲信息错误：" + ew);
                }
            }

            return httpStatus;
        }

        // 获取歌曲原文歌词、译文歌词
        public HttpStatus GetSongLrc(string jsonStr, ref Song song)
        {
            JObject obj = JObject.Parse(jsonStr);
            HttpStatus httpStatus = GetHttpStatus(obj);

            if (httpStatus.GetCode() == HTTP_STATUS_SUCCESS)
            {
                JToken lyric = obj["lrc"]["lyric"];
                if (lyric != null)
                {
                    song.SetLyric(lyric.ToString());
                }
                else
                {
                    Console.WriteLine("歌词为空");
                }

                JToken tlyric = obj["tlyric"]["lyric"];
                if (tlyric != null)
                {
                    song.SetTlyric(tlyric.ToString());
                }
                else
                {
                    Console.WriteLine("译文歌词为空");
                }
            }
            
            return httpStatus;
        }
        
        // 将歌词切割为数组
        private string[] SplitLrc(string lrc)
        {
            string[] tmp = lrc.Split('[');
            string[] lyric = new string[tmp.Length - 1];

            for (int i = 1; i < tmp.Length; i++)
            {
                lyric[i - 1] = "[" + tmp[i];
            }

            return lyric;
        }

        // 将歌词数组转为字符串
        private string TrimLrc(string[] lrcs)
        {
            string result = "";

            foreach (string i in lrcs)
                result += i + "\r\n";
            
            return result;
        }

        private string GetOutputFileName(ref Song song)
        {
            string outputFileName = null;
            switch (outputFileNameType)
            {
                case (int)OUTPUT_FILENAME_TYPE.NAME_SINGER:
                    // 歌曲名 - 歌手
                    outputFileName = song.GetName() + " - " + song.GetSinger();
                    break;
                case (int)OUTPUT_FILENAME_TYPE.SINGER_NAME:
                    // 歌手 - 歌曲名
                    outputFileName = song.GetSinger() + " - " + song.GetName();
                    break;
                case (int)OUTPUT_FILENAME_TYPE.NAME:
                    // 歌曲名
                    outputFileName = song.GetName();
                    break;
            }
            return outputFileName;
        }
 
        // 歌词排序函数
        private static int Compare(string originLrc, string translateLrc, bool hasOriginLrcPrior)
        {
            int str1Index = originLrc.IndexOf("]");
            string str1Timestamp = originLrc.Substring(0, str1Index + 1);
            int str2Index = translateLrc.IndexOf("]");
            string str2Timestamp = translateLrc.Substring(0, str2Index + 1);

            if (str1Timestamp != str2Timestamp)
            {
                str1Timestamp = str1Timestamp.Substring(1, str1Timestamp.Length - 2);
                str2Timestamp = str2Timestamp.Substring(1, str2Timestamp.Length - 2);
                string[] t1s = str1Timestamp.Split(':');
                string[] t2s = str2Timestamp.Split(':');
                for (int i = 0; i < t1s.Length; i++)
                {
                    if (double.TryParse(t1s[i], out double t1))
                    {
                        if (double.TryParse(t2s[i], out double t2))
                        {
                            if (t1 > t2)
                                return 1;
                            else if (t1 < t2)
                                return -1;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                    else
                    {
                        return -1;
                    }
                }
                return 0;
            }

            // 是否原文歌词优先
            if(hasOriginLrcPrior)
            {
                return string.Compare(originLrc.Substring(str1Index), translateLrc.Substring(str2Index));
            } else
            {
                return string.Compare(translateLrc.Substring(str2Index), originLrc.Substring(str1Index));
            }
        }

        // 双语歌词排序
        private static string[] SortLrc(string[] originLrcs, string[] translateLrcs, bool hasOriginLrcPrior)
        {
            int lena = originLrcs.Length;
            int lenb = translateLrcs.Length;
            string[] c = new string[lena + lenb];
            //分别代表数组a ,b , c 的索引
            int i = 0, j = 0, k = 0;

            while (i < lena && j < lenb)
            {
                if (Compare(originLrcs[i], translateLrcs[j], hasOriginLrcPrior) == 1)
                    c[k++] = translateLrcs[j++];
                else
                    c[k++] = originLrcs[i++];
            }
            
            while (i < lena)
                c[k++] = originLrcs[i++];
            while (j < lenb)
                c[k++] = translateLrcs[j++];
            return c;
        }

        // 双语歌词合并
        private static string[] MergeLrc(string[] originLrcs, string[] translateLrcs, string splitStr)
        {
            string[] c = SortLrc(originLrcs, translateLrcs, true);
            List<string> list = new List<string>
            {
                c[0]
            };

            for (int i=1;i<c.Length;i++)
            {
                int str1Index = c[i-1].IndexOf("]") + 1;
                int str2Index = c[i].IndexOf("]") + 1;
                string str1Timestamp = c[i-1].Substring(0, str1Index);
                string str2Timestamp = c[i].Substring(0, str2Index);
                if(str1Timestamp != str2Timestamp)
                {
                    list.Add(c[i]);
                }
                else
                {
                    int index = list.Count - 1;
                    list[index] = list[index] + splitStr + c[i].Substring(str2Index);
                }
            }
            return list.ToArray();
        }

        // 处理双语歌词
        public string[] ParserDiglossiaLrc(ref Song song)
        {
            string[] res = null;
            string originLrc = song.GetLyric();
            string translateLrc = song.GetTlyric();

            // 如果不存在翻译歌词，或者选择返回原歌词，直接返回原歌词
            string[] originLrcs = SplitLrc(originLrc);
            if (translateLrc  == null || translateLrc == "" || diglossiaLrcType == (int)DIGLOSSIA_LRC_TYPE.ONLY_ORIGIN)
            {
                return originLrcs;
            }
            
            // 如果选择仅译文
            string[] translateLrcs = SplitLrc(translateLrc);
            if (diglossiaLrcType == (int)DIGLOSSIA_LRC_TYPE.ONLY_TRANSLATE)
            {
                return translateLrcs;
            }
            
            switch (diglossiaLrcType)
            {
                case (int)DIGLOSSIA_LRC_TYPE.ORIGIN_PRIOR: // 先显示原文，后显示译文
                    res = SortLrc(originLrcs, translateLrcs, true);
                    break;
                case (int)DIGLOSSIA_LRC_TYPE.TRANSLATE_PRIOR: // 先显示译文，后显示原文
                    res = SortLrc(originLrcs, translateLrcs, false);
                    break;
                case (int)DIGLOSSIA_LRC_TYPE.MERGE: // 合并显示
                    res = MergeLrc(originLrcs, translateLrcs, splitTextBox.Text);
                    break;
            }

            return res;
        }

        // 设置时间戳小数位数
        public void SetTimeStamp2Dot(ref string[] lrcStr)
        {
            for(int i=0;i<lrcStr.Length;i++)
            {
                int index = lrcStr[i].IndexOf("]");
                string timestamp = lrcStr[i].Substring(1, index - 1);
                string[] ts = timestamp.Split(':');

                bool hasNum = true;
                string tmp = "[";
                foreach (string t in ts)
                {
                    if(double.TryParse(t, out double num))
                    {
                        tmp = tmp + num.ToString("00.##") + ":";
                    }
                    else
                    {
                        hasNum = false;
                        break;
                    }
                }
                if(hasNum)
                {
                    lrcStr[i] = tmp.Substring(0, tmp.Length - 1) + "]" + lrcStr[i].Substring(index + 1);
                }
            }
        }

        // HTTP 返回
        private HttpStatus GetHttpStatus(JObject obj)
        {
            try
            {
                string code = obj["code"].ToString();
                if (code != HTTP_STATUS_SUCCESS)
                {
                    return new HttpStatus(code, obj["msg"].ToString());
                }

                JToken uncollected = obj["uncollected"];
                if(uncollected != null && (bool)uncollected)
                {
                    return new HttpStatus(HTTP_STATUS_NOT_FOUND, "歌词未被收集");
                }

                return new HttpStatus(HTTP_STATUS_SUCCESS, "成功");
            }
            catch (Exception ew)
            {
                Console.WriteLine("解析HTTP返回错误：" + ew);
                return new HttpStatus(HTTP_STATUS_INTERNAL_ERROR, "解析 HTTP 返回失败");
            }
        }

        private bool CheckNum(string s)
        {
            return Regex.Match(s, "^\\d+$").Success;
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
            string id = textBox_id.Text;
            if (id == "" || id == null || !CheckNum(id))
            {
                MessageBox.Show("【警告】ID 为空或格式非法！", "提示");
                return;
            }

            try
            {
                //歌曲类实例
                Song song = new Song();
                string lrc_url = string.Format(LRC_URL, id);

                HttpStatus status = GetSongLrc(HttpHelper(lrc_url).Replace("\\n", ""), ref song);
                if (status.GetCode() != HTTP_STATUS_SUCCESS)
                {
                    MessageBox.Show("获取歌词失败，错误信息：" + status.GetMsg(), "异常");
                    return;
                }

                // 双语歌词处理
                string[] lrcResult = ParserDiglossiaLrc(ref song);

                // 强制两位小数
                if (dotCheckBox.CheckState == CheckState.Checked)
                {
                    //设置时间戳两位小数
                    SetTimeStamp2Dot(ref lrcResult);
                }

                string song_url = string.Format(SONG_INFO_URL, id, id);

                status = GetSongBasicInfo(HttpHelper(song_url), ref song);
                if (status.GetCode() != HTTP_STATUS_SUCCESS)
                {
                    MessageBox.Show("请求歌曲信息失败，错误信息：" + status.GetMsg(), "异常");
                    return;
                }

                currentSong = song;
                textBox_lrc.Text = TrimLrc(lrcResult);
                textBox_song.Text = song.GetName();
                textBox_singer.Text = song.GetSinger();
            }
            catch (Exception ew)
            {
                MessageBox.Show("搜索失败，错误信息：\n" + ew.Message);
            }
        }

        // 保存按钮点击事件
        private void saveBtn_Click(object sender, EventArgs e)
        {
            if(currentSong == null)
            {
                MessageBox.Show("请先点击搜索按钮", "提示");
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog();
            try
            {
                string outputFileName = GetOutputFileName(ref currentSong);
                if (outputFileName == null)
                {
                    MessageBox.Show("命名格式发生错误！", "错误");
                    return;
                } else
                {
                    outputFileName = GetSafeFilename(outputFileName);
                }

                saveDialog.FileName = outputFileName;
                saveDialog.Filter = "lrc文件(*.lrc)|*.lrc|txt文件(*.txt)|*.txt";
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = saveDialog.FileName;
                    StreamWriter sw = File.AppendText(fileName);
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

        // 批量保存按钮点击事件
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

                    HttpStatus status = GetSongLrc(HttpHelper(string.Format(LRC_URL, id)).Replace("\\n", ""), ref song);

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
            if(failureMaps.Count > 0)
            {
                foreach (KeyValuePair<string, string> kvp in failureMaps)
                {
                    log.Append("ID: " + kvp.Key + ", Reason: " + kvp.Value + "\r\n");
                }
            }
            textBox_lrc.Text = log.ToString();

            // 保存
            if(resultMaps.Count > 0)
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
                            StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8);
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
        }

        private void comboBox_output_name_SelectedIndexChanged(object sender, EventArgs e)
        {
            outputFileNameType = comboBox_output_name.SelectedIndex;
        }

        private void comboBox_diglossia_lrc_SelectedIndexChanged(object sender, EventArgs e)
        {
            diglossiaLrcType = comboBox_diglossia_lrc.SelectedIndex;
            if(diglossiaLrcType == (int)DIGLOSSIA_LRC_TYPE.MERGE)
            {
                splitTextBox.ReadOnly = false;
            }
            else
            {
                splitTextBox.Text = null;
                splitTextBox.ReadOnly = true;
            }
        }
    }

    enum DIGLOSSIA_LRC_TYPE
    {
        ONLY_ORIGIN,
        ONLY_TRANSLATE,
        ORIGIN_PRIOR,
        TRANSLATE_PRIOR,
        MERGE
    }

    enum OUTPUT_FILENAME_TYPE
    {
        NAME_SINGER,
        SINGER_NAME,
        NAME
    }

    //歌曲类
    public class Song
    {
        private string name; //歌曲名

        private string singer; //歌手名

        private string lyric; //原文歌词

        private string tlyric; //翻译歌词
        
        public string GetName()
        {
            return this.name;
        }

        public void SetName(string s)
        {
            this.name = s;
        }

        public string GetSinger()
        {
            return this.singer;
        }

        public void SetSinger(string s)
        {
            this.singer = s;
        }

        public string GetLyric()
        {
            return this.lyric;
        }

        public void SetLyric(string s)
        {
            this.lyric = s;
        }

        public string GetTlyric()
        {
            return this.tlyric;
        }

        public void SetTlyric(string s)
        {
            this.tlyric = s;
        }
    }

    public class HttpStatus
    {
        private string code;

        private string msg;

        public HttpStatus()
        {
        }

        public HttpStatus(string code) : this(code, null)
        {
        }

        public HttpStatus(string code, string msg)
        {
            this.code = code;
            this.msg = msg;
        }

        public string GetCode()
        {
            return code;
        }

        public void setCode(string s)
        {
            code = s;
        }

        public string GetMsg()
        {
            return msg;
        }

        public void setMsg(string s)
        {
            msg = s;
        }
    }
}
