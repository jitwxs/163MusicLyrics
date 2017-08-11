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
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_id = new System.Windows.Forms.TextBox();
            this.confirmBtn = new System.Windows.Forms.Button();
            this.textBox_lrc = new System.Windows.Forms.TextBox();
            this.saveBtn = new System.Windows.Forms.Button();
            this.label_singer = new System.Windows.Forms.Label();
            this.label_song = new System.Windows.Forms.Label();
            this.textBox_song = new System.Windows.Forms.TextBox();
            this.textBox_singer = new System.Windows.Forms.TextBox();
            this.comboBox = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(12, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(28, 14);
            this.label1.TabIndex = 0;
            this.label1.Text = "ID:";
            // 
            // textBox_id
            // 
            this.textBox_id.Location = new System.Drawing.Point(61, 21);
            this.textBox_id.Name = "textBox_id";
            this.textBox_id.Size = new System.Drawing.Size(122, 21);
            this.textBox_id.TabIndex = 1;
            // 
            // confirmBtn
            // 
            this.confirmBtn.Location = new System.Drawing.Point(218, 14);
            this.confirmBtn.Name = "confirmBtn";
            this.confirmBtn.Size = new System.Drawing.Size(76, 33);
            this.confirmBtn.TabIndex = 2;
            this.confirmBtn.Text = "搜索";
            this.confirmBtn.UseVisualStyleBackColor = true;
            this.confirmBtn.Click += new System.EventHandler(this.confirmBtn_Click);
            // 
            // textBox_lrc
            // 
            this.textBox_lrc.Location = new System.Drawing.Point(15, 87);
            this.textBox_lrc.Multiline = true;
            this.textBox_lrc.Name = "textBox_lrc";
            this.textBox_lrc.ReadOnly = true;
            this.textBox_lrc.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox_lrc.Size = new System.Drawing.Size(291, 249);
            this.textBox_lrc.TabIndex = 3;
            // 
            // saveBtn
            // 
            this.saveBtn.Location = new System.Drawing.Point(231, 342);
            this.saveBtn.Name = "saveBtn";
            this.saveBtn.Size = new System.Drawing.Size(75, 23);
            this.saveBtn.TabIndex = 4;
            this.saveBtn.Text = "保存";
            this.saveBtn.UseVisualStyleBackColor = true;
            this.saveBtn.Click += new System.EventHandler(this.saveBtn_Click);
            // 
            // label_singer
            // 
            this.label_singer.AutoSize = true;
            this.label_singer.Location = new System.Drawing.Point(160, 61);
            this.label_singer.Name = "label_singer";
            this.label_singer.Size = new System.Drawing.Size(41, 12);
            this.label_singer.TabIndex = 5;
            this.label_singer.Text = "歌手：";
            // 
            // label_song
            // 
            this.label_song.AutoSize = true;
            this.label_song.Location = new System.Drawing.Point(13, 61);
            this.label_song.Name = "label_song";
            this.label_song.Size = new System.Drawing.Size(41, 12);
            this.label_song.TabIndex = 6;
            this.label_song.Text = "歌名：";
            // 
            // textBox_song
            // 
            this.textBox_song.Location = new System.Drawing.Point(61, 58);
            this.textBox_song.Name = "textBox_song";
            this.textBox_song.ReadOnly = true;
            this.textBox_song.Size = new System.Drawing.Size(93, 21);
            this.textBox_song.TabIndex = 7;
            // 
            // textBox_singer
            // 
            this.textBox_singer.Location = new System.Drawing.Point(207, 58);
            this.textBox_singer.Name = "textBox_singer";
            this.textBox_singer.ReadOnly = true;
            this.textBox_singer.Size = new System.Drawing.Size(100, 21);
            this.textBox_singer.TabIndex = 8;
            // 
            // comboBox
            // 
            this.comboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox.FormattingEnabled = true;
            this.comboBox.Items.AddRange(new object[] {
            "歌曲名 - 歌手",
            "歌手 - 歌曲名",
            "歌曲名"});
            this.comboBox.Location = new System.Drawing.Point(15, 344);
            this.comboBox.Name = "comboBox";
            this.comboBox.Size = new System.Drawing.Size(121, 20);
            this.comboBox.TabIndex = 9;
            this.comboBox.SelectedIndexChanged += new System.EventHandler(this.comboBox_SelectedIndexChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(318, 377);
            this.Controls.Add(this.comboBox);
            this.Controls.Add(this.textBox_singer);
            this.Controls.Add(this.textBox_song);
            this.Controls.Add(this.label_song);
            this.Controls.Add(this.label_singer);
            this.Controls.Add(this.saveBtn);
            this.Controls.Add(this.textBox_lrc);
            this.Controls.Add(this.confirmBtn);
            this.Controls.Add(this.textBox_id);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "Form1";
            this.Text = "网易云歌词提取";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox_id;
        private System.Windows.Forms.Button confirmBtn;
        private System.Windows.Forms.TextBox textBox_lrc;
        private System.Windows.Forms.Button saveBtn;
        private System.Windows.Forms.Label label_singer;
        private System.Windows.Forms.Label label_song;
        private System.Windows.Forms.TextBox textBox_song;
        private System.Windows.Forms.TextBox textBox_singer;
        private System.Windows.Forms.ComboBox comboBox;
    }
}

