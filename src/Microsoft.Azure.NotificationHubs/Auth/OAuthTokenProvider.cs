using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Microsoft.Azure.NotificationHubs.Messaging;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.NotificationHubs.Auth
{
    /// <summary>Provides authentication token for the service bus.</summary>
    public class OAuthTokenProvider : TokenProvider
    {
        // private readonly Func<Uri, Uri> _onBuildUri = new Func<Uri, Uri>(BuildStsUri);
        private readonly List<Uri> _stsUris;
        private readonly NetworkCredential _credential;

        private const int DefaultCacheSize = 100;
        private const string OAuthTokenServicePath = "$STS/OAuth/";
        private const string ClientPasswordFormat = "grant_type=authorization_code&client_id={0}&client_secret={1}&scope={2}";

        internal OAuthTokenProvider(IEnumerable<Uri> stsUris, NetworkCredential credential)
          : base(true, true, 100, TokenScope.Namespace)
        {
            if (stsUris == null)
                throw new ArgumentNullException(nameof(stsUris));
            _stsUris = stsUris.ToList<Uri>();
            if (_stsUris.Count == 0)
                throw new ArgumentNullException(nameof(stsUris));
            _credential = credential ?? throw new ArgumentNullException(nameof(credential));
        }

        /// <summary>Retrieves a token when the provider begins.</summary>
        /// <returns>The asynchronous result of the operation.</returns>
        /// <param name="appliesTo">The provider in which the token will be applied.</param>
        /// <param name="action">The action.</param>
        /// <param name="timeout">The duration.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="state">The state of the operation.</param>
        //protected override IAsyncResult OnBeginGetToken(
        //  string appliesTo,
        //  string action,
        //  TimeSpan timeout,
        //  AsyncCallback callback,
        //  object state)
        //{
        //    DateTime expiresIn;
        //    string audience;
        //    SimpleWebSecurityToken webSecurityToken = new SimpleWebSecurityToken(TokenProviderHelper.GetOAuthAccessTokenCore((IEnumerator<Uri>)_stsUris.GetEnumerator(), _onBuildUri, BuildRequestToken(appliesTo), timeout, out expiresIn, out audience), expiresIn, audience);
        //    return (IAsyncResult)new CompletedAsyncResult<TokenProviderHelper.TokenResult<SecurityToken>>(new TokenProviderHelper.TokenResult<SecurityToken>()
        //    {
        //        CacheUntil = expiresIn,
        //        Token = (SecurityToken)webSecurityToken
        //    }, callback, state);
        //}

        /// <summary>Retrieves a web token when the provider begins.</summary>
        /// <returns>The asynchronous result of the operation.</returns>
        /// <param name="appliesTo">The provider in which the web token will be applied.</param>
        /// <param name="action">The action.</param>
        /// <param name="timeout">The duration.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="state">The state of the operation.</param>
        //protected override IAsyncResult OnBeginGetWebToken(
        //  string appliesTo,
        //  string action,
        //  TimeSpan timeout,
        //  AsyncCallback callback,
        //  object state)
        //{
        //    DateTime expiresIn;
        //    string audience;
        //    string str = string.Format((IFormatProvider)CultureInfo.InvariantCulture, "{0} {1}=\"{2}\"", (object)"WRAP", (object)"access_token", (object)TokenProviderHelper.GetOAuthAccessTokenCore((IEnumerator<Uri>)_stsUris.GetEnumerator(), _onBuildUri, BuildRequestToken(appliesTo), timeout, out expiresIn, out audience));
        //    return (IAsyncResult)new CompletedAsyncResult<TokenProviderHelper.TokenResult<string>>(new TokenProviderHelper.TokenResult<string>()
        //    {
        //        CacheUntil = expiresIn,
        //        Token = str
        //    }, callback, state);
        //}

        /// <summary>Retrieves a token when the provider ends.</summary>
        /// <returns>The retrieved token.</returns>
        /// <param name="result">The result of the operation.</param>
        /// <param name="cacheUntil">The duration for the provider to store data.</param>
        //protected override SecurityToken OnEndGetToken(
        //  IAsyncResult result,
        //  out DateTime cacheUntil)
        //{
        //    TokenProviderHelper.TokenResult<SecurityToken> tokenResult = CompletedAsyncResult<TokenProviderHelper.TokenResult<SecurityToken>>.End(result);
        //    cacheUntil = tokenResult.CacheUntil;
        //    return tokenResult.Token;
        //}

        /// <summary>Retrieves a web token when the provider ends.</summary>
        /// <returns>The retrieved token.</returns>
        /// <param name="result">The result of the operation.</param>
        /// <param name="cacheUntil">The duration for the provider to store data.</param>
        //protected override string OnEndGetWebToken(IAsyncResult result, out DateTime cacheUntil)
        //{
        //    TokenProviderHelper.TokenResult<string> tokenResult = CompletedAsyncResult<TokenProviderHelper.TokenResult<string>>.End(result);
        //    cacheUntil = tokenResult.CacheUntil;
        //    return tokenResult.Token;
        //}

        /// <summary>Builds a key for the provider.</summary>
        /// <returns>A Key.</returns>
        /// <param name="appliesTo">The provider in which the key will be applied.</param>
        /// <param name="action">The action.</param>
        //protected override TokenProvider.Key BuildKey(string appliesTo, string action)
        //{
        //    return new TokenProvider.Key(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "{0}\\{1}", (object)_credential.Domain, (object)_credential.UserName), string.Empty);
        //}

        /// <summary>Applies normalization into the token provider.</summary>
        /// <returns>The normalized token provider.</returns>
        /// <param name="appliesTo">The token provider where the normalization will be applied.</param>
        //protected override string NormalizeAppliesTo(string appliesTo)
        //{
        //    return ServiceBusUriHelper.NormalizeUri(appliesTo, "http", StripQueryParameters, false, false);
        //}

        //private string BuildRequestToken(string scope)
        //{
        //    string str;
        //    if (!string.IsNullOrWhiteSpace(_credential.Domain))
        //        str = string.Format((IFormatProvider)CultureInfo.InvariantCulture, "{0}@{1}", (object)_credential.UserName, (object)_credential.Domain);
        //    else
        //        str = _credential.UserName;
        //    return string.Format((IFormatProvider)CultureInfo.InvariantCulture, "grant_type=authorization_code&client_id={0}&client_secret={1}&scope={2}", (object)HttpUtility.UrlEncode(str), (object)HttpUtility.UrlEncode(_credential.Password), (object)HttpUtility.UrlEncode(scope));
        //}

        //private static Uri BuildStsUri(Uri baseAddress)
        //{
        //    UriBuilder httpsSchemeAndPort = MessagingUtilities.CreateUriBuilderWithHttpsSchemeAndPort(baseAddress);
        //    httpsSchemeAndPort.Path = string.Format((IFormatProvider)CultureInfo.InvariantCulture, "{0}{1}", (object)httpsSchemeAndPort.Path, (object)"$STS/OAuth/");
        //    return httpsSchemeAndPort.Uri;
        //}
    }
}
