namespace Localizations.Contracts
{
    public interface ILocalization
    {
        SafeGet<TranslationModel> Get(string key, string locale);

        /// <summary>
        /// Specifies if fall back to two letter part of locale is allowed e.g en-GB would fall back to en
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        ILocalization UseStrictLocale(bool value);

        /// <summary>
        /// Specifies default fall back locale
        /// </summary>
        /// <param name="locale"></param>
        /// <returns></returns>
        ILocalization UseDefaultLocale(string locale);
    }
}
