using System;

namespace Localizations.PhraseApp.Internal
{
    internal class SanitizedPhraseAppLocaleModel
    {
        public SanitizedPhraseAppLocaleModel(string id, SanitizedLocaleName name)
        {
            if (string.IsNullOrEmpty(id) == true) throw new ArgumentNullException(nameof(id));
            if (name is null == true) throw new ArgumentNullException(nameof(name));

            Id = id;
            Name = name;
        }

        public string Id { get; set; }

        public SanitizedLocaleName Name { get; set; }
    }
}
