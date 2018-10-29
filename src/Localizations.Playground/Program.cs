using System;
using Localizations.PhraseApp;

namespace Localizations.Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            LogStartup.Boot();

            TimeSpan ttl = TimeSpan.FromSeconds(1);
            string accessToken = "a";
            string projectId = "e";
            ILocalization localization = new PhraseAppLocalization(accessToken, projectId, ttl);

            var byKey = localization.Get("1vipcustomer", "En");
            var byKeyWithHeader = localization.Get("1vipcustomer", new AcceptLanguageHeader("EN"));
            var getAll = localization.GetAll("eN");
            var getAllWithHeader = localization.GetAll(new AcceptLanguageHeader("EN"));
        }
    }
}
