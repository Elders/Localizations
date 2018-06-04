﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
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

        public bool StrictLocale { get; private set; }

        public string DefaultLocale { get; private set; }

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

            // separator can be _ or -
            var replaced = locale.Replace("_", "-");
            if (StrictLocale == false && replaced.Contains("-") == true)
            {
                var next = replaced.Remove(replaced.LastIndexOf('-'));
                return Get(key, next);
            }

            if (string.IsNullOrEmpty(DefaultLocale) == false && DefaultLocale.Equals(locale, StringComparison.OrdinalIgnoreCase) == false)
                return Get(key, DefaultLocale);

            return SafeGet<TranslationModel>.NotFound;
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
            DefaultLocale = locale;
            return this;
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
                    var lastModified = GetLastModifiedFromHeadersAsFileTimeUtc(response);
                    foreach (var translation in response.Data)
                    {
                        var model = new TranslationModel(translation.Key, translation.Value, locale.Name, lastModified);
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
            var request = CreateRestRequest(resource, Method.GET, localeEtag);
            string requestLog = $"{Enum.GetName(typeof(Method), request.Method)} - {client.BaseUrl}{request.Resource}{Environment.NewLine}";

            var response = client.Execute<List<PhraseAppLocaleModel>>(request);

            if (ReferenceEquals(null, response) == true)
                log.Warn(() => $"Unable to load locales for project {projectId}");

            CalculateNextRequestTimestamp(response);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                localeEtag = GetEtagValueFromHeaders(response);
                foreach (var locale in response.Data)
                {
                    localeCache.AddOrUpdate(locale.Name, locale, (k, v) => locale);
                }
            }
            else
            {
                log.Warn(() => $"Unable to load locales for project {projectId}. Response status is {response.ResponseStatus}. Error Message: {response.ErrorMessage}");
            }
        }

        T GetHeaderValue<T>(IRestResponse response, string headerName, T defaultValue = default(T))
        {
            var headerParam = response.Headers.Where(header => header.Name == headerName).SingleOrDefault();
            if (ReferenceEquals(null, headerParam) == false)
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

        private static DateTime ConvertTimestampFromParameterToDateTime(long epoch)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epoch);
        }

        bool ShouldCheckForChanges()
        {
            if (nextCheckForChanges < DateTime.UtcNow)
                return true;

            return false;
        }

        string GetEtagValueFromHeaders(IRestResponse response)
        {
            var etagHeader = response.Headers.Where(x => x.Name == "ETag").SingleOrDefault();
            if (ReferenceEquals(null, etagHeader) == false)
                return etagHeader.Value.ToString();

            return string.Empty;
        }

        long GetLastModifiedFromHeadersAsFileTimeUtc(IRestResponse response)
        {
            var lastModifiedHeader = response.Headers.Where(x => x.Name == "Last-Modified").SingleOrDefault();

            if (ReferenceEquals(null, lastModifiedHeader) == false)
            {
                DateTime d;
                if (DateTime.TryParse(lastModifiedHeader.Value.ToString(), out d) == true)
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

        void LogRequest(IRestRequest request, string requestLog)
        {
            if (log.IsDebugEnabled())
            {
                requestLog += $"Parameters:{Environment.NewLine}" + string.Join($"{Environment.NewLine}", request.Parameters.Select(par => par.ToString()));
                log.Debug(requestLog);
            }
        }

        void LogResponse<T>(IRestResponse<T> response, string requestLog)
        {
            if (ReferenceEquals(null, response.ErrorException))
            {
                if (response.HasClientError() && log.IsWarnEnabled())
                    log.WarnException($"{requestLog} => {response.StatusCode}", new Exception(response.Content));
                else
                    log.Debug(() => $"{requestLog} => {response.StatusCode} {Environment.NewLine} {response.Content}");
            }
            else
            {
                log.ErrorException(requestLog, response.ErrorException);
            }
        }
    }
}
