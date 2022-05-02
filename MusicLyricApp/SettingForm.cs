using System;
using System.IO;
using System.Linq;
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

            RememberParam_CheckBox.Checked = _configBean.RememberParam;
            AutoReadClipboard_CheckBox.Checked = _configBean.AutoReadClipboard;
            AutoCheckUpdate_CheckBox.Checked = _configBean.AutoCheckUpdate;

            var romajiConfig = _configBean.RomajiConfig;
            ShowRomaji_CheckBox.Checked = romajiConfig.Enable;
            RomajiMode_ComboBox.SelectedIndex = (int)romajiConfig.ModeEnum;
            RomajiSystem_ComboBox.SelectedIndex = (int)romajiConfig.SystemEnum;
            ShowRomajiChangeListener(ShowRomaji_CheckBox.Checked);
        }

        private void Save_Btn_Click(object sender, EventArgs e)
        {
            _configBean.RememberParam = RememberParam_CheckBox.Checked;
            _configBean.AutoReadClipboard = AutoReadClipboard_CheckBox.Checked;
            _configBean.AutoCheckUpdate = AutoCheckUpdate_CheckBox.Checked;

            var romajiConfig = _configBean.RomajiConfig;
            romajiConfig.Enable = ShowRomaji_CheckBox.Checked;
            romajiConfig.ModeEnum = (RomajiModeEnum)RomajiMode_ComboBox.SelectedIndex;
            romajiConfig.SystemEnum = (RomajiSystemEnum)RomajiSystem_ComboBox.SelectedIndex;

            Close();
        }

        private void ShowRomaji_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ShowRomajiChangeListener(ShowRomaji_CheckBox.Checked);
        }

        private void ShowRomajiChangeListener(bool isEnable)
        {
            // 检查依赖
            if (isEnable && Constants.IpaDicDependency.Any(e => !File.Exists(e)))
            {
                MessageBox.Show(ErrorMsg.ROMAJI_DEPENDENCY_LOSS, "提示");
                ShowRomaji_CheckBox.Checked = false;
                ShowRomajiChangeListener(ShowRomaji_CheckBox.Checked);
            }
            
            RomajiMode_ComboBox.Enabled = isEnable;
            RomajiSystem_ComboBox.Enabled = isEnable;
        }
    }
}