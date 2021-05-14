using System;
using System.Collections.Concurrent;
using Localizations.PhraseApp.Internal;

namespace Localizations.PhraseApp
{
    public class PhraseAppLocalizationCache
    {
        public PhraseAppLocalizationCache()
        {
            LocaleCache = new ConcurrentDictionary<string, SanitizedPhraseAppLocaleModel>();
            EtagPerLocaleCache = new ConcurrentDictionary<string, string>();
            TranslationCachePerLocale = new ConcurrentDictionary<string, ConcurrentDictionary<string, TranslationModel>>();
        }

        internal ConcurrentDictionary<string, SanitizedPhraseAppLocaleModel> LocaleCache { get; private set; }
        internal ConcurrentDictionary<string, ConcurrentDictionary<string, TranslationModel>> TranslationCachePerLocale { get; private set; }
        internal ConcurrentDictionary<string, string> EtagPerLocaleCache { get; private set; }

        internal DateTime NextCheckForChanges { get; set; }
    }
}
