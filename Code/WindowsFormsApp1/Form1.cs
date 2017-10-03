using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        Song song;//歌曲类实例
        
        int index = 0;//保存文件名类型索引

        public Form1()
        {
            InitializeComponent();
            comboBox.SelectedIndex = 0;
        }

        private void confirmBtn_Click(object sender, EventArgs e)
        {
            String id = textBox_id.Text;
            if (id == "" || id == null)
            {
                MessageBox.Show("请先输入id号！", "提示");
            }
            else
            {
                song = new Song();

                try
                {
                    String lrc_url = "http://music.163.com/api/song/lyric?os=pc&id=" + id + "&lv=-1&kv=-1&tv=-1";

                    getResult(httpHelper(lrc_url).Replace("\\n", ""));

                    String result = "";

                    if ("".Equals(song.getLyric()))
                    {
                        MessageBox.Show("获取歌曲发生错误，请检查歌曲id是否输入正确。", "异常");
                    }
                    else
                    {
                        result = sortLyric();
                    }
                    
                    if (dotCheckBox.CheckState == CheckState.Checked)
                    {
                        //设置时间戳小数，取值为0~2，默认为3（不要手动设为3，会出bug）
                        result = setDot(result, 2);
                    }
                    
                    String song_url = "http://music.163.com/api/song/detail/?id=" + id + "&ids=[" + id + "]";

                    getInfo(httpHelper(song_url));

                    textBox_lrc.Text = result;
                    textBox_song.Text = song.getName();
                    textBox_singer.Text = song.getSinger();
                }
                catch(Exception ew)
                {
                    MessageBox.Show("严重错误：程序处理异常！\n" + ew.Message);
                }
            }
        }

        //获取歌曲、歌手信息
        private void getInfo(String t)
        {
            Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(t);
            try
            {
                String songName = obj["songs"][0]["name"].ToString();
                song.setName(songName);

            }
            catch (Exception ew)
            {
                song.setName("");
            }

            try
            {
                String singer = obj["songs"][0]["artists"][0]["name"].ToString();
                song.setSinger(singer);

            }
            catch (Exception ew)
            {
                song.setSinger("");
            }
        }

        //歌词排序
        private String sortLyric()
        {

            String result = "";
            String[] tmp = song.getLrcAndTlyric().Split('[');

            String[] lyric = new String[tmp.Length - 1];

            for(int i = 1; i < tmp.Length; i++)
            {
                lyric[i - 1] = "[" + tmp[i];
            }
            Boolean flag = false;

            for(int i = 0; i < lyric.Length; i++)
            {
                if(Regex.IsMatch(lyric[i], "^\\[\\d+:\\d+\\.\\d+\\]$") == true)
                {
                    flag = true;
                    break;
                }
            }

            //仅对没有重复歌词进行排序（典型例子id：428642270）
            if (flag == false)
            {
                Array.Sort(lyric);
            }

            foreach (String i in lyric)
                result += i + "\r\n";

            return result;
        }

        //设置时间戳两位小数
        private String setDot(string ss,int value)
        {
            String result = "";
            String[] tmp = ss.Split('[');

            String[] lyric = new String[tmp.Length - 1];

            for (int i = 1; i < tmp.Length; i++)
            {
                Regex r = new Regex(@"\d+:\d+");
                if (r.IsMatch(tmp[i]))
                {
                    int start = tmp[i].IndexOf(':') + 1;
                    int end = tmp[i].IndexOf(']') + 1;
                    lyric[i - 1] = "[" + tmp[i].Substring(0, start) + tmp[i].Substring(start, 7 - value) + "]" + tmp[i].Substring(end);
                }
                else
                {
                    lyric[i - 1] = "[" + tmp[i];
                }
            }

            foreach (String i in lyric)
                result += i;
            return result;
        }

        //保存文件
        private void saveBtn_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            try
            {
                String StyleName = "";
                switch (index)
                {
                    case 0:
                        StyleName = song.getName() + " - " + song.getSinger();
                        break;
                    case 1:
                        StyleName = song.getSinger() + " - " + song.getName();
                        break;
                    case 2:
                        StyleName = song.getName();
                        break;
                }
                if (StyleName != "")
                {
                    saveDialog.FileName = StyleName;
                }
                else
                {
                    MessageBox.Show("命名格式发生错误！", "错误");
                }

                saveDialog.Filter = "lrc文件(*.lrc)|*.lrc|txt文件(*.txt)|*.txt";
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    String fileName = saveDialog.FileName;
                    StreamWriter sw = File.AppendText(fileName);
                    sw.Write(textBox_lrc.Text);
                    sw.Flush();
                    sw.Close();
                    MessageBox.Show("保存成功！", "提示");
                }
            }
            catch (Exception ew)
            {
                MessageBox.Show("严重错误：保存出现异常！");
            }
        }
        
        //GET请求服务器
        private String httpHelper(String url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 2000;
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
            String retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }

        //得到返回码、歌词、翻译歌词信息
        public void getResult(String temp)
        {
            Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(temp);
            try
            {
                String code = obj["code"].ToString();
                song.setCode(code);
            }
            catch(Exception ew)
            {
                song.setCode("");
            }

            try
            {
                String lyric = obj["lrc"]["lyric"].ToString();
                song.setLyric(lyric);
            }
            catch (Exception ew)
            {
                song.setLyric("");
            }

            try
            {
                String tlyric = obj["tlyric"]["lyric"].ToString();
                song.setTlyric(tlyric);
            }
            catch (Exception ew)
            {
                song.setTlyric("");
            }
        }

        private void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            index = comboBox.SelectedIndex;
        }
    }

    //歌曲类
    public class Song
    {
        private String name; //歌曲名

        private String singer; //歌手名

        private String code; //返回码:200正常

        private String lyric; //原文歌词

        private String tlyric; //翻译歌词
        
        public String getName()
        {
            return this.name;
        }

        public void setName(String s)
        {
            this.name = s;
        }

        public String getSinger()
        {
            return this.singer;
        }

        public void setSinger(String s)
        {
            this.singer = s;
        }

        public String getCode()
        {
            return this.code;
        }

        public void setCode(String s)
        {
            this.code = s;
        }

        public String getLyric()
        {
            return this.lyric;
        }

        public void setLyric(String s)
        {
            this.lyric = s;
        }

        public String getTlyric()
        {
            return this.tlyric;
        }

        public void setTlyric(String s)
        {
            this.tlyric = s;
        }
        
        public String getLrcAndTlyric()
        {
            return this.lyric + this.tlyric;
        }
        
    }
}
