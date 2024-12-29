using System;
using System.Text;
using Markdig;
using MusicLyricApp.Bean;

namespace MusicLyricApp
{
    public partial class ShortcutForm : MusicLyricForm
    {
        private ScalingFormConfig _scalingFormConfig;
        
        public ShortcutForm()
        {
            InitializeComponent();
            
            _scalingFormConfig = new ScalingFormConfig(this);

            var sb = new StringBuilder();
            sb
                .Append("<style>.td{width:100px; overflow:hidden}</style>")
                .Append("<table border='1' style='border-collapse: collapse;'>")
                .Append("<thead>")
                .Append("<tr>")
                .Append("<th class='td'>功能</th>")
                .Append("<th class='td'>快捷键</th>")
                .Append("<tbody align='center'>")
                .Append("<tr>")
                .Append("<th class='td'>精确搜索</th>").Append("<td class='td'><kbd>Enter</kbd></td>")
                .Append("</tr>")
                .Append("<tr>")
                .Append("<th class='td'>模糊搜索</th>").Append("<td class='td'><kbd>Ctrl</kbd> + <kbd>Enter</kbd></td>")
                .Append("</tr>")
                .Append("<tr>")
                .Append("<th class='td'>保存</th>").Append("<td class='td'><kbd>Ctrl</kbd> + <kbd>S</kbd></td>")
                .Append("</tr>")
                .Append("</tbody>")
                .Append("</tr>")
                .Append("</thead>")
                .Append("</table>");
            
            Shortcut_Browser.DocumentText = Markdown.ToHtml(sb.ToString());
        }

        private void ShortcutForm_Resize(object sender, EventArgs e)
        {
            _scalingFormConfig?.SetControls(this);
        }
    }
}