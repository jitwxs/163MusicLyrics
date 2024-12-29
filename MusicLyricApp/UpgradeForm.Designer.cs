using System.ComponentModel;

namespace MusicLyricApp
{
    partial class UpgradeForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpgradeForm));
            this.label1 = new System.Windows.Forms.Label();
            this.Upgrade_Btn = new System.Windows.Forms.Button();
            this.UpgradeSpan1_Label = new System.Windows.Forms.Label();
            this.UpgradeSpan2_Label = new System.Windows.Forms.Label();
            this.UpgradeLog_Browser = new System.Windows.Forms.WebBrowser();
            this.UpgradeTag_Label = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.SystemColors.Control;
            this.label1.Cursor = System.Windows.Forms.Cursors.No;
            this.label1.Font = new System.Drawing.Font("微软雅黑", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(12, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(143, 33);
            this.label1.TabIndex = 0;
            this.label1.Text = "检测到新版本";
            // 
            // Upgrade_Btn
            // 
            this.Upgrade_Btn.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.Upgrade_Btn.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.Upgrade_Btn.Location = new System.Drawing.Point(331, 20);
            this.Upgrade_Btn.Name = "Upgrade_Btn";
            this.Upgrade_Btn.Size = new System.Drawing.Size(142, 60);
            this.Upgrade_Btn.TabIndex = 4;
            this.Upgrade_Btn.Text = "立即升级";
            this.Upgrade_Btn.UseVisualStyleBackColor = false;
            this.Upgrade_Btn.Click += new System.EventHandler(this.Upgrade_Btn_Click);
            // 
            // UpgradeSpan1_Label
            // 
            this.UpgradeSpan1_Label.Location = new System.Drawing.Point(21, 59);
            this.UpgradeSpan1_Label.Name = "UpgradeSpan1_Label";
            this.UpgradeSpan1_Label.Size = new System.Drawing.Size(304, 28);
            this.UpgradeSpan1_Label.TabIndex = 5;
            this.UpgradeSpan1_Label.Text = "label2";
            // 
            // UpgradeSpan2_Label
            // 
            this.UpgradeSpan2_Label.Location = new System.Drawing.Point(21, 87);
            this.UpgradeSpan2_Label.Name = "UpgradeSpan2_Label";
            this.UpgradeSpan2_Label.Size = new System.Drawing.Size(304, 25);
            this.UpgradeSpan2_Label.TabIndex = 6;
            this.UpgradeSpan2_Label.Text = "label3";
            // 
            // UpgradeLog_Browser
            // 
            this.UpgradeLog_Browser.Location = new System.Drawing.Point(15, 115);
            this.UpgradeLog_Browser.MinimumSize = new System.Drawing.Size(20, 20);
            this.UpgradeLog_Browser.Name = "UpgradeLog_Browser";
            this.UpgradeLog_Browser.Size = new System.Drawing.Size(457, 265);
            this.UpgradeLog_Browser.TabIndex = 7;
            // 
            // UpgradeTag_Label
            // 
            this.UpgradeTag_Label.Font = new System.Drawing.Font("微软雅黑", 10.5F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.UpgradeTag_Label.Location = new System.Drawing.Point(161, 26);
            this.UpgradeTag_Label.Name = "UpgradeTag_Label";
            this.UpgradeTag_Label.Size = new System.Drawing.Size(57, 22);
            this.UpgradeTag_Label.TabIndex = 8;
            this.UpgradeTag_Label.Text = "vX.x";
            // 
            // UpgradeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(485, 397);
            this.Controls.Add(this.UpgradeTag_Label);
            this.Controls.Add(this.UpgradeLog_Browser);
            this.Controls.Add(this.UpgradeSpan2_Label);
            this.Controls.Add(this.UpgradeSpan1_Label);
            this.Controls.Add(this.Upgrade_Btn);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "UpgradeForm";
            this.Text = "版本更新";
            this.Resize += new System.EventHandler(this.UpgradeForm_Resize);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Label UpgradeTag_Label;

        private System.Windows.Forms.WebBrowser UpgradeLog_Browser;

        private System.Windows.Forms.Label UpgradeSpan1_Label;
        private System.Windows.Forms.Label UpgradeSpan2_Label;

        private System.Windows.Forms.Button Upgrade_Btn;

        private System.Windows.Forms.Label label1;

        #endregion
    }
}