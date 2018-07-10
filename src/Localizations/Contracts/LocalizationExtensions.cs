using System.Collections.Generic;
using System.Linq;

namespace Localizations.Contracts
{
    public static class LocalizationExtensions
    {
        private static bool TryGetValue(ILocalization localization, string key, string locale, string fallbackValue, out string result)
        {
            result = fallbackValue;

            var translation = localization.Get(key, locale);

            if (translation.Found == true)
            {
                result = translation.Result().Value;
                return true;
            }
            return false;
        }

        private static bool TryGetAllValues(ILocalization localization, string locale, out List<SafeGet<TranslationModel>> result)
        {
            result = new List<SafeGet<TranslationModel>>();

            var translations = localization.GetAll(locale);

            if (ReferenceEquals(null, translations) == false && translations.Any() == true)
            {
                result = translations;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Try to get the transalation based on <paramref name="header"/>. If it the <paramref name="key"/> is missing we are returning "missing-key-'{<paramref name="key"/>}'".
        /// </summary>
        /// <remarks>
        /// Depending on the implementation of <see cref name="ILocalization"/> we might return a translation based on a default locale if we can.
        /// </remarks>
        /// <param name="key">The translation key that will be used to get the translation.</param>
        /// <param name="header">The Accept-Language header that will be used to get the translation.</param>
        /// <param name="fallbackValue">The fallback value that we are going to return if we do not find the specified <paramref name="key"/> and there is no default locale.</param>
        /// <returns>The resulting translation.</returns>
        public static string GetValue(this ILocalization localization, string key, AcceptLanguageHeader header, string fallbackValue)
        {
            var result = string.Empty;

            foreach (var locale in header.Locales)
            {
                if (TryGetValue(localization, key, locale, fallbackValue, out result) == true)
                    return result;
            }

            return result;
        }

        /// <summary>
        /// Try to get the transalation based on <paramref name="locale"/>. If it the <paramref name="key"/> is missing we are returning <paramref name="fallbackValue"/>.
        /// </summary>
        /// <remarks>
        /// Depending on the implementation of <see cref name="ILocalization"/> we might return a translation based on a default locale if we can.
        /// </remarks>
        /// <param name="key">The translation key that will be used to get the translation.</param>
        /// <param name="locale">The local that will be used to get the translation.</param>
        /// <param name="fallbackValue">The fallback value that we are going to return if we do not find the specified <paramref name="key"/> and there is no default locale.</param>
        /// <returns>The resulting translation.</returns>
        public static string GetValue(this ILocalization localization, string key, string locale, string fallbackValue)
        {
            var result = string.Empty;

            TryGetValue(localization, key, locale, fallbackValue, out result);

            return result;
        }

        /// <summary>
        /// Try to get the transalation based on <paramref name="header"/>. If it the <paramref name="key"/> is missing we are returning "missing-key-'{<paramref name="key"/>}'".
        /// </summary>
        /// <remarks>
        /// Depending on the implementation of <see cref name="ILocalization"/> we might return a translation based on a default locale if we can.
        /// </remarks>
        /// <param name="key">The translation key that will be used to get the translation.</param>
        /// <param name="header">The Accept-Language header that will be used to get the translation.</param>
        /// <returns>The resulting translation.</returns>
        public static string GetValue(this ILocalization localization, string key, AcceptLanguageHeader header)
        {
            var result = string.Empty;
            var fallbackValue = $"missing-key-'{key}'";

            foreach (var locale in header.Locales)
            {
                if (TryGetValue(localization, key, locale, fallbackValue, out result) == true)
                    return result;
            }

            return result;
        }

        /// <summary>
        /// Try to get the transalation based on <paramref name="locale"/>. If it the <paramref name="key"/> is missing we are returning "missing-key-'{<paramref name="key"/>}'-locale-'{<paramref name="locale"/>}'".
        /// </summary>
        /// <remarks>
        /// Depending on the implementation of <see cref name="ILocalization"/> we might return a translation based on a default locale if we can.
        /// </remarks>
        /// <param name="key">The translation key that will be used to get the translation.</param>
        /// <param name="locale">The local that will be used to get the translation.</param>
        /// <returns>The resulting translation.</returns>
        public static string GetValue(this ILocalization localization, string key, string locale)
        {
            var result = string.Empty;
            var fallbackValue = $"missing-key-'{key}'-locale-'{locale}'";
            TryGetValue(localization, key, locale, fallbackValue, out result);
            return result;
        }

        /// <summary>
        /// Try to get the transalations based on <paramref name="header"/>.
        /// </summary>
        /// <remarks>
        /// Depending on the implementation of <see cref name="ILocalization"/> we might return translations based on a default locale if we can.
        /// </remarks>
        /// <param name="header">The Accept-Language header that will be used to get the translations.</param>
        /// <returns>The resulting translations for this <paramref name="header"/>. If no translations are found for this <paramref name="header"/> the collection will be empty.</returns>
        public static Dictionary<string, string> GetAllValues(this ILocalization localization, AcceptLanguageHeader header)
        {
            var result = new List<SafeGet<TranslationModel>>();

            foreach (var locale in header.Locales)
            {
                if (TryGetAllValues(localization, locale, out result) == true)
                    return result.ToDictionary(key => key.Result().Key, value => value.Result().Value);
            }

            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Try to get the transalations based on <paramref name="locale"/>.
        /// </summary>
        /// <remarks>
        /// Depending on the implementation of <see cref name="ILocalization"/> we might return translations based on a default locale if we can.
        /// </remarks>
        /// <param name="locale">The local that will be used to get the translations.</param>
        /// <returns>The resulting translations for this <paramref name="locale"/>. If no translations are found for this <paramref name="locale"/> the collection will be empty.</returns>
        public static Dictionary<string, string> GetAllValues(this ILocalization localization, string locale)
        {
            var result = new List<SafeGet<TranslationModel>>();

            if (TryGetAllValues(localization, locale, out result) == true)
                return result.ToDictionary(key => key.Result().Key, value => value.Result().Value);

            return new Dictionary<string, string>();
        }
    }
}
