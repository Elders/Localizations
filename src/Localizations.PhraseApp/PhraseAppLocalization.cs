using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Localizations.PhraseApp.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Localizations.PhraseApp
{
    public class PhraseAppLocalization : ILocalization
    {
        private readonly HttpClient client;

        private readonly JsonSerializerOptions jsonSerializerOptions;

        private readonly ILogger<PhraseAppLocalization> log;
        private readonly PhraseAppLocalizationCache cache;
        private string localesEtag;

        PhraseAppOptions options;

        public PhraseAppLocalization(HttpClient client, IOptionsMonitor<PhraseAppOptions> optionsMonitor, ILogger<PhraseAppLocalization> log, PhraseAppLocalizationCache cache)
        {
            this.client = client;
            this.options = optionsMonitor.CurrentValue;
            optionsMonitor.OnChange(Changed);
            this.log = log;
            this.cache = cache;
            this.jsonSerializerOptions = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
        }

        private void Changed(PhraseAppOptions newOptions)
        {
            if (options != newOptions)
            {
                options = newOptions;
                //optionsHasChanged = true;
            }
        }

        /// <summary>
        /// Translations based on key and locale
        /// Depending of configuration can fallback and try with less restrictive locales e.g zh-hk-hans to zh-hk to zh
        /// Depending of configuration can fallback specified DefaultLocale
        /// </summary>
        /// <param name="key"></param>
        /// <param name="locale"></param>
        /// <returns>Returns translation by key and locale</returns>
        public async Task<SafeGet<TranslationModel>> GetAsync(string key, string locale)
        {
            if (string.IsNullOrEmpty(key) == true) throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrEmpty(locale) == true) throw new ArgumentNullException(nameof(locale));

            List<SafeGet<TranslationModel>> translations = await GetAllAsync(locale).ConfigureAwait(false);
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
        public async Task<List<SafeGet<TranslationModel>>> GetAllAsync(string locale)
        {
            if (string.IsNullOrEmpty(locale) == true) throw new ArgumentNullException(nameof(locale));
            var sanitizedLocaleName = new SanitizedLocaleName(locale);

            await CacheLocalesAndTranslationsAsync().ConfigureAwait(false);

            if (cache.TranslationCachePerLocale.TryGetValue(sanitizedLocaleName, out ConcurrentDictionary<string, TranslationModel> translationsForLocale))
                return new List<SafeGet<TranslationModel>>(translationsForLocale.Values.Select(x => new SafeGet<TranslationModel>(x)));

            if (options.UseStrictLocale == false && sanitizedLocaleName.Value.Contains(SanitizedLocaleName.LocaleSeparator) == true)
            {
                var next = sanitizedLocaleName.Value.Remove(sanitizedLocaleName.Value.LastIndexOf(SanitizedLocaleName.LocaleSeparator));
                return await GetAllAsync(next).ConfigureAwait(false);
            }

            if (ShouldFallbackToDefaultLocale(sanitizedLocaleName))
                return await GetAllAsync(options.DefaultLocale).ConfigureAwait(false);

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
        public async Task<SafeGet<TranslationModel>> GetAsync(string key, AcceptLanguageHeader header)
        {
            if (string.IsNullOrEmpty(key) == true) throw new ArgumentNullException(nameof(key));
            if (header is null) throw new ArgumentNullException(nameof(header));

            foreach (var locale in header.Locales)
            {
                var translationModel = await GetAsync(key, locale).ConfigureAwait(false);
                if (translationModel.Found)
                    return translationModel;
            }

            if (ShouldFallbackToDefaultLocale(header))
                return await GetAsync(key, options.DefaultLocale).ConfigureAwait(false);

            return SafeGet<TranslationModel>.NotFound;
        }

        /// <summary>
        /// Translations based on locale
        /// Depending of configuration can fallback and try with less restrictive locales e.g zh-hk-hans to zh-hk to zh
        /// Depending of configuration can fallback specified DefaultLocale
        /// </summary>
        /// <param name="header">>The Accept-Language header that will be used to get the translation.</param>
        /// <returns>The resulting translations for this <paramref name="header"/>. If no translations are not found for this <paramref name="header"/> the collection will be empty.</returns>
        public async Task<List<SafeGet<TranslationModel>>> GetAllAsync(AcceptLanguageHeader header)
        {
            if (header is null) throw new ArgumentNullException(nameof(header));

            foreach (var locale in header.Locales)
            {
                var translations = await GetAllAsync(locale).ConfigureAwait(false);
                if (translations.Count > 0)
                    return translations;
            }

            if (ShouldFallbackToDefaultLocale(header))
                return await GetAllAsync(options.DefaultLocale).ConfigureAwait(false);

            return new List<SafeGet<TranslationModel>>();
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
            return string.IsNullOrEmpty(options.DefaultLocale) == false && header.Locales.Any(x => x.Equals(options.DefaultLocale, StringComparison.OrdinalIgnoreCase)) == false;
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
            return string.IsNullOrEmpty(options.DefaultLocale) == false && options.DefaultLocale.Equals(currentLocale, StringComparison.OrdinalIgnoreCase) == false;
        }

        async Task CacheTranslationsAsync()
        {
            foreach (var sanitizedLocale in cache.LocaleCache.Values)
            {
                var resource = $"projects/{options.ProjectId}/locales/{sanitizedLocale.Id}/download?file_format=simple_json";
                //IRestResponse<Dictionary<string, string>> response = null;
                HttpResponseMessage response = null;

                if (cache.EtagPerLocaleCache.TryGetValue(sanitizedLocale.Name, out string currentLocaleEtag))
                    //response = client.Execute<Dictionary<string, string>>(CreateRestRequest(resource, Method.GET, currentLocaleEtag));
                    response = await client.SendAsync(CreateRestRequest(resource, HttpMethod.Get, currentLocaleEtag)).ConfigureAwait(false);
                else
                    //response = client.Execute<Dictionary<string, string>>(CreateRestRequest(resource, Method.GET));
                    response = await client.SendAsync(CreateRestRequest(resource, HttpMethod.Get)).ConfigureAwait(false);

                if (response is null)
                {
                    log.LogWarning($"Initialization for locale {sanitizedLocale.Name} with id {sanitizedLocale.Id} failed. Response was null");
                    continue;
                }

                if (response.IsSuccessStatusCode == false)
                {
                    log.LogWarning($"Initialization for locale {sanitizedLocale.Name} with id {sanitizedLocale.Id} failed. Response status was {response.StatusCode}");
                    continue;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    log.LogError($"Initialization for locale {sanitizedLocale.Name} with id {sanitizedLocale.Id} failed. Response status code is Unauthorized");
                    break;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
                    continue;

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Dictionary<string, string> data = JsonSerializer.Deserialize<Dictionary<string, string>>(json, jsonSerializerOptions);

                    var cacheForSpecificLocale = new ConcurrentDictionary<string, TranslationModel>();
                    var lastModified = GetLastModifiedFromHeadersAsFileTimeUtc(response);
                    foreach (var translation in data)
                    {
                        var model = new TranslationModel(translation.Key, translation.Value, sanitizedLocale.Name.Value, lastModified);
                        cacheForSpecificLocale.AddOrUpdate(translation.Key, model, (k, v) => model);
                    }

                    if (TryGetEtagValueFromHeaders(response, out string localeEtagFromHeader))
                        cache.EtagPerLocaleCache.AddOrUpdate(sanitizedLocale.Name, currentLocaleEtag, (k, v) => localeEtagFromHeader);

                    cache.TranslationCachePerLocale.AddOrUpdate(sanitizedLocale.Name, cacheForSpecificLocale, (k, v) => cacheForSpecificLocale);
                }

                log.LogWarning($"Initialization for locale {sanitizedLocale.Name} with id {sanitizedLocale.Id} failed.");
            }

            cache.NextCheckForChanges = DateTime.UtcNow.AddMinutes(options.TtlInMinutes);
        }

        public async Task CacheLocalesAsync()
        {
            var resource = $"projects/{options.ProjectId}/locales";
            var request = CreateRestRequest(resource, HttpMethod.Get, localesEtag);

            // var response = client.Execute<List<PhraseAppLocaleModel>>(request);
            var response = await client.SendAsync(request).ConfigureAwait(false);

            if (response is null)
            {
                log.LogWarning($"Unable to load locales for project {options.ProjectId}");
                return;
            }

            if (response.IsSuccessStatusCode == false)
            {
                log.LogWarning($"Initialization locales for project {options.ProjectId} failed. Response status was {response.StatusCode}");
                return;
            }

            CalculateNextRequestTimestamp(response);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                log.LogError($"Unable to load locales for project {options.ProjectId}. Response status code is Unauthorized.");
                return;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
                return;

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                if (TryGetEtagValueFromHeaders(response, out string etag))
                    localesEtag = etag;

                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                List<PhraseAppLocaleModel> data = JsonSerializer.Deserialize<List<PhraseAppLocaleModel>>(json, jsonSerializerOptions);

                foreach (PhraseAppLocaleModel locale in data)
                {
                    var sanitizedLocale = new SanitizedPhraseAppLocaleModel(locale.Id, new SanitizedLocaleName(locale.Name));
                    cache.LocaleCache.AddOrUpdate(sanitizedLocale.Id, sanitizedLocale, (k, v) => sanitizedLocale);
                }

                return;
            }

            log.LogWarning($"Unable to load locales for project {options.ProjectId}. Response status is {response.StatusCode}. Response status code is {response.StatusCode}. Error Message: {response.ReasonPhrase}");
        }

        public async Task CacheLocalesAndTranslationsAsync()
        {
            if (ShouldCheckForChanges() == true)
            {
                await CacheLocalesAsync().ConfigureAwait(false);
                await CacheTranslationsAsync().ConfigureAwait(false);
            }
        }

        bool ShouldCheckForChanges()
        {
            if (cache.NextCheckForChanges < DateTime.UtcNow)
                return true;

            return false;
        }

        void CalculateNextRequestTimestamp(HttpResponseMessage response)
        {
            int remainingRequests = GetHeaderValue<int>(response, "X-Rate-Limit-Remaining", 500);

            if (remainingRequests == 0)
            {
                log.LogWarning("[PhraseApp] Request limit exceeded (X-Rate-Limit-Remaining). https://phraseapp.com/docs/api/v2/#rate-limit");

                long headerTimeoutParameter = GetHeaderValue<long>(response, "X-Rate-Limit-Reset");
                if (headerTimeoutParameter > 0)
                    cache.NextCheckForChanges = ConvertTimestampFromParameterToDateTime(headerTimeoutParameter);
            }
        }

        T GetHeaderValue<T>(HttpResponseMessage response, string headerName, T defaultValue = default(T))
        {
            KeyValuePair<string, IEnumerable<string>> headerParam = response.Headers.Where(header => header.Key == headerName).SingleOrDefault();
            if (headerParam.Equals(default(KeyValuePair<string, IEnumerable<string>>)) == false)
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

        bool TryGetEtagValueFromHeaders(HttpResponseMessage response, out string etag)
        {
            etag = string.Empty;
            KeyValuePair<string, IEnumerable<string>> etagHeader = response.Headers.Where(x => x.Key == "ETag").SingleOrDefault();
            if (etagHeader.Equals(default(KeyValuePair<string, IEnumerable<string>>)) == false)
            {
                etag = etagHeader.Value.FirstOrDefault();
                return true;
            }

            return false;
        }

        long GetLastModifiedFromHeadersAsFileTimeUtc(HttpResponseMessage response)
        {
            KeyValuePair<string, IEnumerable<string>> lastModifiedHeader = response.Headers.Where(x => x.Key == "Last-Modified").SingleOrDefault();

            if (lastModifiedHeader.Equals(default(KeyValuePair<string, IEnumerable<string>>)) == false)
            {
                if (DateTime.TryParse(lastModifiedHeader.Value.FirstOrDefault(), out DateTime d) == true)
                {
                    return d.ToFileTimeUtc();
                }
            }

            return 0;
        }

        HttpRequestMessage CreateRestRequest(string resource, HttpMethod method)
        {
            return CreateRestRequest(resource, method, new List<KeyValuePair<string, string>>());
        }

        HttpRequestMessage CreateRestRequest(string resource, HttpMethod method, string eTagValue)
        {
            return CreateRestRequest(resource, method, new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("If-None-Match", eTagValue) });
        }

        HttpRequestMessage CreateRestRequest(string resource, HttpMethod method, List<KeyValuePair<string, string>> headers)
        {
            var request = new HttpRequestMessage(method, resource);

            foreach (var header in headers)
            {
                if (string.IsNullOrEmpty(header.Key) == true || string.IsNullOrEmpty(header.Value) == true)
                    continue;

                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return request;
        }
    }
}
