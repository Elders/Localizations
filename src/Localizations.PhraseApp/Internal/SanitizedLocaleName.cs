using System;

namespace Localizations.PhraseApp.Internal
{
    internal class SanitizedLocaleName
    {
        public static char LocaleSeparator = '-';

        public SanitizedLocaleName(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            Value = name.Replace('_', LocaleSeparator).ToLower();
        }

        public string Value { get; private set; }

        public override string ToString()
        {
            return Value;
        }

        public static implicit operator string(SanitizedLocaleName name)
        {
            return name?.Value;
        }
    }
}
