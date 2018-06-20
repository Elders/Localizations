using System.Collections.Generic;

namespace Localizations.Contracts
{
    public interface ILocalization
    {
        SafeGet<TranslationModel> Get(string key, string locale);

        List<SafeGet<TranslationModel>> GetAll(string locale);
    }
}
