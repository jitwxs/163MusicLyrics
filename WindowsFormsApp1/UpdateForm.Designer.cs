namespace 网易云歌词提取
{
    partial class UpdateForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.softwareText = new System.Windows.Forms.Label();
            this.textBox_UpdateContent = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label_UpdateDate = new System.Windows.Forms.Label();
            this.label_authorName = new System.Windows.Forms.Label();
            this.label_downloadCount = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // softwareText
            // 
            this.softwareText.AutoSize = true;
            this.softwareText.Font = new System.Drawing.Font("宋体", 11F);
            this.softwareText.Location = new System.Drawing.Point(369, 9);
            this.softwareText.Name = "softwareText";
            this.softwareText.Size = new System.Drawing.Size(129, 19);
            this.softwareText.TabIndex = 0;
            this.softwareText.Tag = "";
            this.softwareText.Text = "SoftwareName";
            // 
            // textBox_UpdateContent
            // 
            this.textBox_UpdateContent.Font = new System.Drawing.Font("宋体", 11F);
            this.textBox_UpdateContent.Location = new System.Drawing.Point(215, 154);
            this.textBox_UpdateContent.Multiline = true;
            this.textBox_UpdateContent.Name = "textBox_UpdateContent";
            this.textBox_UpdateContent.Size = new System.Drawing.Size(465, 368);
            this.textBox_UpdateContent.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 11F);
            this.label1.Location = new System.Drawing.Point(389, 132);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(85, 19);
            this.label1.TabIndex = 2;
            this.label1.Text = "更新内容";
            // 
            // label_UpdateDate
            // 
            this.label_UpdateDate.AutoSize = true;
            this.label_UpdateDate.Font = new System.Drawing.Font("宋体", 10F);
            this.label_UpdateDate.Location = new System.Drawing.Point(498, 132);
            this.label_UpdateDate.Name = "label_UpdateDate";
            this.label_UpdateDate.Size = new System.Drawing.Size(98, 17);
            this.label_UpdateDate.TabIndex = 3;
            this.label_UpdateDate.Text = "UpdateDate";
            // 
            // label_authorName
            // 
            this.label_authorName.AutoSize = true;
            this.label_authorName.Font = new System.Drawing.Font("宋体", 11F);
            this.label_authorName.Location = new System.Drawing.Point(609, 12);
            this.label_authorName.Name = "label_authorName";
            this.label_authorName.Size = new System.Drawing.Size(69, 19);
            this.label_authorName.TabIndex = 4;
            this.label_authorName.Text = "author";
            // 
            // label_downloadCount
            // 
            this.label_downloadCount.AutoSize = true;
            this.label_downloadCount.Font = new System.Drawing.Font("宋体", 10F);
            this.label_downloadCount.Location = new System.Drawing.Point(230, 132);
            this.label_downloadCount.Name = "label_downloadCount";
            this.label_downloadCount.Size = new System.Drawing.Size(125, 17);
            this.label_downloadCount.TabIndex = 5;
            this.label_downloadCount.Text = "downloadCount";
            // 
            // UpdateForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(905, 583);
            this.Controls.Add(this.label_downloadCount);
            this.Controls.Add(this.label_authorName);
            this.Controls.Add(this.label_UpdateDate);
            this.Controls.Add(this.textBox_UpdateContent);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.softwareText);
            this.Name = "UpdateForm";
            this.Text = "Update";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label softwareText;
        private System.Windows.Forms.TextBox textBox_UpdateContent;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label_UpdateDate;
        private System.Windows.Forms.Label label_authorName;
        private System.Windows.Forms.Label label_downloadCount;
    }
}