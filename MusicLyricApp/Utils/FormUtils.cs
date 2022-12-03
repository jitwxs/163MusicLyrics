using System;
using System.Drawing;
using System.Windows.Forms;
using MusicLyricApp.Bean;

namespace MusicLyricApp.Utils
{
    public class FormUtils
    {
        public static void OpenForm(Form openForm, Func<int, Form> createFunc, Form baseForm)
        {
            if (openForm == null || openForm.IsDisposed)
            {
                openForm = createFunc.Invoke(0);

                openForm.Location = new Point(baseForm.Left + Constants.SettingFormOffset,
                    baseForm.Top + Constants.SettingFormOffset);
                openForm.StartPosition = FormStartPosition.Manual;

                openForm.Show();
            }
            else
            {
                openForm.Activate();
            }
        }
    }
}