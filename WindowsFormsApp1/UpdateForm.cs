using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using 网易云歌词提取.JsonBase;

namespace 网易云歌词提取
{
    public partial class UpdateForm : Form
    {
        public UpdateForm(GitHubInfo info)
        {
            InitializeComponent();

            softwareText.Text = info.Name;
            textBox_UpdateContent.Text = info.Body;
            label_UpdateDate.Text = $"更新日期：{info.PublishedAt}";
            label_authorName.Text = $"作者：{info.Author.Login}";
            label_downloadCount.Text = $"下载次数：{info.Assets[0].DownloadCount}";
        }
    }
}
