using MusicLyricApp.Bean;
using MusicLyricApp.Utils;

namespace MusicLyricApp
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
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
            this.LrcType_ComboBox.Items.AddRange(GlobalUtils.GetEnumDescArray<ShowLrcTypeEnum>());
            this.OutputEncoding_ComboBox.Items.AddRange(GlobalUtils.GetEnumDescArray<OutputEncodingEnum>());
            this.SearchType_ComboBox.Items.AddRange(GlobalUtils.GetEnumDescArray<SearchTypeEnum>());
            this.OutputFormat_CombBox.Items.AddRange(new object[] { "LRC", "SRT" });
            this.SearchSource_ComboBox.Items.AddRange(GlobalUtils.GetEnumDescArray<SearchSourceEnum>());

            _scalingFormConfig = new ScalingFormConfig(this);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.Console_TextBox = new System.Windows.Forms.TextBox();
            this.Singer_TextBox = new System.Windows.Forms.TextBox();
            this.SongName_Label = new System.Windows.Forms.Label();
            this.Singer_Label = new System.Windows.Forms.Label();
            this.Search_Btn = new System.Windows.Forms.Button();
            this.Save_Btn = new System.Windows.Forms.Button();
            this.LrcType_ComboBox = new System.Windows.Forms.ComboBox();
            this.Search_Text = new System.Windows.Forms.TextBox();
            this.LrcMergeSeparator_Label = new System.Windows.Forms.Label();
            this.LrcMergeSeparator_TextBox = new System.Windows.Forms.TextBox();
            this.SongName_TextBox = new System.Windows.Forms.TextBox();
            this.OutputEncoding_Label = new System.Windows.Forms.Label();
            this.OutputEncoding_ComboBox = new System.Windows.Forms.ComboBox();
            this.Album_TextBox = new System.Windows.Forms.TextBox();
            this.Album_Label = new System.Windows.Forms.Label();
            this.SongLink_Btn = new System.Windows.Forms.Button();
            this.SearchType_ComboBox = new System.Windows.Forms.ComboBox();
            this.Top_MenuStrip = new System.Windows.Forms.MenuStrip();
            this.Home_MItem = new System.Windows.Forms.ToolStripMenuItem();
            this.Wiki_MItem = new System.Windows.Forms.ToolStripMenuItem();
            this.Issue_MItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CheckVersion_MItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ShortCut_MItem = new System.Windows.Forms.ToolStripMenuItem();
            this.Setting_MItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OutputFormat_Label = new System.Windows.Forms.Label();
            this.OutputFormat_CombBox = new System.Windows.Forms.ComboBox();
            this.SearchSource_ComboBox = new System.Windows.Forms.ComboBox();
            this.SongPic_Btn = new System.Windows.Forms.Button();
            this.Blur_Search_Btn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.Top_MenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // Console_TextBox
            // 
            this.Console_TextBox.Location = new System.Drawing.Point(8, 157);
            this.Console_TextBox.Multiline = true;
            this.Console_TextBox.Name = "Console_TextBox";
            this.Console_TextBox.ReadOnly = true;
            this.Console_TextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.Console_TextBox.Size = new System.Drawing.Size(472, 203);
            this.Console_TextBox.TabIndex = 3;
            // 
            // Singer_TextBox
            // 
            this.Singer_TextBox.Location = new System.Drawing.Point(60, 130);
            this.Singer_TextBox.Name = "Singer_TextBox";
            this.Singer_TextBox.ReadOnly = true;
            this.Singer_TextBox.Size = new System.Drawing.Size(93, 21);
            this.Singer_TextBox.TabIndex = 8;
            // 
            // SongName_Label
            // 
            this.SongName_Label.AutoSize = true;
            this.SongName_Label.Location = new System.Drawing.Point(168, 133);
            this.SongName_Label.Name = "SongName_Label";
            this.SongName_Label.Size = new System.Drawing.Size(41, 12);
            this.SongName_Label.TabIndex = 6;
            this.SongName_Label.Text = "歌名：";
            // 
            // Singer_Label
            // 
            this.Singer_Label.AutoSize = true;
            this.Singer_Label.Location = new System.Drawing.Point(13, 133);
            this.Singer_Label.Name = "Singer_Label";
            this.Singer_Label.Size = new System.Drawing.Size(41, 12);
            this.Singer_Label.TabIndex = 5;
            this.Singer_Label.Text = "歌手：";
            // 
            // Search_Btn
            // 
            this.Search_Btn.Location = new System.Drawing.Point(390, 81);
            this.Search_Btn.Name = "Search_Btn";
            this.Search_Btn.Size = new System.Drawing.Size(90, 30);
            this.Search_Btn.TabIndex = 2;
            this.Search_Btn.Text = "精确搜索";
            this.Search_Btn.UseVisualStyleBackColor = true;
            this.Search_Btn.Click += new System.EventHandler(this.Search_Btn_Click);
            // 
            // Save_Btn
            // 
            this.Save_Btn.Location = new System.Drawing.Point(222, 377);
            this.Save_Btn.Name = "Save_Btn";
            this.Save_Btn.Size = new System.Drawing.Size(258, 51);
            this.Save_Btn.TabIndex = 4;
            this.Save_Btn.Text = "保存";
            this.Save_Btn.UseVisualStyleBackColor = true;
            this.Save_Btn.Click += new System.EventHandler(this.Save_Btn_Click);
            // 
            // LrcType_ComboBox
            // 
            this.LrcType_ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.LrcType_ComboBox.FormattingEnabled = true;
            this.LrcType_ComboBox.Location = new System.Drawing.Point(147, 42);
            this.LrcType_ComboBox.Name = "LrcType_ComboBox";
            this.LrcType_ComboBox.Size = new System.Drawing.Size(62, 20);
            this.LrcType_ComboBox.TabIndex = 13;
            this.LrcType_ComboBox.SelectedIndexChanged += new System.EventHandler(this.LrcType_ComboBox_SelectedIndexChanged);
            // 
            // Search_Text
            // 
            this.Search_Text.Location = new System.Drawing.Point(89, 85);
            this.Search_Text.Name = "Search_Text";
            this.Search_Text.Size = new System.Drawing.Size(196, 21);
            this.Search_Text.TabIndex = 1;
            this.Search_Text.MouseEnter += new System.EventHandler(this.Search_Text_MouseEnter);
            // 
            // LrcMergeSeparator_Label
            // 
            this.LrcMergeSeparator_Label.AutoSize = true;
            this.LrcMergeSeparator_Label.Location = new System.Drawing.Point(222, 45);
            this.LrcMergeSeparator_Label.Name = "LrcMergeSeparator_Label";
            this.LrcMergeSeparator_Label.Size = new System.Drawing.Size(65, 12);
            this.LrcMergeSeparator_Label.TabIndex = 14;
            this.LrcMergeSeparator_Label.Text = "歌词合并符";
            // 
            // LrcMergeSeparator_TextBox
            // 
            this.LrcMergeSeparator_TextBox.BackColor = System.Drawing.SystemColors.Control;
            this.LrcMergeSeparator_TextBox.Location = new System.Drawing.Point(299, 42);
            this.LrcMergeSeparator_TextBox.Name = "LrcMergeSeparator_TextBox";
            this.LrcMergeSeparator_TextBox.ReadOnly = true;
            this.LrcMergeSeparator_TextBox.Size = new System.Drawing.Size(70, 21);
            this.LrcMergeSeparator_TextBox.TabIndex = 15;
            this.LrcMergeSeparator_TextBox.TextChanged += new System.EventHandler(this.LrcMergeSeparator_TextBox_TextChanged);
            // 
            // SongName_TextBox
            // 
            this.SongName_TextBox.Location = new System.Drawing.Point(215, 130);
            this.SongName_TextBox.Name = "SongName_TextBox";
            this.SongName_TextBox.ReadOnly = true;
            this.SongName_TextBox.Size = new System.Drawing.Size(93, 21);
            this.SongName_TextBox.TabIndex = 7;
            // 
            // OutputEncoding_Label
            // 
            this.OutputEncoding_Label.AutoSize = true;
            this.OutputEncoding_Label.Location = new System.Drawing.Point(13, 411);
            this.OutputEncoding_Label.Name = "OutputEncoding_Label";
            this.OutputEncoding_Label.Size = new System.Drawing.Size(53, 12);
            this.OutputEncoding_Label.TabIndex = 17;
            this.OutputEncoding_Label.Text = "文件编码";
            // 
            // OutputEncoding_ComboBox
            // 
            this.OutputEncoding_ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.OutputEncoding_ComboBox.FormattingEnabled = true;
            this.OutputEncoding_ComboBox.Location = new System.Drawing.Point(98, 408);
            this.OutputEncoding_ComboBox.Name = "OutputEncoding_ComboBox";
            this.OutputEncoding_ComboBox.Size = new System.Drawing.Size(102, 20);
            this.OutputEncoding_ComboBox.TabIndex = 18;
            this.OutputEncoding_ComboBox.SelectedIndexChanged += new System.EventHandler(this.Config_ComboBox_SelectedIndexChanged);
            // 
            // Album_TextBox
            // 
            this.Album_TextBox.Location = new System.Drawing.Point(387, 130);
            this.Album_TextBox.Name = "Album_TextBox";
            this.Album_TextBox.ReadOnly = true;
            this.Album_TextBox.Size = new System.Drawing.Size(93, 21);
            this.Album_TextBox.TabIndex = 20;
            // 
            // Album_Label
            // 
            this.Album_Label.AutoSize = true;
            this.Album_Label.Location = new System.Drawing.Point(328, 133);
            this.Album_Label.Name = "Album_Label";
            this.Album_Label.Size = new System.Drawing.Size(41, 12);
            this.Album_Label.TabIndex = 19;
            this.Album_Label.Text = "专辑：";
            // 
            // SongLink_Btn
            // 
            this.SongLink_Btn.Location = new System.Drawing.Point(299, 85);
            this.SongLink_Btn.Name = "SongLink_Btn";
            this.SongLink_Btn.Size = new System.Drawing.Size(38, 22);
            this.SongLink_Btn.TabIndex = 21;
            this.SongLink_Btn.Text = "直链";
            this.SongLink_Btn.UseVisualStyleBackColor = true;
            this.SongLink_Btn.Click += new System.EventHandler(this.SongLink_Btn_Click);
            // 
            // SearchType_ComboBox
            // 
            this.SearchType_ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SearchType_ComboBox.FormattingEnabled = true;
            this.SearchType_ComboBox.Location = new System.Drawing.Point(11, 85);
            this.SearchType_ComboBox.Name = "SearchType_ComboBox";
            this.SearchType_ComboBox.Size = new System.Drawing.Size(62, 20);
            this.SearchType_ComboBox.TabIndex = 22;
            this.SearchType_ComboBox.SelectedIndexChanged += new System.EventHandler(this.SearchType_ComboBox_SelectedIndexChanged);
            // 
            // Top_MenuStrip
            // 
            this.Top_MenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.Home_MItem, this.Wiki_MItem, this.Issue_MItem, this.CheckVersion_MItem, this.ShortCut_MItem, this.Setting_MItem });
            this.Top_MenuStrip.Location = new System.Drawing.Point(0, 0);
            this.Top_MenuStrip.Name = "Top_MenuStrip";
            this.Top_MenuStrip.Size = new System.Drawing.Size(492, 25);
            this.Top_MenuStrip.TabIndex = 25;
            this.Top_MenuStrip.Text = "menuStrip1";
            // 
            // Home_MItem
            // 
            this.Home_MItem.Name = "Home_MItem";
            this.Home_MItem.Size = new System.Drawing.Size(68, 21);
            this.Home_MItem.Text = "项目主页";
            this.Home_MItem.Click += new System.EventHandler(this.Top_MItem_Click);
            // 
            // Wiki_MItem
            // 
            this.Wiki_MItem.Name = "Wiki_MItem";
            this.Wiki_MItem.Size = new System.Drawing.Size(68, 21);
            this.Wiki_MItem.Text = "使用手册";
            this.Wiki_MItem.Click += new System.EventHandler(this.Top_MItem_Click);
            // 
            // Issue_MItem
            // 
            this.Issue_MItem.Name = "Issue_MItem";
            this.Issue_MItem.Size = new System.Drawing.Size(68, 21);
            this.Issue_MItem.Text = "问题反馈";
            this.Issue_MItem.Click += new System.EventHandler(this.Top_MItem_Click);
            // 
            // CheckVersion_MItem
            // 
            this.CheckVersion_MItem.Name = "CheckVersion_MItem";
            this.CheckVersion_MItem.Size = new System.Drawing.Size(68, 21);
            this.CheckVersion_MItem.Text = "检查更新";
            this.CheckVersion_MItem.Click += new System.EventHandler(this.Top_MItem_Click);
            // 
            // ShortCut_MItem
            // 
            this.ShortCut_MItem.Name = "ShortCut_MItem";
            this.ShortCut_MItem.Size = new System.Drawing.Size(56, 21);
            this.ShortCut_MItem.Text = "快捷键";
            this.ShortCut_MItem.Click += new System.EventHandler(this.Top_MItem_Click);
            // 
            // Setting_MItem
            // 
            this.Setting_MItem.Name = "Setting_MItem";
            this.Setting_MItem.Size = new System.Drawing.Size(68, 21);
            this.Setting_MItem.Text = "更多设置";
            this.Setting_MItem.Click += new System.EventHandler(this.Top_MItem_Click);
            // 
            // OutputFormat_Label
            // 
            this.OutputFormat_Label.AutoSize = true;
            this.OutputFormat_Label.Location = new System.Drawing.Point(13, 377);
            this.OutputFormat_Label.Name = "OutputFormat_Label";
            this.OutputFormat_Label.Size = new System.Drawing.Size(53, 12);
            this.OutputFormat_Label.TabIndex = 28;
            this.OutputFormat_Label.Text = "输出格式";
            // 
            // OutputFormat_CombBox
            // 
            this.OutputFormat_CombBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.OutputFormat_CombBox.FormattingEnabled = true;
            this.OutputFormat_CombBox.Location = new System.Drawing.Point(98, 377);
            this.OutputFormat_CombBox.Name = "OutputFormat_CombBox";
            this.OutputFormat_CombBox.Size = new System.Drawing.Size(102, 20);
            this.OutputFormat_CombBox.TabIndex = 29;
            this.OutputFormat_CombBox.SelectedIndexChanged += new System.EventHandler(this.Config_ComboBox_SelectedIndexChanged);
            // 
            // SearchSource_ComboBox
            // 
            this.SearchSource_ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SearchSource_ComboBox.FormattingEnabled = true;
            this.SearchSource_ComboBox.Location = new System.Drawing.Point(11, 42);
            this.SearchSource_ComboBox.Name = "SearchSource_ComboBox";
            this.SearchSource_ComboBox.Size = new System.Drawing.Size(62, 20);
            this.SearchSource_ComboBox.TabIndex = 30;
            this.SearchSource_ComboBox.SelectedIndexChanged += new System.EventHandler(this.SearchSource_ComboBox_SelectedIndexChanged);
            // 
            // SongPic_Btn
            // 
            this.SongPic_Btn.Location = new System.Drawing.Point(345, 85);
            this.SongPic_Btn.Name = "SongPic_Btn";
            this.SongPic_Btn.Size = new System.Drawing.Size(39, 23);
            this.SongPic_Btn.TabIndex = 31;
            this.SongPic_Btn.Text = "封面";
            this.SongPic_Btn.UseVisualStyleBackColor = true;
            this.SongPic_Btn.Click += new System.EventHandler(this.SongPic_Btn_Click);
            // 
            // Blur_Search_Btn
            // 
            this.Blur_Search_Btn.Location = new System.Drawing.Point(390, 40);
            this.Blur_Search_Btn.Name = "Blur_Search_Btn";
            this.Blur_Search_Btn.Size = new System.Drawing.Size(90, 30);
            this.Blur_Search_Btn.TabIndex = 32;
            this.Blur_Search_Btn.Text = "模糊搜索";
            this.Blur_Search_Btn.UseVisualStyleBackColor = true;
            this.Blur_Search_Btn.Click += new System.EventHandler(this.Search_Btn_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(89, 45);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 33;
            this.label1.Text = "歌词格式";
            // 
            // MainForm
            // 
            this.AcceptButton = this.Search_Btn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(492, 443);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.Blur_Search_Btn);
            this.Controls.Add(this.SongPic_Btn);
            this.Controls.Add(this.SearchSource_ComboBox);
            this.Controls.Add(this.OutputFormat_CombBox);
            this.Controls.Add(this.OutputFormat_Label);
            this.Controls.Add(this.SearchType_ComboBox);
            this.Controls.Add(this.SongLink_Btn);
            this.Controls.Add(this.Album_TextBox);
            this.Controls.Add(this.Album_Label);
            this.Controls.Add(this.OutputEncoding_ComboBox);
            this.Controls.Add(this.OutputEncoding_Label);
            this.Controls.Add(this.SongName_TextBox);
            this.Controls.Add(this.LrcMergeSeparator_TextBox);
            this.Controls.Add(this.Search_Text);
            this.Controls.Add(this.Search_Btn);
            this.Controls.Add(this.Singer_Label);
            this.Controls.Add(this.LrcMergeSeparator_Label);
            this.Controls.Add(this.SongName_Label);
            this.Controls.Add(this.Singer_TextBox);
            this.Controls.Add(this.Console_TextBox);
            this.Controls.Add(this.LrcType_ComboBox);
            this.Controls.Add(this.Save_Btn);
            this.Controls.Add(this.Top_MenuStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MainMenuStrip = this.Top_MenuStrip;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "云音乐歌词提取 " + Constants.Version;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.Top_MenuStrip.ResumeLayout(false);
            this.Top_MenuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label label1;

        private System.Windows.Forms.ToolStripMenuItem ShortCut_MItem;

        private System.Windows.Forms.Button Blur_Search_Btn;

        private System.Windows.Forms.Button SongPic_Btn;

        private System.Windows.Forms.ToolStripMenuItem Setting_MItem;

        private System.Windows.Forms.Label OutputFormat_Label;
        private System.Windows.Forms.ComboBox OutputFormat_CombBox;

        #endregion
        private System.Windows.Forms.TextBox Console_TextBox;
        private System.Windows.Forms.TextBox Singer_TextBox;
        private System.Windows.Forms.Label SongName_Label;
        private System.Windows.Forms.Label Singer_Label;
        private System.Windows.Forms.Button Search_Btn;
        private System.Windows.Forms.Button Save_Btn;
        private System.Windows.Forms.ComboBox LrcType_ComboBox;
        public System.Windows.Forms.TextBox Search_Text;
        private System.Windows.Forms.Label LrcMergeSeparator_Label;
        private System.Windows.Forms.TextBox LrcMergeSeparator_TextBox;
        private System.Windows.Forms.TextBox SongName_TextBox;
        private System.Windows.Forms.Label OutputEncoding_Label;
        private System.Windows.Forms.ComboBox OutputEncoding_ComboBox;
        private System.Windows.Forms.TextBox Album_TextBox;
        private System.Windows.Forms.Label Album_Label;
        private System.Windows.Forms.Button SongLink_Btn;
        private System.Windows.Forms.ComboBox SearchType_ComboBox;
        private System.Windows.Forms.ComboBox SearchSource_ComboBox;
        private System.Windows.Forms.MenuStrip Top_MenuStrip;
        private System.Windows.Forms.ToolStripMenuItem Home_MItem;
        private System.Windows.Forms.ToolStripMenuItem CheckVersion_MItem;
        private System.Windows.Forms.ToolStripMenuItem Issue_MItem;
        private System.Windows.Forms.ToolStripMenuItem Wiki_MItem;
    }
}

