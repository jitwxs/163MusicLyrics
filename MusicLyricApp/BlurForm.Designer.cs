using System.ComponentModel;

namespace MusicLyricApp
{
    partial class BlurForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BlurForm));
            this.Blur_DataGridView = new System.Windows.Forms.DataGridView();
            this.Blur_ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.Blur_Download_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.Blur_DataGridView)).BeginInit();
            this.Blur_ContextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // Blur_DataGridView
            // 
            this.Blur_DataGridView.AllowUserToAddRows = false;
            this.Blur_DataGridView.AllowUserToDeleteRows = false;
            this.Blur_DataGridView.AllowUserToResizeColumns = false;
            this.Blur_DataGridView.AllowUserToResizeRows = false;
            this.Blur_DataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.Blur_DataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.Blur_DataGridView.ContextMenuStrip = this.Blur_ContextMenuStrip;
            this.Blur_DataGridView.GridColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.Blur_DataGridView.Location = new System.Drawing.Point(0, 0);
            this.Blur_DataGridView.Name = "Blur_DataGridView";
            this.Blur_DataGridView.ReadOnly = true;
            this.Blur_DataGridView.RowTemplate.Height = 23;
            this.Blur_DataGridView.Size = new System.Drawing.Size(622, 397);
            this.Blur_DataGridView.TabIndex = 0;
            this.Blur_DataGridView.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.BlurSearch_DataGridView_CellMouseDown);
            // 
            // Blur_ContextMenuStrip
            // 
            this.Blur_ContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.Blur_Download_ToolStripMenuItem });
            this.Blur_ContextMenuStrip.Name = "Blur_ContextMenuStrip";
            this.Blur_ContextMenuStrip.Size = new System.Drawing.Size(101, 26);
            // 
            // Blur_Download_ToolStripMenuItem
            // 
            this.Blur_Download_ToolStripMenuItem.Name = "Blur_Download_ToolStripMenuItem";
            this.Blur_Download_ToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.Blur_Download_ToolStripMenuItem.Text = "下载";
            this.Blur_Download_ToolStripMenuItem.Click += new System.EventHandler(this.Blur_Download_ToolStripMenuItem_Click);
            // 
            // BlurForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(620, 396);
            this.Controls.Add(this.Blur_DataGridView);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BlurForm";
            this.Text = "搜索结果";
            ((System.ComponentModel.ISupportInitialize)(this.Blur_DataGridView)).EndInit();
            this.Blur_ContextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.ContextMenuStrip Blur_ContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem Blur_Download_ToolStripMenuItem;

        private System.Windows.Forms.DataGridView Blur_DataGridView;

        #endregion
    }
}