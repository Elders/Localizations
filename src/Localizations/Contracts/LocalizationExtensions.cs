namespace Localizations.Contracts
{
    public static class LocalizationExtensions
    {
        public static string GetValue(this ILocalization localization, string key, string locale, string defaultValue)
        {
            var translation = localization.Get(key, locale);

            if (translation.Found == true)
                return translation.Result().Value;

            return defaultValue;
        }

        public static string GetValue(this ILocalization localization, string key, string locale)
        {
            var translation = localization.Get(key, locale);

            if (translation.Found == true)
                return translation.Result().Value;

            return $"missing-key-'{key}'-locale-'{locale}'";
        }
    }
}