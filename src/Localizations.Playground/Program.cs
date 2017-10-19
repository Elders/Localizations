using System;
using Localizations.Contracts;
using Localizations.PhraseApp;

namespace Localizations.Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            LogStartup.Boot();
            var accessToken = "501f395d764c1949c01d3c8fe5bac9b0d20a124f77f283859d3348dadd6ec984";
            var projectId = "8d9a41a987d79eae77db46efe676d914";
            ILocalization client = new PhraseAppLocalization(accessToken, projectId, TimeSpan.FromSeconds(2)).UseDefaultLocale("en").UseStrictLocale(false);

            var translation = client.Get("help_url", "bg").Result(new TranslationModel("help_url", "none", "bg"));
            translation = client.Get("help_urlx", "bg-BG").Result(new TranslationModel("help_urlx", "none", "bg"));
            System.Console.ReadLine();
        }
    }
}
