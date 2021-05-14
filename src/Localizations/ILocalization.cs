using System.Collections.Generic;
using System.Threading.Tasks;

namespace Localizations
{
    public interface ILocalization
    {
        Task<SafeGet<TranslationModel>> GetAsync(string key, string locale);

        Task<SafeGet<TranslationModel>> GetAsync(string key, AcceptLanguageHeader header);

        Task<List<SafeGet<TranslationModel>>> GetAllAsync(string locale);

        Task<List<SafeGet<TranslationModel>>> GetAllAsync(AcceptLanguageHeader header);
    }
}
