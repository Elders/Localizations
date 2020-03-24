using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Localizations
{
    public static class LocalizationExtensions
    {
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
        public static async Task<string> GetValueAsync(this ILocalization localization, string key, AcceptLanguageHeader header, string fallbackValue)
        {
            string result = fallbackValue;

            var translation = await localization.GetAsync(key, header).ConfigureAwait(false);

            if (translation.Found == true)
                result = translation.Result().Value;

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
        public static async Task<string> GetValueAsync(this ILocalization localization, string key, string locale, string fallbackValue)
        {
            string result = fallbackValue;

            var translation = await localization.GetAsync(key, locale).ConfigureAwait(false);

            if (translation.Found == true)
                result = translation.Result().Value;

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
        public static Task<string> GetValueAsync(this ILocalization localization, string key, AcceptLanguageHeader header)
        {
            var fallbackValue = $"missing-key-'{key}'";
            return GetValueAsync(localization, key, header, fallbackValue);
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
        public static Task<string> GetValueAsync(this ILocalization localization, string key, string locale)
        {
            var fallbackValue = $"missing-key-'{key}'-locale-'{locale}'";
            return GetValueAsync(localization, key, locale, fallbackValue);
        }

        /// <summary>
        /// Try to get the transalations based on <paramref name="header"/>.
        /// </summary>
        /// <remarks>
        /// Depending on the implementation of <see cref name="ILocalization"/> we might return translations based on a default locale if we can.
        /// </remarks>
        /// <param name="header">The Accept-Language header that will be used to get the translations.</param>
        /// <returns>The resulting translations for this <paramref name="header"/>. If no translations are not found for this <paramref name="header"/> the collection will be empty.</returns>
        public static async Task<Dictionary<string, string>> GetAllValuesAsync(this ILocalization localization, AcceptLanguageHeader header)
        {
            var translations = await localization.GetAllAsync(header).ConfigureAwait(false);

            if (translations is null == false && translations.Any() == true)
            {
                return translations.ToDictionary(key => key.Result().Key, value => value.Result().Value);
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
        /// <returns>The resulting translations for this <paramref name="locale"/>. If no translations are not found for this <paramref name="locale"/> the collection will be empty.</returns>
        public static async Task<Dictionary<string, string>> GetAllValuesAsync(this ILocalization localization, string locale)
        {
            var translations = await localization.GetAllAsync(locale).ConfigureAwait(false);

            if (translations is null == false && translations.Any() == true)
            {
                return translations.ToDictionary(key => key.Result().Key, value => value.Result().Value);
            }

            return new Dictionary<string, string>();
        }
    }
}
