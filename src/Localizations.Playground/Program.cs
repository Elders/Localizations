using Localizations.PhraseApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Localizations.Playground
{
    class Program
    {
        static async Task Main()
        {
            IServiceCollection services = new ServiceCollection();
            IConfiguration cfg = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            services.AddLogging();
            services.AddPhraseApp(cfg);
            services.AddSingleton<IConfiguration>(cfg);
            var serviceProvider = services.BuildServiceProvider();

            var opts = serviceProvider.GetRequiredService<IOptions<PhraseAppOptions>>();

            PhraseAppLocalization localization = serviceProvider.GetRequiredService<PhraseAppLocalization>();
            await localization.CacheLocalesAndTranslationsAsync();
            var byKey = await localization.GetAsync("1vipcustomer", "En");
            var byKeyWithHeader = await localization.GetAsync("1vipcustomer", new AcceptLanguageHeader("zh-Hant"));
            var getAll = await localization.GetAllAsync("eN");
            var getAllWithHeader = await localization.GetAllAsync(new AcceptLanguageHeader("EN"));
        }
    }
}
