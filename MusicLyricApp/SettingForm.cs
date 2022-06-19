using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MusicLyricApp.Bean;

namespace MusicLyricApp
{
    public partial class SettingForm : Form
    {
        private readonly SettingBean _settingBean;

        public SettingForm(SettingBean settingBean)
        {
            _settingBean = settingBean;

            InitializeComponent();

            AfterInitializeComponent();
            
            ShowRomajiChangeListener(ShowRomaji_CheckBox.Checked);
        }

        private void Save_Btn_Click(object sender, EventArgs e)
        {
            _settingBean.Config.RememberParam = RememberParam_CheckBox.Checked;
            _settingBean.Config.AutoReadClipboard = AutoReadClipboard_CheckBox.Checked;
            _settingBean.Config.AutoCheckUpdate = AutoCheckUpdate_CheckBox.Checked;

            _settingBean.Param.DotType = (DotTypeEnum)Dot_ComboBox.SelectedIndex;
            _settingBean.Param.LrcTimestampFormat = LrcTimestampFormat_TextBox.Text;
            _settingBean.Param.SrtTimestampFormat = SrtTimestampFormat_TextBox.Text;

            _settingBean.Config.RomajiConfig.Enable = ShowRomaji_CheckBox.Checked;
            _settingBean.Config.RomajiConfig.ModeEnum = (RomajiModeEnum)RomajiMode_ComboBox.SelectedIndex;
            _settingBean.Config.RomajiConfig.SystemEnum = (RomajiSystemEnum)RomajiSystem_ComboBox.SelectedIndex;

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
                MessageBox.Show(string.Format(ErrorMsg.DEPENDENCY_LOSS, "IpaDic"), "提示");
                ShowRomaji_CheckBox.Checked = false;

                isEnable = false;
            }
            
            RomajiMode_ComboBox.Enabled = isEnable;
            RomajiSystem_ComboBox.Enabled = isEnable;
        }
    }
}