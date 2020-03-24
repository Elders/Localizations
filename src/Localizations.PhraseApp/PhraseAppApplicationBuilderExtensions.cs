using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Localizations.PhraseApp
{
    public static class PhraseAppApplicationBuilderExtensions
    {
        public static async Task<IApplicationBuilder> UsePhraseApp(this IApplicationBuilder app)
        {
            PhraseAppLocalization localization = app.ApplicationServices.GetRequiredService<PhraseAppLocalization>();

            await localization.CacheLocalesAndTranslationsAsync().ConfigureAwait(false);

            return app;
        }
    }
}
