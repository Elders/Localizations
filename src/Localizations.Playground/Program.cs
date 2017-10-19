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
            var accessToken = "";
            var projectId = "";
            ILocalization client = new PhraseAppLocalization(accessToken, projectId, TimeSpan.FromSeconds(2)).UseDefaultLocale("en").UseStrictLocale(false);

            var translation = client.Get("help_url", "bg").Result(new TranslationModel("help_url", "none", "bg"));
            translation = client.Get("help_url", "bg-BG").Result(new TranslationModel("help_url", "none", "bg"));
            System.Console.ReadLine();
        }
    }
}
