namespace Localizations.PhraseApp
{
    public class SanitizedLocaleName
    {
        public static char LocaleSeparator = '-';

        public SanitizedLocaleName(string name)
        {
            name = name.Replace('_', LocaleSeparator);
            Value = name.ToLower();
        }

        public string Value { get; private set; }

        public override string ToString()
        {
            return Value;
        }

        public static implicit operator string(SanitizedLocaleName name)
        {
            return name.Value;
        }
    }
}
