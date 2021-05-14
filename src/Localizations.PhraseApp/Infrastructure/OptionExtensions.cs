using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Localizations.PhraseApp.Infrastructure
{
    internal static class OptionExtensions
    {
        public static IServiceCollection AddOption<T, V>(this IServiceCollection services)
            where T : class, new()
            where V : OptionsProviderBase<T>
        {
            services.AddSingleton<IConfigureOptions<T>, V>();
            services.AddSingleton<IOptionsChangeTokenSource<T>, V>();
            services.AddSingleton<IOptionsFactory<T>, V>();

            return services;
        }
    }
}
