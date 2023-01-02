using MusicLyricApp.Bean;

namespace MusicLyricApp.Api.Translate
{
    public interface ITranslateApi
    {
        string[] Translate(string[] inputs, LanguageEnum inputLanguage, LanguageEnum outputLanguage);

        bool IsSupport(LanguageEnum inputLanguage, LanguageEnum outputLanguage);
    }
}