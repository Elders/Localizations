using System.Collections.Generic;
using System.Linq;

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

        public static Dictionary<string, string> GetAll(this ILocalization localization, string locale)
        {
            var translations = localization.GetAll(locale);

            if (ReferenceEquals(null, translations))
                return new Dictionary<string, string>();

            return translations.ToDictionary(key => key.Result().Key, value => value.Result().Value);
        }
    }
}
