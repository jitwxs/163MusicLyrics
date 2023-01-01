using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MusicLyricApp.Bean;
using MusicLyricApp.Exception;

namespace MusicLyricApp
{
    public partial class SettingForm : MusicLyricForm
    {
        private readonly SettingBean _settingBean;

        public SettingForm(SettingBean settingBean)
        {
            _settingBean = settingBean;

            InitializeComponent();

            AfterInitializeComponent();
            
            ShowRomajiChangeListener(ShowRomaji_CheckBox.Checked);
            
            VerbatimLyricChangeListener(VerbatimLyric_CheckBox.Checked);
        }

        private void Save_Btn_Click(object sender, EventArgs e)
        {
            // 歌词时间戳
            _settingBean.Param.DotType = (DotTypeEnum)Dot_ComboBox.SelectedIndex;
            _settingBean.Param.LrcTimestampFormat = LrcTimestampFormat_TextBox.Text;
            _settingBean.Param.SrtTimestampFormat = SrtTimestampFormat_TextBox.Text;
            
            // 原文歌词
            _settingBean.Param.IgnoreEmptyLyric = IgnoreEmptyLyric_CheckBox.Checked;
            _settingBean.Param.EnableVerbatimLyric = VerbatimLyric_CheckBox.Checked;
            
            // 译文歌词
            _settingBean.Config.TranslateLyricDefaultRule = (TranslateLyricDefaultRuleEnum)TranLyricDefaultRule_ComboBox.SelectedIndex;
            _settingBean.Config.TranslateMatchPrecisionDeviation = int.Parse(TranslateMatchPrecisionDeviation_TextBox.Text);
            _settingBean.Config.RomajiConfig.Enable = ShowRomaji_CheckBox.Checked;
            _settingBean.Config.RomajiConfig.ModeEnum = (RomajiModeEnum)RomajiMode_ComboBox.SelectedIndex;
            _settingBean.Config.RomajiConfig.SystemEnum = (RomajiSystemEnum)RomajiSystem_ComboBox.SelectedIndex;
            
            // 输出设置
            _settingBean.Config.IgnorePureMusicInSave = IgnorePureMusicInSave_CheckBox.Checked;
            
            // 应用设置
            _settingBean.Config.RememberParam = RememberParam_CheckBox.Checked;
            _settingBean.Config.AutoReadClipboard = AutoReadClipboard_CheckBox.Checked;
            _settingBean.Config.AutoCheckUpdate = AutoCheckUpdate_CheckBox.Checked;
            _settingBean.Config.QQMusicCookie = QQMusic_Cookie_TextBox.Text;
            _settingBean.Config.NetEaseCookie = NetEase_Cookie_TextBox.Text;

            Close();
        }

        private void Help_Btn_Click(object sender, EventArgs e)
        {
            if (!(sender is Button input))
            {
                throw new MusicLyricException(ErrorMsg.SYSTEM_ERROR);
            }

            if (input == TimestampHelp_Button)
            {
                SettingTips_TextBox.Text =
                    Constants.HelpTips.GetContent(Constants.HelpTips.TypeEnum.TIME_STAMP_SETTING);
            }
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

        private void LrcMatchDigit_TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar!='\b' && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void VerbatimLyric_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            VerbatimLyricChangeListener(VerbatimLyric_CheckBox.Checked);
        }
        
        private void VerbatimLyricChangeListener(bool isEnable)
        {
            // 检查依赖
            if (isEnable && Constants.VerbatimLyricDependency.Any(e => !File.Exists(e)))
            {
                MessageBox.Show(string.Format(ErrorMsg.DEPENDENCY_LOSS, "Verbatim"), "提示");
                VerbatimLyric_CheckBox.Checked = false;
            }
        }
    }
}