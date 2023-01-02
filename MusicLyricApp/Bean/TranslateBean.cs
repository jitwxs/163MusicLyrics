namespace MusicLyricApp.Bean
{
    public class TranslateBean
    {
        public class CaiYunTranslateResult
        {
            public string[] Target { get; set; }

            public int Rc { get; set; }

            public double Confidence { get; set; }
        }
    }
}