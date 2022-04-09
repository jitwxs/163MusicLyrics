using System.ComponentModel;

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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingForm));
            this.Save_Btn = new System.Windows.Forms.Button();
            this.RememberParam_ComboBox = new System.Windows.Forms.CheckBox();
            this.AutoReadClipboard_CheckBox = new System.Windows.Forms.CheckBox();
            this.AutoCheckUpdate_CheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // Save_Btn
            // 
            this.Save_Btn.Location = new System.Drawing.Point(173, 114);
            this.Save_Btn.Name = "Save_Btn";
            this.Save_Btn.Size = new System.Drawing.Size(102, 44);
            this.Save_Btn.TabIndex = 0;
            this.Save_Btn.Text = "保存";
            this.Save_Btn.UseVisualStyleBackColor = true;
            this.Save_Btn.Click += new System.EventHandler(this.Save_Btn_Click);
            // 
            // RememberParam_ComboBox
            // 
            this.RememberParam_ComboBox.Location = new System.Drawing.Point(27, 25);
            this.RememberParam_ComboBox.Name = "RememberParam_ComboBox";
            this.RememberParam_ComboBox.Size = new System.Drawing.Size(78, 24);
            this.RememberParam_ComboBox.TabIndex = 1;
            this.RememberParam_ComboBox.Text = "参数记忆";
            this.RememberParam_ComboBox.UseVisualStyleBackColor = true;
            // 
            // AutoReadClipboard_CheckBox
            // 
            this.AutoReadClipboard_CheckBox.Location = new System.Drawing.Point(27, 77);
            this.AutoReadClipboard_CheckBox.Name = "AutoReadClipboard_CheckBox";
            this.AutoReadClipboard_CheckBox.Size = new System.Drawing.Size(112, 26);
            this.AutoReadClipboard_CheckBox.TabIndex = 2;
            this.AutoReadClipboard_CheckBox.Text = "自动读取剪贴板";
            this.AutoReadClipboard_CheckBox.UseVisualStyleBackColor = true;
            // 
            // AutoCheckUpdate_CheckBox
            // 
            this.AutoCheckUpdate_CheckBox.Location = new System.Drawing.Point(27, 126);
            this.AutoCheckUpdate_CheckBox.Name = "AutoCheckUpdate_CheckBox";
            this.AutoCheckUpdate_CheckBox.Size = new System.Drawing.Size(97, 32);
            this.AutoCheckUpdate_CheckBox.TabIndex = 3;
            this.AutoCheckUpdate_CheckBox.Text = "自动检查更新";
            this.AutoCheckUpdate_CheckBox.UseVisualStyleBackColor = true;
            // 
            // SettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(307, 170);
            this.Controls.Add(this.AutoCheckUpdate_CheckBox);
            this.Controls.Add(this.AutoReadClipboard_CheckBox);
            this.Controls.Add(this.RememberParam_ComboBox);
            this.Controls.Add(this.Save_Btn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "SettingForm";
            this.Text = "设置";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.CheckBox AutoCheckUpdate_CheckBox;

        private System.Windows.Forms.CheckBox AutoReadClipboard_CheckBox;

        private System.Windows.Forms.CheckBox RememberParam_ComboBox;

        private System.Windows.Forms.Button Save_Btn;

        #endregion
    }
}