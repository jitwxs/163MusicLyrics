using System;
using System.Windows.Forms;
using MusicLyricApp.Bean;

namespace MusicLyricApp
{
    public partial class SettingForm : Form
    {
        private readonly ConfigBean _configBean;
        
        public SettingForm(ConfigBean configBean)
        {
            InitializeComponent();

            _configBean = configBean;

            checkBox_reme_param.Checked = _configBean.RememberParam;
            checkBox_auto_read_clipboard.Checked = _configBean.AutoReadClipboard;
            checkBox_auto_check_update.Checked = _configBean.AutoCheckUpdate;
        }

        private void btn_save_Click(object sender, EventArgs e)
        {
            _configBean.RememberParam = checkBox_reme_param.Checked;
            _configBean.AutoReadClipboard = checkBox_auto_read_clipboard.Checked;
            _configBean.AutoCheckUpdate = checkBox_auto_check_update.Checked;
            
            Close();
        }
    }
}