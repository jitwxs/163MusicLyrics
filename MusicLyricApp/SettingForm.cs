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

            RememberParam_ComboBox.Checked = _configBean.RememberParam;
            AutoReadClipboard_CheckBox.Checked = _configBean.AutoReadClipboard;
            AutoCheckUpdate_CheckBox.Checked = _configBean.AutoCheckUpdate;
        }

        private void Save_Btn_Click(object sender, EventArgs e)
        {
            _configBean.RememberParam = RememberParam_ComboBox.Checked;
            _configBean.AutoReadClipboard = AutoReadClipboard_CheckBox.Checked;
            _configBean.AutoCheckUpdate = AutoCheckUpdate_CheckBox.Checked;
            
            Close();
        }
    }
}