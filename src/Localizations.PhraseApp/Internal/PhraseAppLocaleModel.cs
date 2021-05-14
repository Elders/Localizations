using System;
using System.Collections.Generic;

namespace Localizations.PhraseApp.Internal
{
    internal class PhraseAppLocaleModel
    {
        public PhraseAppLocaleModel()
        {
            Plural_forms = new List<string>();
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        public bool Default { get; set; }

        public bool Main { get; set; }

        public bool Rtl { get; set; }

        public List<string> Plural_forms { get; set; }

        public PhraseAppSourceLocaleModel Source_locale { get; set; }

        public DateTime Created_at { get; set; }

        public DateTime Updated_at { get; set; }

        public override string ToString()
        {
            return $"Id: {Id}, Name: {Name}, Code: {Code}, Default: {Default}, Main: {Main}, Rtl: {Rtl}";
        }
    }
}
