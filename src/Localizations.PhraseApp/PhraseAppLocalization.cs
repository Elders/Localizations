using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Localizations.PhraseApp.Logging;
using RestSharp;

namespace Localizations.PhraseApp
{
    public class PhraseAppLocalization : ILocalization
    {
        static readonly ILog log = LogProvider.GetLogger(typeof(PhraseAppLocalization));

        readonly IRestClient client;

        readonly ConcurrentDictionary<string, SanitizedPhraseAppLocaleModel> localeCache;

        readonly ConcurrentDictionary<string, ConcurrentDictionary<string, TranslationModel>> translationCachePerLocale;

        readonly ConcurrentDictionary<string, string> etagPerLocaleCache;

        readonly string accessToken;

        readonly string projectId;

        readonly TimeSpan ttl;

        DateTime nextCheckForChanges;

        string localesEtag;

        public bool StrictLocale { get; private set; }

        public SanitizedLocaleName DefaultLocale { get; private set; }

        public PhraseAppLocalization(string accessToken, string projectId, TimeSpan ttl)
        {
            if (string.IsNullOrEmpty(accessToken) == true) throw new ArgumentNullException(nameof(accessToken));
            if (string.IsNullOrEmpty(projectId) == true) throw new ArgumentNullException(nameof(projectId));
            if (ReferenceEquals(null, ttl) == true) throw new ArgumentNullException(nameof(ttl));

            this.accessToken = accessToken;
            this.projectId = projectId;
            this.ttl = ttl;

            client = new RestClient(PhraseAppConstants.BaseUrl);
            translationCachePerLocale = new ConcurrentDictionary<string, ConcurrentDictionary<string, TranslationModel>>();
            localeCache = new ConcurrentDictionary<string, SanitizedPhraseAppLocaleModel>();
            etagPerLocaleCache = new ConcurrentDictionary<string, string>();

            CacheLocales();
            CacheTranslations();
        }

        /// <summary>
        /// Translations based on key and locale
        /// Depending of configuration can fallback and try with less restrictive locales e.g zh-hk-hans to zh-hk to zh
        /// Depending of configuration can fallback specified DefaultLocale
        /// </summary>
        /// <param name="key"></param>
        /// <param name="locale"></param>
        /// <returns>Returns translation by key and locale</returns>
        public SafeGet<TranslationModel> Get(string key, string locale)
        {
            if (string.IsNullOrEmpty(key) == true) throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrEmpty(locale) == true) throw new ArgumentNullException(nameof(locale));

            List<SafeGet<TranslationModel>> translations = GetAll(locale);
            SafeGet<TranslationModel> translation = translations.SingleOrDefault(x => x.Result().Key.Equals(key, StringComparison.OrdinalIgnoreCase));

            if (translation is null)
                return SafeGet<TranslationModel>.NotFound;

            return translation;
        }

        /// <summary>
        /// Translations based on locale
        /// Depending of configuration can fallback and try with less restrictive locales e.g zh-hk-hans to zh-hk to zh
        /// Depending of configuration can fallback specified DefaultLocale
        /// </summary>
        /// <param name="locale"></param>
        /// <returns>Returns all the translations by locale</returns>
        public List<SafeGet<TranslationModel>> GetAll(string locale)
        {
            if (string.IsNullOrEmpty(locale) == true) throw new ArgumentNullException(nameof(locale));
            var sanitizedLocaleName = new SanitizedLocaleName(locale);

            CacheLocalesAndTranslations();

            if (translationCachePerLocale.TryGetValue(sanitizedLocaleName, out ConcurrentDictionary<string, TranslationModel> translationsForLocale))
                return new List<SafeGet<TranslationModel>>(translationsForLocale.Values.Select(x => new SafeGet<TranslationModel>(x)));

            if (StrictLocale == false && sanitizedLocaleName.Value.Contains(SanitizedLocaleName.LocaleSeparator) == true)
            {
                var next = sanitizedLocaleName.Value.Remove(sanitizedLocaleName.Value.LastIndexOf(SanitizedLocaleName.LocaleSeparator));
                return GetAll(next);
            }

            if (ShouldFallbackToDefaultLocale(sanitizedLocaleName))
                return GetAll(DefaultLocale);

            return new List<SafeGet<TranslationModel>>();
        }

        /// <summary>
        /// Translations based on key and locale
        /// Depending of configuration can fallback and try with less restrictive locales e.g zh-hk-hans to zh-hk to zh
        /// Depending of configuration can fallback specified DefaultLocale
        /// </summary>
        /// <param name="key">The translation key.</param>
        /// <param name="header">The Accept-Language header that will be used to get the translation.</param>
        /// <returns>The resulting translation for this <paramref name="header"/>. If no translation is not found for this <paramref name="header"/> the result will be "missing-key-'{<paramref name="key"/>}'".</returns>
        public SafeGet<TranslationModel> Get(string key, AcceptLanguageHeader header)
        {
            if (string.IsNullOrEmpty(key) == true) throw new ArgumentNullException(nameof(key));
            if (header is null) throw new ArgumentNullException(nameof(header));

            foreach (var locale in header.Locales)
            {
                var translationModel = Get(key, locale);
                if (translationModel.Found)
                    return translationModel;
            }

            if (ShouldFallbackToDefaultLocale(header))
                return Get(key, DefaultLocale);

            return SafeGet<TranslationModel>.NotFound;
        }

        /// <summary>
        /// Translations based on locale
        /// Depending of configuration can fallback and try with less restrictive locales e.g zh-hk-hans to zh-hk to zh
        /// Depending of configuration can fallback specified DefaultLocale
        /// </summary>
        /// <param name="header">>The Accept-Language header that will be used to get the translation.</param>
        /// <returns>The resulting translations for this <paramref name="header"/>. If no translations are not found for this <paramref name="header"/> the collection will be empty.</returns>
        public List<SafeGet<TranslationModel>> GetAll(AcceptLanguageHeader header)
        {
            if (header is null) throw new ArgumentNullException(nameof(header));

            foreach (var locale in header.Locales)
            {
                var translations = GetAll(locale);
                if (translations.Count > 0)
                    return translations;
            }

            if (ShouldFallbackToDefaultLocale(header))
                return GetAll(DefaultLocale);

            return new List<SafeGet<TranslationModel>>();
        }

        /// <summary>
        /// Specifies if fall back to two letter part of locale is allowed e.g en-GB would fall back to en
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public PhraseAppLocalization UseStrictLocale(bool value)
        {
            StrictLocale = value;
            return this;
        }

        /// <summary>
        /// Specifies default fall back locale
        /// </summary>
        /// <param name="locale"></param>
        /// <returns></returns>
        public PhraseAppLocalization UseDefaultLocale(string locale)
        {
            DefaultLocale = new SanitizedLocaleName(locale);
            return this;
        }

        /// <summary>
        /// Attempts to use fall-back strategy by using DefaultLocale
        /// Checks if default locale is defined.
        /// Also checks if it is different than the locales we have already tried
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        bool ShouldFallbackToDefaultLocale(AcceptLanguageHeader header)
        {
            return string.IsNullOrEmpty(DefaultLocale) == false && header.Locales.Any(x => x.Equals(DefaultLocale, StringComparison.OrdinalIgnoreCase)) == false;
        }

        /// <summary>
        /// Attempts to use fall-back strategy by using DefaultLocale
        /// Checks if default locale is defined.
        /// Also checks if the passed locale is different than the DefaultLocale
        /// </summary>
        /// <param name="currentLocale"></param>
        /// <returns></returns>
        bool ShouldFallbackToDefaultLocale(string currentLocale)
        {
            return string.IsNullOrEmpty(DefaultLocale) == false && DefaultLocale.Value.Equals(currentLocale, StringComparison.OrdinalIgnoreCase) == false;
        }

        void CacheTranslations()
        {
            foreach (var sanitizedLocale in localeCache.Values)
            {
                try
                {
                    var resource = $"projects/{projectId}/locales/{sanitizedLocale.Id}/download?file_format=simple_json";
                    IRestResponse<Dictionary<string, string>> response = null;

                    if (etagPerLocaleCache.TryGetValue(sanitizedLocale.Name, out string currentLocaleEtag))
                        response = client.Execute<Dictionary<string, string>>(CreateRestRequest(resource, Method.GET, currentLocaleEtag));
                    else
                        response = client.Execute<Dictionary<string, string>>(CreateRestRequest(resource, Method.GET));

                    if (response is null)
                    {
                        log.Warn(() => $"Initialization for locale {sanitizedLocale.Name} with id {sanitizedLocale.Id} failed. Response was null");
                        continue;
                    }

                    if (response.ResponseStatus != ResponseStatus.Completed)
                    {
                        log.Warn(() => $"Initialization for locale {sanitizedLocale.Name} with id {sanitizedLocale.Id} failed. Response status was {response.ResponseStatus}");
                        continue;
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
                        continue;

                    var cacheForSpecificLocale = new ConcurrentDictionary<string, TranslationModel>();
                    var lastModified = GetLastModifiedFromHeadersAsFileTimeUtc(response);
                    foreach (var translation in response.Data)
                    {
                        var model = new TranslationModel(translation.Key, translation.Value, sanitizedLocale.Name.Value, lastModified);
                        cacheForSpecificLocale.AddOrUpdate(translation.Key, model, (k, v) => model);
                    }

                    if (TryGetEtagValueFromHeaders(response, out string localeEtagFromHeader))
                        etagPerLocaleCache.AddOrUpdate(sanitizedLocale.Name, currentLocaleEtag, (k, v) => localeEtagFromHeader);

                    translationCachePerLocale.AddOrUpdate(sanitizedLocale.Name, cacheForSpecificLocale, (k, v) => cacheForSpecificLocale);
                }
                catch (Exception ex)
                {
                    log.ErrorException($"Initialization for locale {sanitizedLocale.Name} with id {sanitizedLocale.Id} failed.", ex);
                }
            }

            nextCheckForChanges = DateTime.UtcNow.Add(ttl);
        }

        void CacheLocales()
        {
            var resource = $"projects/{projectId}/locales";
            var request = CreateRestRequest(resource, Method.GET, localesEtag);

            var response = client.Execute<List<PhraseAppLocaleModel>>(request);

            if (response is null)
            {
                log.Warn(() => $"Unable to load locales for project {projectId}");
                return;
            }

            if (response.ResponseStatus != ResponseStatus.Completed)
            {
                log.Warn(() => $"Initialization locales for project {projectId} failed. Response status was {response.ResponseStatus}");
                return;
            }

            CalculateNextRequestTimestamp(response);

            if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
                return;

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                if (TryGetEtagValueFromHeaders(response, out string etag))
                    localesEtag = etag;

                foreach (PhraseAppLocaleModel locale in response.Data)
                {
                    var sanitizedLocale = new SanitizedPhraseAppLocaleModel(locale.Id, new SanitizedLocaleName(locale.Name));
                    localeCache.AddOrUpdate(sanitizedLocale.Id, sanitizedLocale, (k, v) => sanitizedLocale);
                }

                return;
            }

            log.Warn(() => $"Unable to load locales for project {projectId}. Response status is {response.ResponseStatus}. Error Message: {response.ErrorMessage}");
        }

        void CacheLocalesAndTranslations()
        {
            if (ShouldCheckForChanges() == true)
            {
                CacheLocales();
                CacheTranslations();
            }
        }

        bool ShouldCheckForChanges()
        {
            if (nextCheckForChanges < DateTime.UtcNow)
                return true;

            return false;
        }

        void CalculateNextRequestTimestamp(IRestResponse response)
        {
            int remainingRequests = GetHeaderValue<int>(response, "X-Rate-Limit-Remaining", 500);

            if (remainingRequests == 0)
            {
                log.Warn("[PhraseApp] Request limit exceeded (X-Rate-Limit-Remaining). https://phraseapp.com/docs/api/v2/#rate-limit");

                long headerTimeoutParameter = GetHeaderValue<long>(response, "X-Rate-Limit-Reset");
                if (headerTimeoutParameter > 0)
                    nextCheckForChanges = ConvertTimestampFromParameterToDateTime(headerTimeoutParameter);
            }
        }

        T GetHeaderValue<T>(IRestResponse response, string headerName, T defaultValue = default(T))
        {
            var headerParam = response.Headers.Where(header => header.Name == headerName).SingleOrDefault();
            if (headerParam is null == false)
            {
                object value = headerParam.Value;
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter.IsValid(value))
                {
                    T converted = (T)converter.ConvertFrom(value);
                    return converted;
                }
            }

            if (defaultValue.Equals(default(T)) == false)
                return defaultValue;

            return default(T);
        }

        static DateTime ConvertTimestampFromParameterToDateTime(long epoch)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epoch);
        }

        bool TryGetEtagValueFromHeaders(IRestResponse response, out string etag)
        {
            etag = string.Empty;
            var etagHeader = response.Headers.Where(x => x.Name == "ETag").SingleOrDefault();
            if (etagHeader is null == false)
            {
                etag = etagHeader.Value.ToString();
                return true;
            }

            return false;
        }

        long GetLastModifiedFromHeadersAsFileTimeUtc(IRestResponse response)
        {
            var lastModifiedHeader = response.Headers.Where(x => x.Name == "Last-Modified").SingleOrDefault();

            if (lastModifiedHeader is null == false)
            {
                if (DateTime.TryParse(lastModifiedHeader.Value.ToString(), out DateTime d) == true)
                {
                    return d.ToFileTimeUtc();
                }
            }

            return 0;
        }

        IRestRequest CreateRestRequest(string resource, Method method)
        {
            return CreateRestRequest(resource, method, new List<KeyValuePair<string, string>>());
        }

        IRestRequest CreateRestRequest(string resource, Method method, string eTagValue)
        {
            return CreateRestRequest(resource, method, new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("If-None-Match", eTagValue) });
        }

        IRestRequest CreateRestRequest(string resource, Method method, List<KeyValuePair<string, string>> headers)
        {
            var request = new RestRequest(resource, method)
            {
                RequestFormat = DataFormat.Json
            };
            request.AddHeader("Authorization", $"token {accessToken}");

            foreach (var header in headers)
            {
                if (string.IsNullOrEmpty(header.Key) == true || string.IsNullOrEmpty(header.Value) == true)
                    continue;

                request.AddHeader(header.Key, header.Value);
            }

            return request;
        }
    }
}
