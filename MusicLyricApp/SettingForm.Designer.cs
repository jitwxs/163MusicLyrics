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
            this.btn_save = new System.Windows.Forms.Button();
            this.checkBox_reme_param = new System.Windows.Forms.CheckBox();
            this.checkBox_auto_read_clipboard = new System.Windows.Forms.CheckBox();
            this.checkBox_auto_check_update = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // btn_save
            // 
            this.btn_save.Location = new System.Drawing.Point(173, 114);
            this.btn_save.Name = "btn_save";
            this.btn_save.Size = new System.Drawing.Size(102, 44);
            this.btn_save.TabIndex = 0;
            this.btn_save.Text = "保存";
            this.btn_save.UseVisualStyleBackColor = true;
            this.btn_save.Click += new System.EventHandler(this.btn_save_Click);
            // 
            // checkBox_reme_param
            // 
            this.checkBox_reme_param.Location = new System.Drawing.Point(27, 25);
            this.checkBox_reme_param.Name = "checkBox_reme_param";
            this.checkBox_reme_param.Size = new System.Drawing.Size(78, 24);
            this.checkBox_reme_param.TabIndex = 1;
            this.checkBox_reme_param.Text = "参数记忆";
            this.checkBox_reme_param.UseVisualStyleBackColor = true;
            // 
            // checkBox_auto_read_clipboard
            // 
            this.checkBox_auto_read_clipboard.Location = new System.Drawing.Point(27, 77);
            this.checkBox_auto_read_clipboard.Name = "checkBox_auto_read_clipboard";
            this.checkBox_auto_read_clipboard.Size = new System.Drawing.Size(112, 26);
            this.checkBox_auto_read_clipboard.TabIndex = 2;
            this.checkBox_auto_read_clipboard.Text = "自动读取剪贴板";
            this.checkBox_auto_read_clipboard.UseVisualStyleBackColor = true;
            // 
            // checkBox_auto_check_update
            // 
            this.checkBox_auto_check_update.Location = new System.Drawing.Point(27, 126);
            this.checkBox_auto_check_update.Name = "checkBox_auto_check_update";
            this.checkBox_auto_check_update.Size = new System.Drawing.Size(97, 32);
            this.checkBox_auto_check_update.TabIndex = 3;
            this.checkBox_auto_check_update.Text = "自动检查更新";
            this.checkBox_auto_check_update.UseVisualStyleBackColor = true;
            // 
            // SettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(307, 170);
            this.Controls.Add(this.checkBox_auto_check_update);
            this.Controls.Add(this.checkBox_auto_read_clipboard);
            this.Controls.Add(this.checkBox_reme_param);
            this.Controls.Add(this.btn_save);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "SettingForm";
            this.Text = "设置";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.CheckBox checkBox_auto_check_update;

        private System.Windows.Forms.CheckBox checkBox_auto_read_clipboard;

        private System.Windows.Forms.CheckBox checkBox_reme_param;

        private System.Windows.Forms.Button btn_save;

        #endregion
    }
}