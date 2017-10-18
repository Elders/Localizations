using System;

namespace Localizations.Contracts
{
    public class TranslationModel
    {
        public TranslationModel(string key, string value, string locale)
        {
            if (string.IsNullOrEmpty(key) == true) throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrEmpty(locale) == true) throw new ArgumentNullException(nameof(locale));

            Key = key;
            Value = value;
            Locale = locale;
        }

        public string Key { get; private set; }

        public string Value { get; private set; }

        public string Locale { get; private set; }
    }
}
