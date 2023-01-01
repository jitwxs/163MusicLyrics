using System;
using System.ComponentModel;
using MusicLyricApp.Bean;
using MusicLyricApp.Utils;

namespace MusicLyricApp
{
    partial class SettingForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void AfterInitializeComponent()
        {
            // 歌词时间戳
            Dot_ComboBox.Items.AddRange(GlobalUtils.GetEnumDescArray<DotTypeEnum>());
            Dot_ComboBox.SelectedIndex = (int)_settingBean.Param.DotType;
            LrcTimestampFormat_TextBox.Text = _settingBean.Param.LrcTimestampFormat;
            SrtTimestampFormat_TextBox.Text = _settingBean.Param.SrtTimestampFormat;
            
            // 原文歌词
            IgnoreEmptyLyric_CheckBox.Checked = _settingBean.Param.IgnoreEmptyLyric;
            VerbatimLyric_CheckBox.Checked = _settingBean.Param.EnableVerbatimLyric;
            
            // 译文歌词
            TransLyricDefaultRule_ComboBox.Items.AddRange(GlobalUtils.GetEnumDescArray<TranslateLyricDefaultRuleEnum>());
            TransLyricDefaultRule_ComboBox.SelectedIndex = (int)_settingBean.Config.TranslateLyricDefaultRule;
            TranslateMatchPrecisionDeviation_TextBox.Text = _settingBean.Config.TranslateMatchPrecisionDeviation.ToString();
            Romaji_RadioBtn.Checked = _settingBean.Config.RomajiConfig.Enable;
            RomajiMode_ComboBox.Items.AddRange(GlobalUtils.GetEnumDescArray<RomajiModeEnum>());
            RomajiMode_ComboBox.SelectedIndex = (int)_settingBean.Config.RomajiConfig.ModeEnum;
            RomajiSystem_ComboBox.Items.AddRange(GlobalUtils.GetEnumDescArray<RomajiSystemEnum>()); 
            RomajiSystem_ComboBox.SelectedIndex = (int)_settingBean.Config.RomajiConfig.SystemEnum;
            
            // 输出设置
            IgnorePureMusicInSave_CheckBox.Checked = _settingBean.Config.IgnorePureMusicInSave;
            OutputName_TextBox.Text = _settingBean.Config.OutputFileNameFormat;
            
            // 应用设置
            RememberParam_CheckBox.Checked = _settingBean.Config.RememberParam;
            AutoReadClipboard_CheckBox.Checked = _settingBean.Config.AutoReadClipboard;
            AutoCheckUpdate_CheckBox.Checked = _settingBean.Config.AutoCheckUpdate;
            QQMusic_Cookie_TextBox.Text = _settingBean.Config.QQMusicCookie;
            NetEase_Cookie_TextBox.Text = _settingBean.Config.NetEaseCookie;
            
            // 提示区
            SettingTips_TextBox.Text = Constants.HelpTips.Prefix;
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingForm));
            this.Save_Btn = new System.Windows.Forms.Button();
            this.RememberParam_CheckBox = new System.Windows.Forms.CheckBox();
            this.AutoReadClipboard_CheckBox = new System.Windows.Forms.CheckBox();
            this.AutoCheckUpdate_CheckBox = new System.Windows.Forms.CheckBox();
            this.RomajiMode_ComboBox = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.RomajiSystem_ComboBox = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.LrcTimestampFormat_TextBox = new System.Windows.Forms.TextBox();
            this.SrtTimestampFormat_TextBox = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.Dot_ComboBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.TransLyricDefaultRule_ComboBox = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.TranslateMatchPrecisionDeviation_TextBox = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.IgnoreEmptyLyric_CheckBox = new System.Windows.Forms.CheckBox();
            this.VerbatimLyric_CheckBox = new System.Windows.Forms.CheckBox();
            this.NetEase_Cookie_TextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.QQMusic_Cookie_TextBox = new System.Windows.Forms.TextBox();
            this.Timestamp_GroupBox = new System.Windows.Forms.GroupBox();
            this.TimestampHelp_Button = new System.Windows.Forms.Button();
            this.SettingTips_TextBox = new System.Windows.Forms.TextBox();
            this.OutputHelp_Button = new System.Windows.Forms.Button();
            this.AppConfig_GroupBox = new System.Windows.Forms.GroupBox();
            this.OriginLyric_GroupBox = new System.Windows.Forms.GroupBox();
            this.TransLyric_GroupBox = new System.Windows.Forms.GroupBox();
            this.TransTranslate_GroupBox = new System.Windows.Forms.GroupBox();
            this.Romaji_RadioBtn = new System.Windows.Forms.RadioButton();
            this.English_RadioBtn = new System.Windows.Forms.RadioButton();
            this.ChineseTanslate_RadioBtn = new System.Windows.Forms.RadioButton();
            this.Output_GroupBox = new System.Windows.Forms.GroupBox();
            this.OutputName_TextBox = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.IgnorePureMusicInSave_CheckBox = new System.Windows.Forms.CheckBox();
            this.Reset_Btn = new System.Windows.Forms.Button();
            this.Timestamp_GroupBox.SuspendLayout();
            this.AppConfig_GroupBox.SuspendLayout();
            this.OriginLyric_GroupBox.SuspendLayout();
            this.TransLyric_GroupBox.SuspendLayout();
            this.TransTranslate_GroupBox.SuspendLayout();
            this.Output_GroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // Save_Btn
            // 
            this.Save_Btn.BackColor = System.Drawing.Color.Honeydew;
            this.Save_Btn.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.Save_Btn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Save_Btn.Location = new System.Drawing.Point(367, 497);
            this.Save_Btn.Name = "Save_Btn";
            this.Save_Btn.Size = new System.Drawing.Size(97, 50);
            this.Save_Btn.TabIndex = 0;
            this.Save_Btn.Text = "保存";
            this.Save_Btn.UseVisualStyleBackColor = false;
            this.Save_Btn.Click += new System.EventHandler(this.Close_Btn_Click);
            // 
            // RememberParam_CheckBox
            // 
            this.RememberParam_CheckBox.Location = new System.Drawing.Point(6, 33);
            this.RememberParam_CheckBox.Name = "RememberParam_CheckBox";
            this.RememberParam_CheckBox.Size = new System.Drawing.Size(78, 24);
            this.RememberParam_CheckBox.TabIndex = 1;
            this.RememberParam_CheckBox.Text = "参数记忆";
            this.RememberParam_CheckBox.UseVisualStyleBackColor = true;
            // 
            // AutoReadClipboard_CheckBox
            // 
            this.AutoReadClipboard_CheckBox.Location = new System.Drawing.Point(98, 31);
            this.AutoReadClipboard_CheckBox.Name = "AutoReadClipboard_CheckBox";
            this.AutoReadClipboard_CheckBox.Size = new System.Drawing.Size(112, 26);
            this.AutoReadClipboard_CheckBox.TabIndex = 2;
            this.AutoReadClipboard_CheckBox.Text = "自动读取剪贴板";
            this.AutoReadClipboard_CheckBox.UseVisualStyleBackColor = true;
            // 
            // AutoCheckUpdate_CheckBox
            // 
            this.AutoCheckUpdate_CheckBox.Location = new System.Drawing.Point(226, 37);
            this.AutoCheckUpdate_CheckBox.Name = "AutoCheckUpdate_CheckBox";
            this.AutoCheckUpdate_CheckBox.Size = new System.Drawing.Size(98, 17);
            this.AutoCheckUpdate_CheckBox.TabIndex = 3;
            this.AutoCheckUpdate_CheckBox.Text = "自动检查更新";
            this.AutoCheckUpdate_CheckBox.UseVisualStyleBackColor = true;
            // 
            // RomajiMode_ComboBox
            // 
            this.RomajiMode_ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.RomajiMode_ComboBox.FormattingEnabled = true;
            this.RomajiMode_ComboBox.Location = new System.Drawing.Point(283, 73);
            this.RomajiMode_ComboBox.Name = "RomajiMode_ComboBox";
            this.RomajiMode_ComboBox.Size = new System.Drawing.Size(100, 20);
            this.RomajiMode_ComboBox.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(213, 76);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(60, 12);
            this.label3.TabIndex = 8;
            this.label3.Text = "转换模式";
            // 
            // RomajiSystem_ComboBox
            // 
            this.RomajiSystem_ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.RomajiSystem_ComboBox.FormattingEnabled = true;
            this.RomajiSystem_ComboBox.Location = new System.Drawing.Point(97, 73);
            this.RomajiSystem_ComboBox.Name = "RomajiSystem_ComboBox";
            this.RomajiSystem_ComboBox.Size = new System.Drawing.Size(100, 20);
            this.RomajiSystem_ComboBox.TabIndex = 10;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(8, 36);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(65, 12);
            this.label6.TabIndex = 13;
            this.label6.Text = "LRC 时间戳";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(8, 75);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(65, 12);
            this.label7.TabIndex = 14;
            this.label7.Text = "SRT 时间戳";
            // 
            // LrcTimestampFormat_TextBox
            // 
            this.LrcTimestampFormat_TextBox.Location = new System.Drawing.Point(103, 33);
            this.LrcTimestampFormat_TextBox.Name = "LrcTimestampFormat_TextBox";
            this.LrcTimestampFormat_TextBox.Size = new System.Drawing.Size(100, 21);
            this.LrcTimestampFormat_TextBox.TabIndex = 15;
            // 
            // SrtTimestampFormat_TextBox
            // 
            this.SrtTimestampFormat_TextBox.Location = new System.Drawing.Point(103, 72);
            this.SrtTimestampFormat_TextBox.Name = "SrtTimestampFormat_TextBox";
            this.SrtTimestampFormat_TextBox.Size = new System.Drawing.Size(100, 21);
            this.SrtTimestampFormat_TextBox.TabIndex = 16;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(8, 115);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(77, 12);
            this.label8.TabIndex = 17;
            this.label8.Text = "毫秒截位规则";
            // 
            // Dot_ComboBox
            // 
            this.Dot_ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Dot_ComboBox.FormattingEnabled = true;
            this.Dot_ComboBox.Location = new System.Drawing.Point(103, 112);
            this.Dot_ComboBox.Name = "Dot_ComboBox";
            this.Dot_ComboBox.Size = new System.Drawing.Size(100, 20);
            this.Dot_ComboBox.TabIndex = 18;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(13, 36);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(82, 12);
            this.label1.TabIndex = 19;
            this.label1.Text = "译文缺省规则";
            // 
            // TransLyricDefaultRule_ComboBox
            // 
            this.TransLyricDefaultRule_ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.TransLyricDefaultRule_ComboBox.FormattingEnabled = true;
            this.TransLyricDefaultRule_ComboBox.Location = new System.Drawing.Point(110, 33);
            this.TransLyricDefaultRule_ComboBox.Name = "TransLyricDefaultRule_ComboBox";
            this.TransLyricDefaultRule_ComboBox.Size = new System.Drawing.Size(98, 20);
            this.TransLyricDefaultRule_ComboBox.TabIndex = 20;
            this.TransLyricDefaultRule_ComboBox.SelectedIndexChanged += new System.EventHandler(this.TransLyricDefaultRule_ComboBox_SelectedIndexChanged);
            // 
            // label9
            // 
            this.label9.Location = new System.Drawing.Point(226, 36);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(82, 12);
            this.label9.TabIndex = 21;
            this.label9.Text = "译文匹配精度";
            // 
            // TranslateMatchPrecisionDeviation_TextBox
            // 
            this.TranslateMatchPrecisionDeviation_TextBox.Location = new System.Drawing.Point(323, 33);
            this.TranslateMatchPrecisionDeviation_TextBox.Name = "TranslateMatchPrecisionDeviation_TextBox";
            this.TranslateMatchPrecisionDeviation_TextBox.Size = new System.Drawing.Size(73, 21);
            this.TranslateMatchPrecisionDeviation_TextBox.TabIndex = 22;
            this.TranslateMatchPrecisionDeviation_TextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.LrcMatchDigit_TextBox_KeyPress);
            // 
            // label5
            // 
            this.label5.Font = new System.Drawing.Font("宋体", 10F);
            this.label5.Location = new System.Drawing.Point(402, 36);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(24, 12);
            this.label5.TabIndex = 23;
            this.label5.Text = "MS";
            // 
            // IgnoreEmptyLyric_CheckBox
            // 
            this.IgnoreEmptyLyric_CheckBox.Location = new System.Drawing.Point(6, 35);
            this.IgnoreEmptyLyric_CheckBox.Name = "IgnoreEmptyLyric_CheckBox";
            this.IgnoreEmptyLyric_CheckBox.Size = new System.Drawing.Size(110, 17);
            this.IgnoreEmptyLyric_CheckBox.TabIndex = 24;
            this.IgnoreEmptyLyric_CheckBox.Text = "跳过空白歌词行";
            this.IgnoreEmptyLyric_CheckBox.UseVisualStyleBackColor = true;
            // 
            // VerbatimLyric_CheckBox
            // 
            this.VerbatimLyric_CheckBox.Location = new System.Drawing.Point(6, 73);
            this.VerbatimLyric_CheckBox.Name = "VerbatimLyric_CheckBox";
            this.VerbatimLyric_CheckBox.Size = new System.Drawing.Size(110, 17);
            this.VerbatimLyric_CheckBox.TabIndex = 25;
            this.VerbatimLyric_CheckBox.Text = "QQ音乐逐字歌词";
            this.VerbatimLyric_CheckBox.UseVisualStyleBackColor = true;
            this.VerbatimLyric_CheckBox.CheckedChanged += new System.EventHandler(this.VerbatimLyric_CheckBox_CheckedChanged);
            // 
            // NetEase_Cookie_TextBox
            // 
            this.NetEase_Cookie_TextBox.Location = new System.Drawing.Point(105, 75);
            this.NetEase_Cookie_TextBox.Name = "NetEase_Cookie_TextBox";
            this.NetEase_Cookie_TextBox.Size = new System.Drawing.Size(323, 21);
            this.NetEase_Cookie_TextBox.TabIndex = 26;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(6, 78);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 12);
            this.label2.TabIndex = 27;
            this.label2.Text = "网易云Cookie";
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(6, 122);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(80, 12);
            this.label4.TabIndex = 28;
            this.label4.Text = "QQ音乐Cookie";
            // 
            // QQMusic_Cookie_TextBox
            // 
            this.QQMusic_Cookie_TextBox.Location = new System.Drawing.Point(105, 122);
            this.QQMusic_Cookie_TextBox.Name = "QQMusic_Cookie_TextBox";
            this.QQMusic_Cookie_TextBox.Size = new System.Drawing.Size(323, 21);
            this.QQMusic_Cookie_TextBox.TabIndex = 29;
            // 
            // Timestamp_GroupBox
            // 
            this.Timestamp_GroupBox.Controls.Add(this.Dot_ComboBox);
            this.Timestamp_GroupBox.Controls.Add(this.LrcTimestampFormat_TextBox);
            this.Timestamp_GroupBox.Controls.Add(this.label8);
            this.Timestamp_GroupBox.Controls.Add(this.label6);
            this.Timestamp_GroupBox.Controls.Add(this.SrtTimestampFormat_TextBox);
            this.Timestamp_GroupBox.Controls.Add(this.label7);
            this.Timestamp_GroupBox.Location = new System.Drawing.Point(12, 12);
            this.Timestamp_GroupBox.Name = "Timestamp_GroupBox";
            this.Timestamp_GroupBox.Size = new System.Drawing.Size(227, 155);
            this.Timestamp_GroupBox.TabIndex = 30;
            this.Timestamp_GroupBox.TabStop = false;
            this.Timestamp_GroupBox.Text = "歌词时间戳";
            // 
            // TimestampHelp_Button
            // 
            this.TimestampHelp_Button.ForeColor = System.Drawing.Color.Red;
            this.TimestampHelp_Button.Location = new System.Drawing.Point(212, 12);
            this.TimestampHelp_Button.Name = "TimestampHelp_Button";
            this.TimestampHelp_Button.Size = new System.Drawing.Size(21, 21);
            this.TimestampHelp_Button.TabIndex = 19;
            this.TimestampHelp_Button.Text = "?";
            this.TimestampHelp_Button.UseVisualStyleBackColor = true;
            this.TimestampHelp_Button.Click += new System.EventHandler(this.Help_Btn_Click);
            // 
            // SettingTips_TextBox
            // 
            this.SettingTips_TextBox.BackColor = System.Drawing.SystemColors.Info;
            this.SettingTips_TextBox.Location = new System.Drawing.Point(485, 235);
            this.SettingTips_TextBox.Multiline = true;
            this.SettingTips_TextBox.Name = "SettingTips_TextBox";
            this.SettingTips_TextBox.ReadOnly = true;
            this.SettingTips_TextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.SettingTips_TextBox.Size = new System.Drawing.Size(458, 312);
            this.SettingTips_TextBox.TabIndex = 31;
            // 
            // OutputHelp_Button
            // 
            this.OutputHelp_Button.ForeColor = System.Drawing.Color.Red;
            this.OutputHelp_Button.Location = new System.Drawing.Point(425, 0);
            this.OutputHelp_Button.Name = "OutputHelp_Button";
            this.OutputHelp_Button.Size = new System.Drawing.Size(21, 21);
            this.OutputHelp_Button.TabIndex = 20;
            this.OutputHelp_Button.Text = "?";
            this.OutputHelp_Button.UseVisualStyleBackColor = true;
            this.OutputHelp_Button.Click += new System.EventHandler(this.Help_Btn_Click);
            // 
            // AppConfig_GroupBox
            // 
            this.AppConfig_GroupBox.Controls.Add(this.AutoCheckUpdate_CheckBox);
            this.AppConfig_GroupBox.Controls.Add(this.AutoReadClipboard_CheckBox);
            this.AppConfig_GroupBox.Controls.Add(this.RememberParam_CheckBox);
            this.AppConfig_GroupBox.Controls.Add(this.NetEase_Cookie_TextBox);
            this.AppConfig_GroupBox.Controls.Add(this.label2);
            this.AppConfig_GroupBox.Controls.Add(this.QQMusic_Cookie_TextBox);
            this.AppConfig_GroupBox.Controls.Add(this.label4);
            this.AppConfig_GroupBox.Location = new System.Drawing.Point(12, 183);
            this.AppConfig_GroupBox.Name = "AppConfig_GroupBox";
            this.AppConfig_GroupBox.Size = new System.Drawing.Size(452, 164);
            this.AppConfig_GroupBox.TabIndex = 32;
            this.AppConfig_GroupBox.TabStop = false;
            this.AppConfig_GroupBox.Text = "应用设置";
            // 
            // OriginLyric_GroupBox
            // 
            this.OriginLyric_GroupBox.Controls.Add(this.IgnoreEmptyLyric_CheckBox);
            this.OriginLyric_GroupBox.Controls.Add(this.VerbatimLyric_CheckBox);
            this.OriginLyric_GroupBox.Location = new System.Drawing.Point(254, 12);
            this.OriginLyric_GroupBox.Name = "OriginLyric_GroupBox";
            this.OriginLyric_GroupBox.Size = new System.Drawing.Size(210, 155);
            this.OriginLyric_GroupBox.TabIndex = 33;
            this.OriginLyric_GroupBox.TabStop = false;
            this.OriginLyric_GroupBox.Text = "原文歌词";
            // 
            // TransLyric_GroupBox
            // 
            this.TransLyric_GroupBox.Controls.Add(this.TransTranslate_GroupBox);
            this.TransLyric_GroupBox.Controls.Add(this.TransLyricDefaultRule_ComboBox);
            this.TransLyric_GroupBox.Controls.Add(this.label1);
            this.TransLyric_GroupBox.Controls.Add(this.TranslateMatchPrecisionDeviation_TextBox);
            this.TransLyric_GroupBox.Controls.Add(this.label9);
            this.TransLyric_GroupBox.Controls.Add(this.label5);
            this.TransLyric_GroupBox.Location = new System.Drawing.Point(483, 12);
            this.TransLyric_GroupBox.Name = "TransLyric_GroupBox";
            this.TransLyric_GroupBox.Size = new System.Drawing.Size(460, 200);
            this.TransLyric_GroupBox.TabIndex = 34;
            this.TransLyric_GroupBox.TabStop = false;
            this.TransLyric_GroupBox.Text = "译文歌词";
            // 
            // TransTranslate_GroupBox
            // 
            this.TransTranslate_GroupBox.Controls.Add(this.Romaji_RadioBtn);
            this.TransTranslate_GroupBox.Controls.Add(this.English_RadioBtn);
            this.TransTranslate_GroupBox.Controls.Add(this.ChineseTanslate_RadioBtn);
            this.TransTranslate_GroupBox.Controls.Add(this.RomajiMode_ComboBox);
            this.TransTranslate_GroupBox.Controls.Add(this.RomajiSystem_ComboBox);
            this.TransTranslate_GroupBox.Controls.Add(this.label3);
            this.TransTranslate_GroupBox.Location = new System.Drawing.Point(13, 73);
            this.TransTranslate_GroupBox.Name = "TransTranslate_GroupBox";
            this.TransTranslate_GroupBox.Size = new System.Drawing.Size(413, 112);
            this.TransTranslate_GroupBox.TabIndex = 36;
            this.TransTranslate_GroupBox.TabStop = false;
            this.TransTranslate_GroupBox.Text = "缺省自动翻译";
            // 
            // Romaji_RadioBtn
            // 
            this.Romaji_RadioBtn.Location = new System.Drawing.Point(17, 67);
            this.Romaji_RadioBtn.Name = "Romaji_RadioBtn";
            this.Romaji_RadioBtn.Size = new System.Drawing.Size(60, 30);
            this.Romaji_RadioBtn.TabIndex = 2;
            this.Romaji_RadioBtn.TabStop = true;
            this.Romaji_RadioBtn.Text = "罗马音";
            this.Romaji_RadioBtn.UseVisualStyleBackColor = true;
            this.Romaji_RadioBtn.CheckedChanged += new System.EventHandler(this.AutoTranslate_RadioBox_CheckedChanged);
            // 
            // English_RadioBtn
            // 
            this.English_RadioBtn.Location = new System.Drawing.Point(97, 27);
            this.English_RadioBtn.Name = "English_RadioBtn";
            this.English_RadioBtn.Size = new System.Drawing.Size(66, 30);
            this.English_RadioBtn.TabIndex = 1;
            this.English_RadioBtn.TabStop = true;
            this.English_RadioBtn.Text = "英文";
            this.English_RadioBtn.UseVisualStyleBackColor = true;
            this.English_RadioBtn.CheckedChanged += new System.EventHandler(this.AutoTranslate_RadioBox_CheckedChanged);
            // 
            // ChineseTanslate_RadioBtn
            // 
            this.ChineseTanslate_RadioBtn.Location = new System.Drawing.Point(17, 27);
            this.ChineseTanslate_RadioBtn.Name = "ChineseTanslate_RadioBtn";
            this.ChineseTanslate_RadioBtn.Size = new System.Drawing.Size(60, 30);
            this.ChineseTanslate_RadioBtn.TabIndex = 0;
            this.ChineseTanslate_RadioBtn.TabStop = true;
            this.ChineseTanslate_RadioBtn.Text = "中文";
            this.ChineseTanslate_RadioBtn.UseVisualStyleBackColor = true;
            this.ChineseTanslate_RadioBtn.CheckedChanged += new System.EventHandler(this.AutoTranslate_RadioBox_CheckedChanged);
            // 
            // Output_GroupBox
            // 
            this.Output_GroupBox.Controls.Add(this.OutputName_TextBox);
            this.Output_GroupBox.Controls.Add(this.label10);
            this.Output_GroupBox.Controls.Add(this.IgnorePureMusicInSave_CheckBox);
            this.Output_GroupBox.Controls.Add(this.OutputHelp_Button);
            this.Output_GroupBox.Location = new System.Drawing.Point(12, 365);
            this.Output_GroupBox.Name = "Output_GroupBox";
            this.Output_GroupBox.Size = new System.Drawing.Size(452, 112);
            this.Output_GroupBox.TabIndex = 35;
            this.Output_GroupBox.TabStop = false;
            this.Output_GroupBox.Text = "输出设置";
            // 
            // OutputName_TextBox
            // 
            this.OutputName_TextBox.Location = new System.Drawing.Point(110, 70);
            this.OutputName_TextBox.Name = "OutputName_TextBox";
            this.OutputName_TextBox.Size = new System.Drawing.Size(323, 21);
            this.OutputName_TextBox.TabIndex = 38;
            // 
            // label10
            // 
            this.label10.Location = new System.Drawing.Point(15, 73);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(65, 12);
            this.label10.TabIndex = 37;
            this.label10.Text = "输出文件名";
            // 
            // IgnorePureMusicInSave_CheckBox
            // 
            this.IgnorePureMusicInSave_CheckBox.Location = new System.Drawing.Point(13, 19);
            this.IgnorePureMusicInSave_CheckBox.Name = "IgnorePureMusicInSave_CheckBox";
            this.IgnorePureMusicInSave_CheckBox.Size = new System.Drawing.Size(93, 29);
            this.IgnorePureMusicInSave_CheckBox.TabIndex = 36;
            this.IgnorePureMusicInSave_CheckBox.Text = "跳过纯音乐";
            this.IgnorePureMusicInSave_CheckBox.UseVisualStyleBackColor = true;
            // 
            // Reset_Btn
            // 
            this.Reset_Btn.BackColor = System.Drawing.Color.OldLace;
            this.Reset_Btn.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.Reset_Btn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Reset_Btn.Location = new System.Drawing.Point(12, 497);
            this.Reset_Btn.Name = "Reset_Btn";
            this.Reset_Btn.Size = new System.Drawing.Size(97, 50);
            this.Reset_Btn.TabIndex = 36;
            this.Reset_Btn.Text = "重置";
            this.Reset_Btn.UseVisualStyleBackColor = false;
            this.Reset_Btn.Click += new System.EventHandler(this.Close_Btn_Click);
            // 
            // SettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(959, 564);
            this.Controls.Add(this.Reset_Btn);
            this.Controls.Add(this.Output_GroupBox);
            this.Controls.Add(this.TransLyric_GroupBox);
            this.Controls.Add(this.OriginLyric_GroupBox);
            this.Controls.Add(this.AppConfig_GroupBox);
            this.Controls.Add(this.TimestampHelp_Button);
            this.Controls.Add(this.SettingTips_TextBox);
            this.Controls.Add(this.Save_Btn);
            this.Controls.Add(this.Timestamp_GroupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "SettingForm";
            this.Text = "设置";
            this.Timestamp_GroupBox.ResumeLayout(false);
            this.Timestamp_GroupBox.PerformLayout();
            this.AppConfig_GroupBox.ResumeLayout(false);
            this.AppConfig_GroupBox.PerformLayout();
            this.OriginLyric_GroupBox.ResumeLayout(false);
            this.TransLyric_GroupBox.ResumeLayout(false);
            this.TransLyric_GroupBox.PerformLayout();
            this.TransTranslate_GroupBox.ResumeLayout(false);
            this.Output_GroupBox.ResumeLayout(false);
            this.Output_GroupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Button Reset_Btn;

        private System.Windows.Forms.GroupBox TransTranslate_GroupBox;
        private System.Windows.Forms.RadioButton ChineseTanslate_RadioBtn;
        private System.Windows.Forms.RadioButton English_RadioBtn;
        private System.Windows.Forms.RadioButton Romaji_RadioBtn;

        private System.Windows.Forms.TextBox OutputName_TextBox;

        private System.Windows.Forms.Label label10;

        private System.Windows.Forms.GroupBox Output_GroupBox;
        private System.Windows.Forms.CheckBox IgnorePureMusicInSave_CheckBox;

        private System.Windows.Forms.GroupBox TransLyric_GroupBox;

        private System.Windows.Forms.GroupBox OriginLyric_GroupBox;

        private System.Windows.Forms.GroupBox AppConfig_GroupBox;

        private System.Windows.Forms.Button OutputHelp_Button;

        private System.Windows.Forms.Button TimestampHelp_Button;

        private System.Windows.Forms.GroupBox Timestamp_GroupBox;
        private System.Windows.Forms.TextBox SettingTips_TextBox;

        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox QQMusic_Cookie_TextBox;

        private System.Windows.Forms.TextBox NetEase_Cookie_TextBox;
        private System.Windows.Forms.Label label2;

        private System.Windows.Forms.CheckBox VerbatimLyric_CheckBox;

        private System.Windows.Forms.CheckBox IgnoreEmptyLyric_CheckBox;

        private System.Windows.Forms.Label label5;

        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox TranslateMatchPrecisionDeviation_TextBox;

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox TransLyricDefaultRule_ComboBox;

        private System.Windows.Forms.ComboBox RomajiSystem_ComboBox;

        private System.Windows.Forms.ComboBox RomajiMode_ComboBox;
        private System.Windows.Forms.Label label3;

        private System.Windows.Forms.CheckBox AutoCheckUpdate_CheckBox;

        private System.Windows.Forms.CheckBox AutoReadClipboard_CheckBox;

        private System.Windows.Forms.CheckBox RememberParam_CheckBox;

        private System.Windows.Forms.Button Save_Btn;

        #endregion

        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox LrcTimestampFormat_TextBox;
        private System.Windows.Forms.TextBox SrtTimestampFormat_TextBox;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox Dot_ComboBox;
    }
}