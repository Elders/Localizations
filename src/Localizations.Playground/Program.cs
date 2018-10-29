using System;
using Localizations.PhraseApp;

namespace Localizations.Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            TimeSpan ttl = TimeSpan.FromMinutes(1);
            string accessToken = "";
            string projectId = "";
            ILocalization localization = new PhraseAppLocalization(accessToken, projectId, ttl);

            var byKey = localization.Get("1vipcustomer", "En");
            var byKeyWithHeader = localization.Get("1vipcustomer", new AcceptLanguageHeader("EN"));
            var getAll = localization.GetAll("eN");
            var getAllWithHeader = localization.GetAll(new AcceptLanguageHeader("EN"));
        }
    }
}
