using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace Localizations
{
    public class AcceptLanguageHeader
    {
        public ReadOnlyCollection<string> Locales { get; private set; }

        public AcceptLanguageHeader(string header)
        {
            var regex = new Regex(@"(?'locale'[a-z]{1,8}(-[a-z]{1,8})?)\s*(;\s*q\s*=\s*(?'quality'1|0\.[0-9]+))?", RegexOptions.IgnoreCase);

            var matchCollection = regex.Matches(header);

            var locales = new List<KeyValuePair<string, decimal>>();

            foreach (Match match in matchCollection)
            {
                var localeGroup = match.Groups["locale"];
                if (localeGroup.Success)
                {
                    var locale = localeGroup.Value;
                    var quality = 1m;

                    var qualityGroup = match.Groups["quality"];

                    if (qualityGroup.Success)
                    {
                        decimal.TryParse(qualityGroup.Value, out quality);
                    }

                    locales.Add(new KeyValuePair<string, decimal>(locale, quality));
                }
            }

            Locales = locales.OrderByDescending(x => x.Value).Select(x => x.Key).ToList().AsReadOnly();
        }
    }
}
