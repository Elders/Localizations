using Localizations.PhraseApp.Infrastructure;
using Localizations.PhraseApp.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Localizations.PhraseApp
{
    public static class PhraseAppServiceCollectionExtensions
    {
        public static IServiceCollection AddPhraseApp(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();
            services.AddOption<PhraseAppOptions, PhraseAppOptionsProvider>();
            services.AddSingleton<PhraseAppLocalizationCache>();// Hey-yo

            var options = new PhraseAppOptions();
            configuration.GetSection(PhraseAppOptionsProvider.Section).Bind(options);
            services.AddHttpClient<ILocalization, PhraseAppLocalization>(client =>
            {
                client.BaseAddress = new Uri(options.Address);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"token {options.AccessToken}");
            });

            services.AddHttpClient<PhraseAppLocalization, PhraseAppLocalization>(client =>
            {
                client.BaseAddress = new Uri(options.Address);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"token {options.AccessToken}");
            });

            return services;
        }
    }
}
