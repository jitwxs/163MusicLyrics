using System;
using System.Windows.Forms;

namespace MusicLyricApp
{
    public class MusicLyricForm : Form
    {
        /// <summary>
        /// 窗体初始状态
        /// </summary>
        private FormWindowState _fwsPrevious;
        
        public void RestoreWindow()
        {
            WindowState = _fwsPrevious;
            ShowInTaskbar = true;
        }
        
        /// <summary>
        /// listener keyboard key
        /// </summary>
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (ModifierKeys == Keys.None && keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            _fwsPrevious = WindowState;
        }

        // to resolve the minimized form active, see https://www.nhooo.com/note/qack1f.html
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            
            if (WindowState == FormWindowState.Minimized)
            {
                ShowInTaskbar = false;
            }
            else if (_fwsPrevious != WindowState)
            {
                _fwsPrevious = WindowState;
            }
        }
    }
}