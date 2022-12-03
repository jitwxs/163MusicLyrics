using MusicLyricApp.Bean;

namespace MusicLyricApp
{
    public partial class BlurForm : MusicLyricForm
    {
        private SearchResultVo _searchResultVo;
        
        public BlurForm(SearchResultVo searchResultVo)
        {
            _searchResultVo = searchResultVo;
            
            InitializeComponent();
            
            AfterInitializeComponent();
        }

        private void AfterInitializeComponent()
        {
            
        }
    }
}