using System;

namespace Localizations.Contracts
{
    public class TranslationModel
    {
        public TranslationModel(string key, string value, string locale) : this(key, value, locale, DateTime.UtcNow.ToFileTimeUtc())
        { }

        /// <summary>
        ///
        /// </summary>
        /// <param name="key">Translation key</param>
        /// <param name="value">Translation value</param>
        /// <param name="locale">Translation locale</param>
        /// <param name="lastModified">Filetime UTC</param>
        public TranslationModel(string key, string value, string locale, long lastModified)
        {
            if (string.IsNullOrEmpty(key) == true) throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrEmpty(locale) == true) throw new ArgumentNullException(nameof(locale));

            Key = key;
            Value = value;
            Locale = locale;
            LastModified = lastModified;
        }

        public string Key { get; private set; }

        public string Value { get; private set; }

        public string Locale { get; private set; }

        public long LastModified { get; private set; }

        public static TranslationModel MissingTranslation(string key)
        {
            return new TranslationModel("missing-translation-" + key, "missing-translation-" + key, "missing-translation-" + key);
        }
    }
}
