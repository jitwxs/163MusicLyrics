using System.ComponentModel;

namespace MusicLyricApp
{
    partial class ShortcutForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ShortcutForm));
            this.Shortcut_Browser = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // Shortcut_Browser
            // 
            this.Shortcut_Browser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Shortcut_Browser.Location = new System.Drawing.Point(0, 0);
            this.Shortcut_Browser.MinimumSize = new System.Drawing.Size(20, 20);
            this.Shortcut_Browser.Name = "Shortcut_Browser";
            this.Shortcut_Browser.ScrollBarsEnabled = false;
            this.Shortcut_Browser.Size = new System.Drawing.Size(254, 141);
            this.Shortcut_Browser.TabIndex = 0;
            // 
            // ShortcutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(254, 141);
            this.Controls.Add(this.Shortcut_Browser);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ShortcutForm";
            this.Text = "快捷键";
            this.Resize += new System.EventHandler(this.ShortcutForm_Resize);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.WebBrowser Shortcut_Browser;

        #endregion
    }
}