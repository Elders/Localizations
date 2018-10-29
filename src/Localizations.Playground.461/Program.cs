using System;
using Localizations.PhraseApp;

namespace Localizations.Playground._461
{
    class Program
    {
        static void Main(string[] args)
        {
            LogStartup.Boot();
            TimeSpan ttl = TimeSpan.FromMinutes(1);
            string accessToken = "x";
            string projectId = "y";
            ILocalization localization = new PhraseAppLocalization(accessToken, projectId, ttl);

            var byKey = localization.Get("1vipcustomer", "En");

        }
    }
}
