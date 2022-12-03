using System.Windows.Forms;

namespace MusicLyricApp
{
    public class MusicLyricForm : Form
    {
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
    }
}