# Simple Localization abstractions
### Supports
- get by localization key and locale - `client.Get("some-key", "some-locale-like-en");`

## PhraseApp C# implementation (read only)
### Using
-  `PhraseApp API v2` [Documentation](https://phraseapp.com/docs/api/v2/)
-  In memory cache for locales and translations
-  Authorization with OAuth tokens

### Getting started

In order to start using this implementation you need to provide `accessToken`, `projectId` and `TTL`
- `accessToken` is used for authenticate against `PhraseApp` APIs. [Generate here](https://phraseapp.com/settings/oauth_access_tokens)
- `projectId` is used to navigate in `PhraseApp` APIs (in order to get locales and translations). You can find it by navigation to project settings in `PhraseApp` then selecting `API`
- `TTL` is the time span between syncing the cache with `PhraseApp`