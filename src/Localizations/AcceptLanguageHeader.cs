using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace Localizations
{
    public sealed class AcceptLanguageHeader
    {
        private const string RegexGroup_locale = "locale";
        private const string RegexGroup_quality = "quality";
        private static readonly Regex HeaderRegex = new Regex(@"(?'locale'[a-z]{1,8}(-[a-z]{1,8})?)\s*(;\s*q\s*=\s*(?'quality'1|0\.[0-9]+))?", RegexOptions.IgnoreCase);

        public ReadOnlyCollection<string> Locales { get; private set; }

        public AcceptLanguageHeader(string header)
        {
            MatchCollection matchCollection = HeaderRegex.Matches(header);

            var locales = new List<KeyValuePair<string, decimal>>();

            foreach (Match match in matchCollection)
            {
                var localeGroup = match.Groups[RegexGroup_locale];
                if (localeGroup.Success)
                {
                    string locale = localeGroup.Value;
                    decimal quality = 1m;

                    Group qualityGroup = match.Groups[RegexGroup_quality];
                    if (qualityGroup.Success)
                        decimal.TryParse(qualityGroup.Value, out quality);

                    locales.Add(new KeyValuePair<string, decimal>(locale, quality));
                }
            }

            Locales = locales.OrderByDescending(x => x.Value).Select(x => x.Key).ToList().AsReadOnly();
        }
    }
}
