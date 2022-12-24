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
            this.RomajiMode_ComboBox.Items.AddRange(GlobalUtils.GetEnumDescArray<RomajiModeEnum>());
            this.RomajiSystem_ComboBox.Items.AddRange(GlobalUtils.GetEnumDescArray<RomajiSystemEnum>());
            this.Dot_ComboBox.Items.AddRange(GlobalUtils.GetEnumDescArray<DotTypeEnum>());
            this.TranLyricDefaultRule_ComboBox.Items.AddRange(GlobalUtils.GetEnumDescArray<TranslateLyricDefaultRuleEnum>());

            RememberParam_CheckBox.Checked = _settingBean.Config.RememberParam;
            AutoReadClipboard_CheckBox.Checked = _settingBean.Config.AutoReadClipboard;
            AutoCheckUpdate_CheckBox.Checked = _settingBean.Config.AutoCheckUpdate;
            VerbatimLyric_CheckBox.Checked = _settingBean.Config.EnableVerbatimLyric;
            IgnoreEmptyLyric_CheckBox.Checked = _settingBean.Param.IgnoreEmptyLyric;

            LrcTimestampFormat_TextBox.Text = _settingBean.Param.LrcTimestampFormat;
            SrtTimestampFormat_TextBox.Text = _settingBean.Param.SrtTimestampFormat;
            TranslateMatchPrecisionDeviation_TextBox.Text = _settingBean.Param.TranslateMatchPrecisionDeviation.ToString();
            Dot_ComboBox.SelectedIndex = (int)_settingBean.Param.DotType;

            ShowRomaji_CheckBox.Checked = _settingBean.Config.RomajiConfig.Enable;
            RomajiMode_ComboBox.SelectedIndex = (int)_settingBean.Config.RomajiConfig.ModeEnum;
            RomajiSystem_ComboBox.SelectedIndex = (int)_settingBean.Config.RomajiConfig.SystemEnum;
            TranLyricDefaultRule_ComboBox.SelectedIndex = (int)_settingBean.Config.TranslateLyricDefaultRule;

            QQMusic_Cookie_TextBox.Text = _settingBean.Config.QQMusicCookie;
            NetEase_Cookie_TextBox.Text = _settingBean.Config.NetEaseCookie;
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
            this.ShowRomaji_CheckBox = new System.Windows.Forms.CheckBox();
            this.RomajiMode_ComboBox = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.RomajiSystem_ComboBox = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.LrcTimestampFormat_TextBox = new System.Windows.Forms.TextBox();
            this.SrtTimestampFormat_TextBox = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.Dot_ComboBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.TranLyricDefaultRule_ComboBox = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.TranslateMatchPrecisionDeviation_TextBox = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.IgnoreEmptyLyric_CheckBox = new System.Windows.Forms.CheckBox();
            this.VerbatimLyric_CheckBox = new System.Windows.Forms.CheckBox();
            this.NetEase_Cookie_TextBox = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.QQMusic_Cookie_TextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // Save_Btn
            // 
            this.Save_Btn.Location = new System.Drawing.Point(550, 216);
            this.Save_Btn.Name = "Save_Btn";
            this.Save_Btn.Size = new System.Drawing.Size(98, 44);
            this.Save_Btn.TabIndex = 0;
            this.Save_Btn.Text = "保存";
            this.Save_Btn.UseVisualStyleBackColor = true;
            this.Save_Btn.Click += new System.EventHandler(this.Save_Btn_Click);
            // 
            // RememberParam_CheckBox
            // 
            this.RememberParam_CheckBox.Location = new System.Drawing.Point(445, 122);
            this.RememberParam_CheckBox.Name = "RememberParam_CheckBox";
            this.RememberParam_CheckBox.Size = new System.Drawing.Size(78, 24);
            this.RememberParam_CheckBox.TabIndex = 1;
            this.RememberParam_CheckBox.Text = "参数记忆";
            this.RememberParam_CheckBox.UseVisualStyleBackColor = true;
            // 
            // AutoReadClipboard_CheckBox
            // 
            this.AutoReadClipboard_CheckBox.Location = new System.Drawing.Point(550, 121);
            this.AutoReadClipboard_CheckBox.Name = "AutoReadClipboard_CheckBox";
            this.AutoReadClipboard_CheckBox.Size = new System.Drawing.Size(112, 26);
            this.AutoReadClipboard_CheckBox.TabIndex = 2;
            this.AutoReadClipboard_CheckBox.Text = "自动读取剪贴板";
            this.AutoReadClipboard_CheckBox.UseVisualStyleBackColor = true;
            // 
            // AutoCheckUpdate_CheckBox
            // 
            this.AutoCheckUpdate_CheckBox.Location = new System.Drawing.Point(550, 188);
            this.AutoCheckUpdate_CheckBox.Name = "AutoCheckUpdate_CheckBox";
            this.AutoCheckUpdate_CheckBox.Size = new System.Drawing.Size(98, 17);
            this.AutoCheckUpdate_CheckBox.TabIndex = 3;
            this.AutoCheckUpdate_CheckBox.Text = "自动检查更新";
            this.AutoCheckUpdate_CheckBox.UseVisualStyleBackColor = true;
            // 
            // ShowRomaji_CheckBox
            // 
            this.ShowRomaji_CheckBox.Location = new System.Drawing.Point(18, 126);
            this.ShowRomaji_CheckBox.Name = "ShowRomaji_CheckBox";
            this.ShowRomaji_CheckBox.Size = new System.Drawing.Size(85, 17);
            this.ShowRomaji_CheckBox.TabIndex = 5;
            this.ShowRomaji_CheckBox.Text = "译文罗马音";
            this.ShowRomaji_CheckBox.UseVisualStyleBackColor = true;
            this.ShowRomaji_CheckBox.CheckedChanged += new System.EventHandler(this.ShowRomaji_CheckBox_CheckedChanged);
            // 
            // RomajiMode_ComboBox
            // 
            this.RomajiMode_ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.RomajiMode_ComboBox.FormattingEnabled = true;
            this.RomajiMode_ComboBox.Location = new System.Drawing.Point(325, 124);
            this.RomajiMode_ComboBox.Name = "RomajiMode_ComboBox";
            this.RomajiMode_ComboBox.Size = new System.Drawing.Size(100, 20);
            this.RomajiMode_ComboBox.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(230, 127);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(60, 17);
            this.label3.TabIndex = 8;
            this.label3.Text = "转换模式";
            // 
            // RomajiSystem_ComboBox
            // 
            this.RomajiSystem_ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.RomajiSystem_ComboBox.FormattingEnabled = true;
            this.RomajiSystem_ComboBox.Location = new System.Drawing.Point(115, 124);
            this.RomajiSystem_ComboBox.Name = "RomajiSystem_ComboBox";
            this.RomajiSystem_ComboBox.Size = new System.Drawing.Size(98, 20);
            this.RomajiSystem_ComboBox.TabIndex = 10;
            // 
            // label2
            // 
            this.label2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label2.Location = new System.Drawing.Point(14, 54);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(630, 2);
            this.label2.TabIndex = 11;
            this.label2.Text = "分割线";
            // 
            // label5
            // 
            this.label5.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label5.Location = new System.Drawing.Point(14, 172);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(630, 2);
            this.label5.TabIndex = 12;
            this.label5.Text = "分割线";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(230, 15);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(65, 12);
            this.label6.TabIndex = 13;
            this.label6.Text = "LRC 时间戳";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(445, 15);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(65, 12);
            this.label7.TabIndex = 14;
            this.label7.Text = "SRT 时间戳";
            // 
            // LrcTimestampFormat_TextBox
            // 
            this.LrcTimestampFormat_TextBox.Location = new System.Drawing.Point(325, 12);
            this.LrcTimestampFormat_TextBox.Name = "LrcTimestampFormat_TextBox";
            this.LrcTimestampFormat_TextBox.Size = new System.Drawing.Size(100, 21);
            this.LrcTimestampFormat_TextBox.TabIndex = 15;
            // 
            // SrtTimestampFormat_TextBox
            // 
            this.SrtTimestampFormat_TextBox.Location = new System.Drawing.Point(538, 12);
            this.SrtTimestampFormat_TextBox.Name = "SrtTimestampFormat_TextBox";
            this.SrtTimestampFormat_TextBox.Size = new System.Drawing.Size(100, 21);
            this.SrtTimestampFormat_TextBox.TabIndex = 16;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(18, 15);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(77, 12);
            this.label8.TabIndex = 17;
            this.label8.Text = "毫秒截位规则";
            // 
            // Dot_ComboBox
            // 
            this.Dot_ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Dot_ComboBox.FormattingEnabled = true;
            this.Dot_ComboBox.Location = new System.Drawing.Point(113, 12);
            this.Dot_ComboBox.Name = "Dot_ComboBox";
            this.Dot_ComboBox.Size = new System.Drawing.Size(100, 20);
            this.Dot_ComboBox.TabIndex = 18;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(18, 78);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(82, 17);
            this.label1.TabIndex = 19;
            this.label1.Text = "译文缺省规则";
            // 
            // TranLyricDefaultRule_ComboBox
            // 
            this.TranLyricDefaultRule_ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.TranLyricDefaultRule_ComboBox.FormattingEnabled = true;
            this.TranLyricDefaultRule_ComboBox.Location = new System.Drawing.Point(115, 75);
            this.TranLyricDefaultRule_ComboBox.Name = "TranLyricDefaultRule_ComboBox";
            this.TranLyricDefaultRule_ComboBox.Size = new System.Drawing.Size(98, 20);
            this.TranLyricDefaultRule_ComboBox.TabIndex = 20;
            // 
            // label9
            // 
            this.label9.Location = new System.Drawing.Point(228, 78);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(82, 17);
            this.label9.TabIndex = 21;
            this.label9.Text = "译文匹配精度";
            // 
            // TranslateMatchPrecisionDeviation_TextBox
            // 
            this.TranslateMatchPrecisionDeviation_TextBox.Location = new System.Drawing.Point(325, 75);
            this.TranslateMatchPrecisionDeviation_TextBox.Name = "TranslateMatchPrecisionDeviation_TextBox";
            this.TranslateMatchPrecisionDeviation_TextBox.Size = new System.Drawing.Size(73, 21);
            this.TranslateMatchPrecisionDeviation_TextBox.TabIndex = 22;
            this.TranslateMatchPrecisionDeviation_TextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.LrcMatchDigit_TextBox_KeyPress);
            // 
            // label10
            // 
            this.label10.Font = new System.Drawing.Font("宋体", 10F);
            this.label10.Location = new System.Drawing.Point(404, 78);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(24, 18);
            this.label10.TabIndex = 23;
            this.label10.Text = "MS";
            // 
            // IgnoreEmptyLyric_CheckBox
            // 
            this.IgnoreEmptyLyric_CheckBox.Location = new System.Drawing.Point(445, 78);
            this.IgnoreEmptyLyric_CheckBox.Name = "IgnoreEmptyLyric_CheckBox";
            this.IgnoreEmptyLyric_CheckBox.Size = new System.Drawing.Size(98, 17);
            this.IgnoreEmptyLyric_CheckBox.TabIndex = 24;
            this.IgnoreEmptyLyric_CheckBox.Text = "忽略空行歌词";
            this.IgnoreEmptyLyric_CheckBox.UseVisualStyleBackColor = true;
            // 
            // VerbatimLyric_CheckBox
            // 
            this.VerbatimLyric_CheckBox.Location = new System.Drawing.Point(552, 77);
            this.VerbatimLyric_CheckBox.Name = "VerbatimLyric_CheckBox";
            this.VerbatimLyric_CheckBox.Size = new System.Drawing.Size(110, 17);
            this.VerbatimLyric_CheckBox.TabIndex = 25;
            this.VerbatimLyric_CheckBox.Text = "QQ音乐逐字歌词";
            this.VerbatimLyric_CheckBox.UseVisualStyleBackColor = true;
            this.VerbatimLyric_CheckBox.CheckedChanged += new System.EventHandler(this.VerbatimLyric_CheckBox_CheckedChanged);
            // 
            // NetEase_Cookie_TextBox
            // 
            this.NetEase_Cookie_TextBox.Location = new System.Drawing.Point(111, 186);
            this.NetEase_Cookie_TextBox.Name = "NetEase_Cookie_TextBox";
            this.NetEase_Cookie_TextBox.Size = new System.Drawing.Size(412, 21);
            this.NetEase_Cookie_TextBox.TabIndex = 26;
            // 
            // label11
            // 
            this.label11.Location = new System.Drawing.Point(18, 189);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(80, 17);
            this.label11.TabIndex = 27;
            this.label11.Text = "网易云Cookie";
            // 
            // label12
            // 
            this.label12.Location = new System.Drawing.Point(15, 232);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(80, 17);
            this.label12.TabIndex = 28;
            this.label12.Text = "QQ音乐Cookie";
            // 
            // QQMusic_Cookie_TextBox
            // 
            this.QQMusic_Cookie_TextBox.Location = new System.Drawing.Point(111, 229);
            this.QQMusic_Cookie_TextBox.Name = "QQMusic_Cookie_TextBox";
            this.QQMusic_Cookie_TextBox.Size = new System.Drawing.Size(412, 21);
            this.QQMusic_Cookie_TextBox.TabIndex = 29;
            // 
            // SettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(674, 270);
            this.Controls.Add(this.QQMusic_Cookie_TextBox);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.NetEase_Cookie_TextBox);
            this.Controls.Add(this.VerbatimLyric_CheckBox);
            this.Controls.Add(this.IgnoreEmptyLyric_CheckBox);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.TranslateMatchPrecisionDeviation_TextBox);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.TranLyricDefaultRule_ComboBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.Dot_ComboBox);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.SrtTimestampFormat_TextBox);
            this.Controls.Add(this.LrcTimestampFormat_TextBox);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.RomajiSystem_ComboBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.RomajiMode_ComboBox);
            this.Controls.Add(this.ShowRomaji_CheckBox);
            this.Controls.Add(this.AutoCheckUpdate_CheckBox);
            this.Controls.Add(this.AutoReadClipboard_CheckBox);
            this.Controls.Add(this.RememberParam_CheckBox);
            this.Controls.Add(this.Save_Btn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "SettingForm";
            this.Text = "设置";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox QQMusic_Cookie_TextBox;

        private System.Windows.Forms.TextBox NetEase_Cookie_TextBox;
        private System.Windows.Forms.Label label11;

        private System.Windows.Forms.CheckBox VerbatimLyric_CheckBox;

        private System.Windows.Forms.CheckBox IgnoreEmptyLyric_CheckBox;

        private System.Windows.Forms.Label label10;

        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox TranslateMatchPrecisionDeviation_TextBox;

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox TranLyricDefaultRule_ComboBox;

        private System.Windows.Forms.ComboBox RomajiSystem_ComboBox;

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox RomajiMode_ComboBox;
        private System.Windows.Forms.Label label3;

        private System.Windows.Forms.CheckBox ShowRomaji_CheckBox;

        private System.Windows.Forms.CheckBox AutoCheckUpdate_CheckBox;

        private System.Windows.Forms.CheckBox AutoReadClipboard_CheckBox;

        private System.Windows.Forms.CheckBox RememberParam_CheckBox;

        private System.Windows.Forms.Button Save_Btn;

        #endregion

        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox LrcTimestampFormat_TextBox;
        private System.Windows.Forms.TextBox SrtTimestampFormat_TextBox;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox Dot_ComboBox;
    }
}