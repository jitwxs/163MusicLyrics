using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;
using 网易云歌词提取.JsonBase;

namespace 网易云歌词提取
{
    public partial class 软件更新 : Form
    {
        private readonly GitHubInfo _gitHubInfo;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public 软件更新(GitHubInfo info)
        {
            if (info == null)
            {
                var ex = new ArgumentNullException(nameof(info));
                _logger.Error(ex);
                throw ex;
            }

            InitializeComponent();
            _gitHubInfo = info;

            softwareText.Text = info.Name;
            textBox_UpdateContent.Text = info.Body;
            label_UpdateDate.Text = $"更新日期：{info.PublishedAt}";
            label_authorName.Text = $"作者：{info.Author.Login}";
            label_downloadCount.Text = $"下载次数：{info.Assets[0].DownloadCount}";
            label_FileSize.Text = $"文件大小：{GetKB(info.Assets[0].Size):F1} KB";
        }

        private static double GetKB(long byteSize)
        {
            return ((double)byteSize) / 1024;
        }

        private void buttonDownload_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(_gitHubInfo.HtmlUrl.ToString());
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "打开失败");
            }
        }
    }
}
