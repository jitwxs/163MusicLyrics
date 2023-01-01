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
            
            ShowRomajiChangeListener();
            
            VerbatimLyricChangeListener(VerbatimLyric_CheckBox.Checked);
        }

        /// <summary>
        /// 保存 & 重置按钮，点击事件
        /// </summary>
        private void Close_Btn_Click(object sender, EventArgs e)
        {
            if (!(sender is Button input))
            {
                throw new MusicLyricException(ErrorMsg.SYSTEM_ERROR);
            }

            if (input == Save_Btn)
            {
                // 歌词时间戳
                _settingBean.Param.DotType = (DotTypeEnum)Dot_ComboBox.SelectedIndex;
                _settingBean.Param.LrcTimestampFormat = LrcTimestampFormat_TextBox.Text;
                _settingBean.Param.SrtTimestampFormat = SrtTimestampFormat_TextBox.Text;
            
                // 原文歌词
                _settingBean.Param.IgnoreEmptyLyric = IgnoreEmptyLyric_CheckBox.Checked;
                _settingBean.Param.EnableVerbatimLyric = VerbatimLyric_CheckBox.Checked;
            
                // 译文歌词
                _settingBean.Config.TranslateLyricDefaultRule = (TranslateLyricDefaultRuleEnum)TransLyricDefaultRule_ComboBox.SelectedIndex;
                _settingBean.Config.TranslateMatchPrecisionDeviation = int.Parse(TranslateMatchPrecisionDeviation_TextBox.Text);
                _settingBean.Config.RomajiConfig.Enable = Romaji_RadioBtn.Enabled;
                _settingBean.Config.RomajiConfig.ModeEnum = (RomajiModeEnum)RomajiMode_ComboBox.SelectedIndex;
                _settingBean.Config.RomajiConfig.SystemEnum = (RomajiSystemEnum)RomajiSystem_ComboBox.SelectedIndex;
            
                // 输出设置
                _settingBean.Config.IgnorePureMusicInSave = IgnorePureMusicInSave_CheckBox.Checked;
                _settingBean.Config.OutputFileNameFormat = OutputName_TextBox.Text;
            
                // 应用设置
                _settingBean.Config.RememberParam = RememberParam_CheckBox.Checked;
                _settingBean.Config.AutoReadClipboard = AutoReadClipboard_CheckBox.Checked;
                _settingBean.Config.AutoCheckUpdate = AutoCheckUpdate_CheckBox.Checked;
                _settingBean.Config.QQMusicCookie = QQMusic_Cookie_TextBox.Text;
                _settingBean.Config.NetEaseCookie = NetEase_Cookie_TextBox.Text;
            } 
            else if (input == Reset_Btn)
            {
                _settingBean.Param = new PersistParamBean();
                _settingBean.Config = new ConfigBean();
            }

            Close();
        }

        /// <summary>
        /// 帮助按钮，点击事件
        /// </summary>
        private void Help_Btn_Click(object sender, EventArgs e)
        {
            if (!(sender is Button input))
            {
                throw new MusicLyricException(ErrorMsg.SYSTEM_ERROR);
            }

            var typeEnum = Constants.HelpTips.TypeEnum.DEFAULT;
            if (input == TimestampHelp_Button)
            {
                typeEnum = Constants.HelpTips.TypeEnum.TIME_STAMP_SETTING;
            } 
            else if (input == OutputHelp_Button)
            {
                typeEnum = Constants.HelpTips.TypeEnum.OUTPUT_SETTING;
            }

            SettingTips_TextBox.Text = Constants.HelpTips.GetContent(typeEnum);
        }

        private void ShowRomajiChangeListener()
        {
            var isEnable = Romaji_RadioBtn.Checked;

            // 检查依赖
            if (isEnable && Constants.IpaDicDependency.Any(e => !File.Exists(e)))
            {
                MessageBox.Show(string.Format(ErrorMsg.DEPENDENCY_LOSS, "IpaDic"), "提示");
                Romaji_RadioBtn.Checked = false;

                isEnable = false;
            }
            
            RomajiMode_ComboBox.Enabled = isEnable;
            RomajiSystem_ComboBox.Enabled = isEnable;
        }

        /// <summary>
        /// 译文匹配精度输入内容有效性校验
        /// </summary>
        private void LrcMatchDigit_TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar!='\b' && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// 逐字歌词依赖检查
        /// </summary>
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

        /// <summary>
        /// 译文缺省规则，数值改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void TransLyricDefaultRule_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var translateLyricDefaultRule = (TranslateLyricDefaultRuleEnum) TransLyricDefaultRule_ComboBox.SelectedIndex;

            if (translateLyricDefaultRule == TranslateLyricDefaultRuleEnum.AUTO_TRANSLATE)
            {
                ChineseTanslate_RadioBtn.Enabled = true;
                ChineseTanslate_RadioBtn.Checked = true;
                English_RadioBtn.Enabled = true;
                English_RadioBtn.Checked = true;
                Romaji_RadioBtn.Enabled = true;
                Romaji_RadioBtn.Checked = true;
            }
            else
            {
                ChineseTanslate_RadioBtn.Enabled = false;
                ChineseTanslate_RadioBtn.Checked = false;
                English_RadioBtn.Enabled = false;
                English_RadioBtn.Checked = false;
                Romaji_RadioBtn.Enabled = false;
                Romaji_RadioBtn.Checked = false;
                
                ShowRomajiChangeListener();
            }
        }
        
        private void AutoTranslate_RadioBox_CheckedChanged(object sender, EventArgs e)
        {
            ShowRomajiChangeListener();
        }
    }
}