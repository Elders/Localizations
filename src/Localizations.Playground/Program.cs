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

            var translation = client.Get("help_url", "bg");
            translation = client.Get("help_url", "bg-BG");
            System.Console.ReadLine();
        }
    }
}
