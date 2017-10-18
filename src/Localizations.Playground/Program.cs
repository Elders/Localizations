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
            ILocalization client = new PhraseAppLocalization(accessToken, projectId, TimeSpan.FromSeconds(2));

            var translation = client.Get("home", "bg");
            translation = client.Get("home", "bg");
            System.Console.ReadLine();
        }
    }
}
