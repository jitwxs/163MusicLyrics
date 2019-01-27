namespace WindowsFormsApp1
{
    partial class Form1
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
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.label2 = new System.Windows.Forms.Label();
            this.idsTextbox = new System.Windows.Forms.TextBox();
            this.batchBtn = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox_lrc = new System.Windows.Forms.TextBox();
            this.textBox_singer = new System.Windows.Forms.TextBox();
            this.label_song = new System.Windows.Forms.Label();
            this.label_singer = new System.Windows.Forms.Label();
            this.searchBtn = new System.Windows.Forms.Button();
            this.saveBtn = new System.Windows.Forms.Button();
            this.labe_translate = new System.Windows.Forms.Label();
            this.comboBox_output_name = new System.Windows.Forms.ComboBox();
            this.comboBox_diglossia_lrc = new System.Windows.Forms.ComboBox();
            this.dotCheckBox = new System.Windows.Forms.CheckBox();
            this.textBox_id = new System.Windows.Forms.TextBox();
            this.label_output = new System.Windows.Forms.Label();
            this.label_split = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.splitTextBox = new System.Windows.Forms.TextBox();
            this.textBox_song = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 405);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(47, 12);
            this.label2.TabIndex = 0;
            this.label2.Text = "ID列表:";
            // 
            // idsTextbox
            // 
            this.idsTextbox.Location = new System.Drawing.Point(65, 402);
            this.idsTextbox.Name = "idsTextbox";
            this.idsTextbox.Size = new System.Drawing.Size(143, 21);
            this.idsTextbox.TabIndex = 1;
            // 
            // batchBtn
            // 
            this.batchBtn.Location = new System.Drawing.Point(216, 393);
            this.batchBtn.Name = "batchBtn";
            this.batchBtn.Size = new System.Drawing.Size(98, 36);
            this.batchBtn.TabIndex = 2;
            this.batchBtn.Text = "批量保存";
            this.batchBtn.UseVisualStyleBackColor = true;
            this.batchBtn.Click += new System.EventHandler(this.batchBtn_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(11, 432);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(281, 12);
            this.label3.TabIndex = 3;
            this.label3.Text = "【注：列表使用英文逗号分隔，配置使用以上配置】";
            // 
            // textBox_lrc
            // 
            this.textBox_lrc.Location = new System.Drawing.Point(14, 71);
            this.textBox_lrc.Multiline = true;
            this.textBox_lrc.Name = "textBox_lrc";
            this.textBox_lrc.ReadOnly = true;
            this.textBox_lrc.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox_lrc.Size = new System.Drawing.Size(300, 180);
            this.textBox_lrc.TabIndex = 3;
            // 
            // textBox_singer
            // 
            this.textBox_singer.Location = new System.Drawing.Point(214, 44);
            this.textBox_singer.Name = "textBox_singer";
            this.textBox_singer.ReadOnly = true;
            this.textBox_singer.Size = new System.Drawing.Size(100, 21);
            this.textBox_singer.TabIndex = 8;
            // 
            // label_song
            // 
            this.label_song.AutoSize = true;
            this.label_song.Location = new System.Drawing.Point(15, 47);
            this.label_song.Name = "label_song";
            this.label_song.Size = new System.Drawing.Size(41, 12);
            this.label_song.TabIndex = 6;
            this.label_song.Text = "歌名：";
            // 
            // label_singer
            // 
            this.label_singer.AutoSize = true;
            this.label_singer.Location = new System.Drawing.Point(167, 47);
            this.label_singer.Name = "label_singer";
            this.label_singer.Size = new System.Drawing.Size(41, 12);
            this.label_singer.TabIndex = 5;
            this.label_singer.Text = "歌手：";
            // 
            // searchBtn
            // 
            this.searchBtn.Location = new System.Drawing.Point(214, 5);
            this.searchBtn.Name = "searchBtn";
            this.searchBtn.Size = new System.Drawing.Size(100, 33);
            this.searchBtn.TabIndex = 2;
            this.searchBtn.Text = "搜索";
            this.searchBtn.UseVisualStyleBackColor = true;
            this.searchBtn.Click += new System.EventHandler(this.searchBtn_Click);
            // 
            // saveBtn
            // 
            this.saveBtn.Location = new System.Drawing.Point(213, 329);
            this.saveBtn.Name = "saveBtn";
            this.saveBtn.Size = new System.Drawing.Size(100, 36);
            this.saveBtn.TabIndex = 4;
            this.saveBtn.Text = "保存";
            this.saveBtn.UseVisualStyleBackColor = true;
            this.saveBtn.Click += new System.EventHandler(this.saveBtn_Click);
            // 
            // labe_translate
            // 
            this.labe_translate.AutoSize = true;
            this.labe_translate.Location = new System.Drawing.Point(14, 278);
            this.labe_translate.Name = "labe_translate";
            this.labe_translate.Size = new System.Drawing.Size(65, 12);
            this.labe_translate.TabIndex = 12;
            this.labe_translate.Text = "双语歌词：";
            // 
            // comboBox_output_name
            // 
            this.comboBox_output_name.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_output_name.FormattingEnabled = true;
            this.comboBox_output_name.Items.AddRange(new object[] {
            "歌曲名 - 歌手",
            "歌手 - 歌曲名",
            "歌曲名"});
            this.comboBox_output_name.Location = new System.Drawing.Point(106, 338);
            this.comboBox_output_name.Name = "comboBox_output_name";
            this.comboBox_output_name.Size = new System.Drawing.Size(101, 20);
            this.comboBox_output_name.TabIndex = 9;
            this.comboBox_output_name.SelectedIndexChanged += new System.EventHandler(this.comboBox_output_name_SelectedIndexChanged);
            // 
            // comboBox_diglossia_lrc
            // 
            this.comboBox_diglossia_lrc.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_diglossia_lrc.FormattingEnabled = true;
            this.comboBox_diglossia_lrc.Items.AddRange(new object[] {
            "不显示译文",
            "仅显示译文",
            "优先原文",
            "优先译文",
            "合并显示"});
            this.comboBox_diglossia_lrc.Location = new System.Drawing.Point(106, 270);
            this.comboBox_diglossia_lrc.Name = "comboBox_diglossia_lrc";
            this.comboBox_diglossia_lrc.Size = new System.Drawing.Size(207, 20);
            this.comboBox_diglossia_lrc.TabIndex = 13;
            this.comboBox_diglossia_lrc.SelectedIndexChanged += new System.EventHandler(this.comboBox_diglossia_lrc_SelectedIndexChanged);
            // 
            // dotCheckBox
            // 
            this.dotCheckBox.AutoSize = true;
            this.dotCheckBox.Location = new System.Drawing.Point(13, 306);
            this.dotCheckBox.Name = "dotCheckBox";
            this.dotCheckBox.Size = new System.Drawing.Size(72, 16);
            this.dotCheckBox.TabIndex = 10;
            this.dotCheckBox.Text = "强制两位";
            this.dotCheckBox.UseVisualStyleBackColor = true;
            // 
            // textBox_id
            // 
            this.textBox_id.Location = new System.Drawing.Point(63, 12);
            this.textBox_id.Name = "textBox_id";
            this.textBox_id.Size = new System.Drawing.Size(133, 21);
            this.textBox_id.TabIndex = 1;
            // 
            // label_output
            // 
            this.label_output.AutoSize = true;
            this.label_output.Location = new System.Drawing.Point(11, 341);
            this.label_output.Name = "label_output";
            this.label_output.Size = new System.Drawing.Size(77, 12);
            this.label_output.TabIndex = 11;
            this.label_output.Text = "输出文件名：";
            // 
            // label_split
            // 
            this.label_split.AutoSize = true;
            this.label_split.Location = new System.Drawing.Point(104, 307);
            this.label_split.Name = "label_split";
            this.label_split.Size = new System.Drawing.Size(101, 12);
            this.label_split.TabIndex = 14;
            this.label_split.Text = "合并显示分隔符：";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(14, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(28, 14);
            this.label1.TabIndex = 0;
            this.label1.Text = "ID:";
            // 
            // splitTextBox
            // 
            this.splitTextBox.Location = new System.Drawing.Point(213, 305);
            this.splitTextBox.Name = "splitTextBox";
            this.splitTextBox.ReadOnly = true;
            this.splitTextBox.Size = new System.Drawing.Size(100, 21);
            this.splitTextBox.TabIndex = 15;
            // 
            // textBox_song
            // 
            this.textBox_song.Location = new System.Drawing.Point(62, 44);
            this.textBox_song.Name = "textBox_song";
            this.textBox_song.ReadOnly = true;
            this.textBox_song.Size = new System.Drawing.Size(93, 21);
            this.textBox_song.TabIndex = 7;
            // 
            // label4
            // 
            this.label4.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label4.Location = new System.Drawing.Point(12, 378);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(300, 2);
            this.label4.TabIndex = 16;
            this.label4.Text = "label4";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(325, 455);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBox_song);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.splitTextBox);
            this.Controls.Add(this.textBox_id);
            this.Controls.Add(this.searchBtn);
            this.Controls.Add(this.batchBtn);
            this.Controls.Add(this.label_singer);
            this.Controls.Add(this.label_split);
            this.Controls.Add(this.label_song);
            this.Controls.Add(this.idsTextbox);
            this.Controls.Add(this.textBox_singer);
            this.Controls.Add(this.label_output);
            this.Controls.Add(this.textBox_lrc);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBox_diglossia_lrc);
            this.Controls.Add(this.dotCheckBox);
            this.Controls.Add(this.saveBtn);
            this.Controls.Add(this.labe_translate);
            this.Controls.Add(this.comboBox_output_name);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "Form1";
            this.Text = "网易云歌词提取";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox idsTextbox;
        private System.Windows.Forms.Button batchBtn;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox_lrc;
        private System.Windows.Forms.TextBox textBox_singer;
        private System.Windows.Forms.Label label_song;
        private System.Windows.Forms.Label label_singer;
        private System.Windows.Forms.Button searchBtn;
        private System.Windows.Forms.Button saveBtn;
        private System.Windows.Forms.Label labe_translate;
        private System.Windows.Forms.ComboBox comboBox_output_name;
        private System.Windows.Forms.ComboBox comboBox_diglossia_lrc;
        private System.Windows.Forms.CheckBox dotCheckBox;
        private System.Windows.Forms.TextBox textBox_id;
        private System.Windows.Forms.Label label_output;
        private System.Windows.Forms.Label label_split;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox splitTextBox;
        private System.Windows.Forms.TextBox textBox_song;
        private System.Windows.Forms.Label label4;
    }
}

