using System;
using System.Windows.Forms;
using Markdig;
using MusicLyricApp.Bean;
using NLog;

namespace MusicLyricApp
{
    public partial class UpgradeForm : Form
    {
        private readonly GitHubInfo _gitHubInfo;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const string SEPARATOR = " | ";

        public UpgradeForm(GitHubInfo info)
        {
            if (info == null)
            {
                var ex = new ArgumentNullException(nameof(info));
                Logger.Error(ex);
                throw ex;
            }
            
            InitializeComponent();
            
            _gitHubInfo = info;

            UpgradeTag_Label.Text = info.TagName;
            UpgradeSpan1_Label.Text = $"更新日期：{info.PublishedAt.DateTime}" + SEPARATOR + $"下载次数：{info.Assets[0].DownloadCount}";
            UpgradeSpan2_Label.Text = $"作者：{info.Author.Login}"  + SEPARATOR + $"文件大小：{GetKb(info.Assets[0].Size):F1} KB";
            UpgradeLog_Browser.DocumentText = Markdown.ToHtml(info.Body);
        }
        
        private static double GetKb(long byteSize)
        {
            return ((double)byteSize) / 1024;
        }

        private void Upgrade_Btn_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(_gitHubInfo.HtmlUrl.ToString());
            }
            catch (System.Exception ex)
            {
                Logger.Error(ex, "打开失败");
            }
        }
    }
}