using Avalonia.Controls;

namespace MusicLyricApp.Views;

public partial class FormatConvertView : UserControl
{
    public FormatConvertView()
    {
        InitializeComponent();

        // Textarea keeps a fixed frame height, so let it follow its pane to fill the column.
        InputPanel.SizeChanged += (_, e) =>
        {
            if (e.NewSize.Height > InputTextArea.FrameMinHeight)
            {
                InputTextArea.FrameHeight = e.NewSize.Height;
            }
        };
    }
}
