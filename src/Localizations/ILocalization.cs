using System.Collections.Generic;

namespace Localizations
{
    public interface ILocalization
    {
        SafeGet<TranslationModel> Get(string key, string locale);

        SafeGet<TranslationModel> Get(string key, AcceptLanguageHeader header);

        List<SafeGet<TranslationModel>> GetAll(string locale);

        List<SafeGet<TranslationModel>> GetAll(AcceptLanguageHeader header);
    }
}
