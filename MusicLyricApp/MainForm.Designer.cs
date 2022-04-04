namespace Application
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

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.textBox_lrc = new System.Windows.Forms.TextBox();
            this.textBox_singer = new System.Windows.Forms.TextBox();
            this.label_song = new System.Windows.Forms.Label();
            this.label_singer = new System.Windows.Forms.Label();
            this.searchBtn = new System.Windows.Forms.Button();
            this.saveBtn = new System.Windows.Forms.Button();
            this.comboBox_output_name = new System.Windows.Forms.ComboBox();
            this.comboBox_diglossia_lrc = new System.Windows.Forms.ComboBox();
            this.search_id_text = new System.Windows.Forms.TextBox();
            this.label_output = new System.Windows.Forms.Label();
            this.label_split = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.splitTextBox = new System.Windows.Forms.TextBox();
            this.textBox_song = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label_encode = new System.Windows.Forms.Label();
            this.comboBox_output_encode = new System.Windows.Forms.ComboBox();
            this.textBox_album = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.songUrlBtn = new System.Windows.Forms.Button();
            this.comboBox_search_type = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.homeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.wikiItem = new System.Windows.Forms.ToolStripMenuItem();
            this.issueMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.latestVersionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.comboBox_dot = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBox_output_format = new System.Windows.Forms.ComboBox();
            this.comboBox_search_source = new System.Windows.Forms.ComboBox();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBox_lrc
            // 
            this.textBox_lrc.Location = new System.Drawing.Point(8, 157);
            this.textBox_lrc.Multiline = true;
            this.textBox_lrc.Name = "textBox_lrc";
            this.textBox_lrc.ReadOnly = true;
            this.textBox_lrc.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox_lrc.Size = new System.Drawing.Size(472, 203);
            this.textBox_lrc.TabIndex = 3;
            this.textBox_lrc.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox_lrc_KeyDown);
            // 
            // textBox_singer
            // 
            this.textBox_singer.Location = new System.Drawing.Point(60, 130);
            this.textBox_singer.Name = "textBox_singer";
            this.textBox_singer.ReadOnly = true;
            this.textBox_singer.Size = new System.Drawing.Size(93, 21);
            this.textBox_singer.TabIndex = 8;
            // 
            // label_song
            // 
            this.label_song.AutoSize = true;
            this.label_song.Location = new System.Drawing.Point(168, 133);
            this.label_song.Name = "label_song";
            this.label_song.Size = new System.Drawing.Size(41, 12);
            this.label_song.TabIndex = 6;
            this.label_song.Text = "歌名：";
            // 
            // label_singer
            // 
            this.label_singer.AutoSize = true;
            this.label_singer.Location = new System.Drawing.Point(13, 133);
            this.label_singer.Name = "label_singer";
            this.label_singer.Size = new System.Drawing.Size(41, 12);
            this.label_singer.TabIndex = 5;
            this.label_singer.Text = "歌手：";
            // 
            // searchBtn
            // 
            this.searchBtn.Location = new System.Drawing.Point(387, 28);
            this.searchBtn.Name = "searchBtn";
            this.searchBtn.Size = new System.Drawing.Size(93, 46);
            this.searchBtn.TabIndex = 2;
            this.searchBtn.Text = "搜索 Enter";
            this.searchBtn.UseVisualStyleBackColor = true;
            this.searchBtn.Click += new System.EventHandler(this.searchBtn_Click);
            // 
            // saveBtn
            // 
            this.saveBtn.Location = new System.Drawing.Point(222, 377);
            this.saveBtn.Name = "saveBtn";
            this.saveBtn.Size = new System.Drawing.Size(258, 83);
            this.saveBtn.TabIndex = 4;
            this.saveBtn.Text = "保存";
            this.saveBtn.UseVisualStyleBackColor = true;
            this.saveBtn.Click += new System.EventHandler(this.saveBtn_Click);
            // 
            // comboBox_output_name
            // 
            this.comboBox_output_name.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_output_name.FormattingEnabled = true;
            this.comboBox_output_name.Items.AddRange(new object[] { "歌曲名 - 歌手", "歌手 - 歌曲名", "歌曲名" });
            this.comboBox_output_name.Location = new System.Drawing.Point(101, 374);
            this.comboBox_output_name.Name = "comboBox_output_name";
            this.comboBox_output_name.Size = new System.Drawing.Size(101, 20);
            this.comboBox_output_name.TabIndex = 9;
            this.comboBox_output_name.SelectedIndexChanged += new System.EventHandler(this.comboBox_output_name_SelectedIndexChanged);
            // 
            // comboBox_diglossia_lrc
            // 
            this.comboBox_diglossia_lrc.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_diglossia_lrc.FormattingEnabled = true;
            this.comboBox_diglossia_lrc.Items.AddRange(new object[] { "不显示译文", "仅显示译文", "优先原文", "优先译文", "合并显示，优先原文", "合并显示，优先译文" });
            this.comboBox_diglossia_lrc.Location = new System.Drawing.Point(89, 42);
            this.comboBox_diglossia_lrc.Name = "comboBox_diglossia_lrc";
            this.comboBox_diglossia_lrc.Size = new System.Drawing.Size(120, 20);
            this.comboBox_diglossia_lrc.TabIndex = 13;
            this.comboBox_diglossia_lrc.SelectedIndexChanged += new System.EventHandler(this.comboBox_diglossia_lrc_SelectedIndexChanged);
            // 
            // search_id_text
            // 
            this.search_id_text.Location = new System.Drawing.Point(89, 80);
            this.search_id_text.Name = "search_id_text";
            this.search_id_text.Size = new System.Drawing.Size(120, 21);
            this.search_id_text.TabIndex = 1;
            // 
            // label_output
            // 
            this.label_output.AutoSize = true;
            this.label_output.Location = new System.Drawing.Point(8, 377);
            this.label_output.Name = "label_output";
            this.label_output.Size = new System.Drawing.Size(65, 12);
            this.label_output.TabIndex = 11;
            this.label_output.Text = "输出文件名";
            // 
            // label_split
            // 
            this.label_split.AutoSize = true;
            this.label_split.Location = new System.Drawing.Point(222, 45);
            this.label_split.Name = "label_split";
            this.label_split.Size = new System.Drawing.Size(77, 12);
            this.label_split.TabIndex = 14;
            this.label_split.Text = "歌词合并符：";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(451, 319);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 14);
            this.label1.TabIndex = 0;
            // 
            // splitTextBox
            // 
            this.splitTextBox.BackColor = System.Drawing.SystemColors.Control;
            this.splitTextBox.Location = new System.Drawing.Point(299, 42);
            this.splitTextBox.Name = "splitTextBox";
            this.splitTextBox.ReadOnly = true;
            this.splitTextBox.Size = new System.Drawing.Size(70, 21);
            this.splitTextBox.TabIndex = 15;
            this.splitTextBox.TextChanged += new System.EventHandler(this.splitTextBox_TextChanged);
            // 
            // textBox_song
            // 
            this.textBox_song.Location = new System.Drawing.Point(215, 130);
            this.textBox_song.Name = "textBox_song";
            this.textBox_song.ReadOnly = true;
            this.textBox_song.Size = new System.Drawing.Size(93, 21);
            this.textBox_song.TabIndex = 7;
            // 
            // label4
            // 
            this.label4.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label4.Location = new System.Drawing.Point(13, 115);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(470, 2);
            this.label4.TabIndex = 16;
            this.label4.Text = "label4";
            // 
            // label_encode
            // 
            this.label_encode.AutoSize = true;
            this.label_encode.Location = new System.Drawing.Point(8, 443);
            this.label_encode.Name = "label_encode";
            this.label_encode.Size = new System.Drawing.Size(53, 12);
            this.label_encode.TabIndex = 17;
            this.label_encode.Text = "文件编码";
            // 
            // comboBox_output_encode
            // 
            this.comboBox_output_encode.AutoCompleteCustomSource.AddRange(new string[] { "UTF-8", "GB2312" });
            this.comboBox_output_encode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_output_encode.FormattingEnabled = true;
            this.comboBox_output_encode.Items.AddRange(new object[] { "UTF-8", "UTF-8-BOM", "GB2312", "GBK", "UNICODE" });
            this.comboBox_output_encode.Location = new System.Drawing.Point(100, 440);
            this.comboBox_output_encode.Name = "comboBox_output_encode";
            this.comboBox_output_encode.Size = new System.Drawing.Size(102, 20);
            this.comboBox_output_encode.TabIndex = 18;
            this.comboBox_output_encode.SelectedIndexChanged += new System.EventHandler(this.comboBox_output_encode_SelectedIndexChanged);
            // 
            // textBox_album
            // 
            this.textBox_album.Location = new System.Drawing.Point(387, 130);
            this.textBox_album.Name = "textBox_album";
            this.textBox_album.ReadOnly = true;
            this.textBox_album.Size = new System.Drawing.Size(93, 21);
            this.textBox_album.TabIndex = 20;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(328, 133);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(41, 12);
            this.label5.TabIndex = 19;
            this.label5.Text = "专辑：";
            // 
            // songUrlBtn
            // 
            this.songUrlBtn.Location = new System.Drawing.Point(387, 79);
            this.songUrlBtn.Name = "songUrlBtn";
            this.songUrlBtn.Size = new System.Drawing.Size(93, 22);
            this.songUrlBtn.TabIndex = 21;
            this.songUrlBtn.Text = "歌曲链接";
            this.songUrlBtn.UseVisualStyleBackColor = true;
            this.songUrlBtn.Click += new System.EventHandler(this.songUrlBtn_Click);
            // 
            // comboBox_search_type
            // 
            this.comboBox_search_type.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_search_type.FormattingEnabled = true;
            this.comboBox_search_type.Items.AddRange(new object[] { "ID", "专辑" });
            this.comboBox_search_type.Location = new System.Drawing.Point(11, 81);
            this.comboBox_search_type.Name = "comboBox_search_type";
            this.comboBox_search_type.Size = new System.Drawing.Size(62, 20);
            this.comboBox_search_type.TabIndex = 22;
            this.comboBox_search_type.SelectedIndexChanged += new System.EventHandler(this.comboBox_search_type_SelectedIndexChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(9, 60);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(0, 12);
            this.label6.TabIndex = 23;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.homeMenuItem, this.wikiItem, this.issueMenuItem, this.latestVersionMenuItem });
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(492, 25);
            this.menuStrip1.TabIndex = 25;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // homeMenuItem
            // 
            this.homeMenuItem.Name = "homeMenuItem";
            this.homeMenuItem.Size = new System.Drawing.Size(68, 21);
            this.homeMenuItem.Text = "项目主页";
            this.homeMenuItem.Click += new System.EventHandler(this.homeMenuItem_Click);
            // 
            // wikiItem
            // 
            this.wikiItem.Name = "wikiItem";
            this.wikiItem.Size = new System.Drawing.Size(68, 21);
            this.wikiItem.Text = "使用手册";
            this.wikiItem.Click += new System.EventHandler(this.wikiItem_Click);
            // 
            // issueMenuItem
            // 
            this.issueMenuItem.Name = "issueMenuItem";
            this.issueMenuItem.Size = new System.Drawing.Size(68, 21);
            this.issueMenuItem.Text = "问题反馈";
            this.issueMenuItem.Click += new System.EventHandler(this.issueMenuItem_Click);
            // 
            // latestVersionMenuItem
            // 
            this.latestVersionMenuItem.Name = "latestVersionMenuItem";
            this.latestVersionMenuItem.Size = new System.Drawing.Size(68, 21);
            this.latestVersionMenuItem.Text = "检查更新";
            this.latestVersionMenuItem.Click += new System.EventHandler(this.latestVersionMenuItem_Click);
            // 
            // comboBox_dot
            // 
            this.comboBox_dot.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_dot.FormattingEnabled = true;
            this.comboBox_dot.Items.AddRange(new object[] { "不开启", "截位", "四舍五入" });
            this.comboBox_dot.Location = new System.Drawing.Point(298, 80);
            this.comboBox_dot.Name = "comboBox_dot";
            this.comboBox_dot.Size = new System.Drawing.Size(71, 20);
            this.comboBox_dot.TabIndex = 26;
            this.comboBox_dot.SelectedIndexChanged += new System.EventHandler(this.comboBox_dot_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(222, 85);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 12);
            this.label2.TabIndex = 27;
            this.label2.Text = "强制两位：";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 409);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 12);
            this.label3.TabIndex = 28;
            this.label3.Text = "输出格式";
            // 
            // comboBox_output_format
            // 
            this.comboBox_output_format.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_output_format.FormattingEnabled = true;
            this.comboBox_output_format.Items.AddRange(new object[] { "LRC", "SRT" });
            this.comboBox_output_format.Location = new System.Drawing.Point(100, 409);
            this.comboBox_output_format.Name = "comboBox_output_format";
            this.comboBox_output_format.Size = new System.Drawing.Size(102, 20);
            this.comboBox_output_format.TabIndex = 29;
            this.comboBox_output_format.SelectedIndexChanged += new System.EventHandler(this.comboBox_output_format_SelectedIndexChanged);
            // 
            // comboBox_search_source
            // 
            this.comboBox_search_source.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_search_source.FormattingEnabled = true;
            this.comboBox_search_source.Items.AddRange(new object[] { "网易云", "QQ音乐" });
            this.comboBox_search_source.Location = new System.Drawing.Point(11, 42);
            this.comboBox_search_source.Name = "comboBox_search_source";
            this.comboBox_search_source.Size = new System.Drawing.Size(62, 20);
            this.comboBox_search_source.TabIndex = 30;
            this.comboBox_search_source.SelectedIndexChanged += new System.EventHandler(this.comboBox_search_source_SelectedIndexChanged);
            // 
            // MainForm
            // 
            this.AcceptButton = this.searchBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(492, 475);
            this.Controls.Add(this.comboBox_search_source);
            this.Controls.Add(this.comboBox_output_format);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBox_dot);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.comboBox_search_type);
            this.Controls.Add(this.songUrlBtn);
            this.Controls.Add(this.textBox_album);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.comboBox_output_encode);
            this.Controls.Add(this.label_encode);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBox_song);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.splitTextBox);
            this.Controls.Add(this.search_id_text);
            this.Controls.Add(this.searchBtn);
            this.Controls.Add(this.label_singer);
            this.Controls.Add(this.label_split);
            this.Controls.Add(this.label_song);
            this.Controls.Add(this.textBox_singer);
            this.Controls.Add(this.label_output);
            this.Controls.Add(this.textBox_lrc);
            this.Controls.Add(this.comboBox_diglossia_lrc);
            this.Controls.Add(this.saveBtn);
            this.Controls.Add(this.comboBox_output_name);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "云音乐歌词提取 " + MainForm.Version;
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboBox_output_format;

        #endregion
        private System.Windows.Forms.TextBox textBox_lrc;
        private System.Windows.Forms.TextBox textBox_singer;
        private System.Windows.Forms.Label label_song;
        private System.Windows.Forms.Label label_singer;
        private System.Windows.Forms.Button searchBtn;
        private System.Windows.Forms.Button saveBtn;
        private System.Windows.Forms.ComboBox comboBox_output_name;
        private System.Windows.Forms.ComboBox comboBox_diglossia_lrc;
        private System.Windows.Forms.TextBox search_id_text;
        private System.Windows.Forms.Label label_output;
        private System.Windows.Forms.Label label_split;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox splitTextBox;
        private System.Windows.Forms.TextBox textBox_song;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label_encode;
        private System.Windows.Forms.ComboBox comboBox_output_encode;
        private System.Windows.Forms.TextBox textBox_album;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button songUrlBtn;
        private System.Windows.Forms.ComboBox comboBox_search_type;
        private System.Windows.Forms.ComboBox comboBox_search_source;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem homeMenuItem;
        private System.Windows.Forms.ToolStripMenuItem latestVersionMenuItem;
        private System.Windows.Forms.ToolStripMenuItem issueMenuItem;
        private System.Windows.Forms.ToolStripMenuItem wikiItem;
        private System.Windows.Forms.ComboBox comboBox_dot;
        private System.Windows.Forms.Label label2;
    }
}

