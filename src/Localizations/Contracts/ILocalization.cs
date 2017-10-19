namespace Localizations.Contracts
{
    public interface ILocalization
    {
        SafeGet<TranslationModel> Get(string key, string locale);
    }
}
