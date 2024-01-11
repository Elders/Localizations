using System.Threading.Tasks;

namespace Localizations
{
    public interface ILocalizationFactory
    {
        Task<ILocalization> GetLocalizationAsync(string tenant);
    }
}
