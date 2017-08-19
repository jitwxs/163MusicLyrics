using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        String song = null;
        String singer = null;
        int index = 0;//保存文件名类型索引

        public Form1()
        {
            InitializeComponent();
            comboBox.SelectedIndex = 0;
        }

        private void confirmBtn_Click(object sender, EventArgs e)
        {
            String id = textBox_id.Text;
            if (id == null || id == "")
            {
                MessageBox.Show("请先输入id号！", "提示");
            }
            else
            {
                String lrc_url = "http://music.163.com/api/song/lyric?os=pc&id=" + id + "&lv=-1&kv=-1&tv=-1";

                String temp = getResult(httpHelper(lrc_url).Replace("\\n", ""));

                String result = "";

                if(temp == "" || temp == null)
                {
                    MessageBox.Show("获取歌曲发生错误，请检查歌曲id是否输入正确。","异常");
                }
                else
                {
                    result = sortLyric(temp);
                }

                
                String song_url = "http://music.163.com/api/song/detail/?id=" + id + "&ids=[" + id + "]";

                getInfo(httpHelper(song_url));

                textBox_lrc.Text = result;
                textBox_song.Text = song;
                textBox_singer.Text = singer;
            }
        }

        //获取歌曲、歌手信息
        private void getInfo(string t)
        {
            String[] temps = t.Split(new char[2] { '{', '}' });
            String[] info = new String[2];
            int j = 0;
            for (int i = 0; i < temps.Length; i++)
            {
                if (temps[i].Contains("\"name\""))
                {
                    String k = temps[i].Substring(temps[i].IndexOf("\"name\""), temps[i].IndexOf(","));
                    info[j++] = k.Split(':')[1].Replace("\"", "");
                    if (j == 2)
                        break;
                }
            }
            song = info[0];
            singer = info[1];
        }

        //歌词排序
        private string sortLyric(string t)
        {
         
            String result = null;

            String[] tmp = t.Split('\n');

            Array.Sort(tmp);

            foreach (String i in tmp)
                result += i + "\r\n";

            return result;
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
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }

        //保存文件
        private void saveBtn_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            String StyleName = null;
            switch (index)
            {
                case 0:
                    StyleName = song + " - " + singer;
                    break;
                case 1:
                    StyleName = singer + " - " + song;
                    break;
                case 2:
                    StyleName = song;
                    break;
            }
            if (StyleName != null)
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

        public string getResult(String temp)
        {
            String result = null;
            String[] temps = temp.Split(new char[2] { '{', '}' });

            for (int k = 0; k < temps.Length; k++)
            {
                if (temps[k].Contains('['))
                {
                    temps[k] = temps[k].Substring(temps[k].IndexOf('['));

                    for (int i = 0; i < temps[k].Length; i++)
                    {
                        if (temps[k][i] == '[')
                        {
                            while (temps[k][i] != '"')
                            {
                                result += temps[k][i];
                                if (temps[k][i + 1] == '[')
                                    result += "\r\n";
                                i++;
                            }
                            break;
                        }
                    }
                }
            }
            return result;
        }

        private void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            index = comboBox.SelectedIndex;
        }
    }
}
