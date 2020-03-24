using Localizations.PhraseApp.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Localizations.PhraseApp.Internal
{
    internal class PhraseAppOptionsProvider : OptionsProviderBase<PhraseAppOptions>
    {
        public const string Section = "localization:phraseapp";

        public PhraseAppOptionsProvider(IConfiguration configuration) : base(configuration) { }

        public override void Configure(PhraseAppOptions options)
        {
            options.Address = configuration[$"{Section}:address"] ?? "https://api.phraseapp.com/api/v2/";
            options.AccessToken = configuration[$"{Section}:accesstoken"];
            options.ProjectId = configuration[$"{Section}:projectid"];
            options.DefaultLocale = configuration[$"{Section}:defaultlocale"] ?? "en";
            options.UseStrictLocale = bool.Parse(configuration[$"{Section}:usestrictlocale"] ?? "false");
            options.TtlInMinutes = int.Parse(configuration[$"{Section}:ttlinminutes"] ?? "5");
        }
    }
}
