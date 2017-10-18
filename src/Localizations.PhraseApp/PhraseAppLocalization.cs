using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Localizations.Contracts;
using Localizations.PhraseApp.Logging;
using RestSharp;

namespace Localizations.PhraseApp
{
    public class PhraseAppLocalization : ILocalization
    {
        readonly ILog log;

        readonly IRestClient client;

        readonly ConcurrentDictionary<string, PhraseAppLocaleModel> localeCache;

        readonly ConcurrentDictionary<string, ConcurrentDictionary<string, TranslationModel>> translationCachePerLocale;

        readonly string accessToken;

        readonly string projectId;

        readonly TimeSpan ttl;

        DateTime nextCheckForChanges;

        string localeEtag;

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
            localeCache = new ConcurrentDictionary<string, PhraseAppLocaleModel>();
            log = LogProvider.GetLogger(typeof(PhraseAppLocalization));

            CacheLocales();
            CacheTranslations();
        }

        public SafeGet<TranslationModel> Get(string key, string locale)
        {
            if (ShouldCheckForChanges() == true)
            {
                CacheLocales();
                CacheTranslations();
            }

            ConcurrentDictionary<string, TranslationModel> translationsForLocale;
            if (translationCachePerLocale.TryGetValue(locale, out translationsForLocale))
            {
                TranslationModel translation;
                if (translationsForLocale.TryGetValue(key, out translation) == true)
                {
                    if (ReferenceEquals(null, translation) == false)
                        return new SafeGet<TranslationModel>(translation);
                }
            }
            return SafeGet<TranslationModel>.NotFound;
        }

        void CacheTranslations()
        {
            foreach (var locale in localeCache.Values)
            {
                try
                {
                    var resource = $"projects/{projectId}/locales/{locale.Id}/download?file_format=simple_json";
                    var response = client.Execute<Dictionary<string, string>>(CreateRestRequest(resource, Method.GET));

                    if (ReferenceEquals(null, response) == true)
                    {
                        log.Warn(() => $"Initialization for locale {locale.Name} with id {locale.Id} failed. Response was null");
                        continue;
                    }

                    if (response.ResponseStatus != ResponseStatus.Completed)
                    {
                        log.Warn(() => $"Initialization for locale {locale.Name} with id {locale.Id} failed. Response status was {response.ResponseStatus}");
                        continue;
                    }

                    var cacheForSpecificLocale = new ConcurrentDictionary<string, TranslationModel>();
                    foreach (var translation in response.Data)
                    {
                        var model = new TranslationModel(translation.Key, translation.Value, locale.Name);
                        cacheForSpecificLocale.AddOrUpdate(translation.Key, model, (k, v) => model);
                    }

                    translationCachePerLocale.AddOrUpdate(locale.Name, cacheForSpecificLocale, (k, v) => cacheForSpecificLocale);
                }
                catch (Exception ex)
                {
                    log.ErrorException($"Initialization for locale {locale.Name} with id {locale.Id} failed.", ex);
                }
            }

            nextCheckForChanges = DateTime.UtcNow.Add(ttl);
        }

        void CacheLocales()
        {
            var resource = $"projects/{projectId}/locales";
            var response = client.Execute<List<PhraseAppLocaleModel>>(CreateRestRequest(resource, Method.GET, localeEtag));

            if (ReferenceEquals(null, response) == true)
            {
                log.Warn(() => $"Unable to load locales for project {projectId}");
            }

            if (response.ResponseStatus != ResponseStatus.Completed)
            {
                log.Warn(() => $"Unable to load locales for project {projectId}. Response status is {response.ResponseStatus}. Error Message: {response.ErrorMessage}");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
                return;

            localeEtag = GetEtagValueFromHeaders(response);
            foreach (var locale in response.Data)
            {
                localeCache.AddOrUpdate(locale.Name, locale, (k, v) => locale);
            }
        }

        bool ShouldCheckForChanges()
        {
            if (nextCheckForChanges < DateTime.UtcNow)
                return true;

            return false;
        }

        string GetEtagValueFromHeaders(IRestResponse response)
        {
            var etag = response.Headers.Where(x => x.Name == "ETag").SingleOrDefault();
            if (ReferenceEquals(null, etag) == false)
                return etag.Value.ToString();

            return string.Empty;
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
            var request = new RestRequest(resource, method);
            request.RequestFormat = DataFormat.Json;
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
