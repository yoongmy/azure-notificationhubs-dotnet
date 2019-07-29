using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Web;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.NotificationHubs.Auth
{
    /// <summary>A WCF SecurityToken that wraps a Simple Web Token.</summary>
    public class SimpleWebSecurityToken : SecurityToken
    {
        private static readonly Func<string, string> Decoder = new Func<string, string>(HttpUtility.UrlDecode);
        private static readonly DateTime EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private const string InternalExpiresOnFieldName = "ExpiresOn";
        private const string InternalAudienceFieldName = "Audience";
        private const string InternalKeyValueSeparator = "=";
        private const string InternalPairSeparator = "&";
        private readonly string _id;
        private readonly string _token;
        private readonly DateTime _validFrom;
        private readonly DateTime _validTo;
        private readonly string _audience;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microsoft.Azure.NotificationHubs.SimpleWebSecurityToken" /> class.
        /// </summary>
        /// <param name="tokenString">The token string.</param>
        /// <param name="expiry">The expiry.</param>
        /// <param name="audience">The audience.</param>
        /// <exception cref="T:System.NullReferenceException">
        /// tokenString
        /// or
        /// audience
        /// </exception>
        public SimpleWebSecurityToken(string tokenString, DateTime expiry, string audience)
        {
            _id = "uuid:" + Guid.NewGuid().ToString();
            _token = tokenString ?? throw new NullReferenceException(nameof(tokenString));
            _validFrom = DateTime.MinValue;
            _validTo = expiry;
            _audience = audience ?? throw new NullReferenceException(nameof(audience));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microsoft.Azure.NotificationHubs.SimpleWebSecurityToken" /> class.
        /// </summary>
        /// <param name="tokenString">The token string.</param>
        /// <param name="expiry">The expiry.</param>
        /// <exception cref="T:System.NullReferenceException">tokenString</exception>
        public SimpleWebSecurityToken(string tokenString, DateTime expiry)
        {
            _id = "uuid:" + Guid.NewGuid().ToString();
            _token = tokenString ?? throw new NullReferenceException(nameof(tokenString));
            _validFrom = DateTime.MinValue;
            _validTo = expiry;
            _audience = GetAudienceFromToken(tokenString);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microsoft.Azure.NotificationHubs.SimpleWebSecurityToken" /> class.
        /// </summary>
        /// <param name="tokenString">The token string.</param>
        public SimpleWebSecurityToken(string tokenString)
          : this("uuid:" + Guid.NewGuid().ToString(), tokenString)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microsoft.Azure.NotificationHubs.SimpleWebSecurityToken" /> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="tokenString">The token string.</param>
        /// <exception cref="T:System.NullReferenceException">
        /// id
        /// or
        /// tokenString
        /// </exception>
        public SimpleWebSecurityToken(string id, string tokenString)
        {
            _id = id ?? throw new NullReferenceException(nameof(id));
            _token = tokenString ?? throw new NullReferenceException(nameof(tokenString));
            GetExpirationDateAndAudienceFromToken(tokenString, out _validTo, out _audience);
        }

        /// <summary>Gets the audience for the simple web token.</summary>
        /// <returns>The audience for the simple web token.</returns>
        public string Audience
        {
            get
            {
                return _audience;
            }
        }

        /// <summary>
        /// Gets the date and time the security token will expire.
        /// </summary>
        /// <returns>The date and time the security token will expire.</returns>
        public DateTime ExpiresOn
        {
            get
            {
                return _validTo;
            }
        }

        /// <summary>
        /// Gets the field name associated with the token expiration.
        /// </summary>
        /// <returns>The field name associated with the token expiration.</returns>
        protected virtual string ExpiresOnFieldName
        {
            get
            {
                return "ExpiresOn";
            }
        }

        /// <summary>Gets the audience field name.</summary>
        /// <returns>The audience field name.</returns>
        protected virtual string AudienceFieldName
        {
            get
            {
                return "Audience";
            }
        }

        /// <summary>
        /// Gets the key value separator associated with the token.
        /// </summary>
        /// <returns>The key value separator associated with the token.</returns>
        protected virtual string KeyValueSeparator
        {
            get
            {
                return "=";
            }
        }

        /// <summary>Gets the pair separator associated with the token.</summary>
        /// <returns>The pair separator associated with the token.</returns>
        protected virtual string PairSeparator
        {
            get
            {
                return "&";
            }
        }

        /// <summary>Gets the ID associated with the Simple Web Token.</summary>
        /// <returns>The ID associated with the Simple Web Token.</returns>
        public override string Id
        {
            get
            {
                return _id;
            }
        }

        public override string Issuer { get; }
        public override SecurityKey SecurityKey { get; }
        public override SecurityKey SigningKey { get; set; }

        /// <summary>
        /// Gets the cryptographic keys associated with the security token.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1" /> of type <see cref="T:System.IdentityModel.Tokens.SecurityKey" /> that contains the set of keys associated with the Simple Web Token.
        /// </returns>
        //public override ReadOnlyCollection<SecurityKey> SecurityKeys
        //{
        //    get
        //    {
        //        return new ReadOnlyCollection<SecurityKey>((IList<SecurityKey>)new List<SecurityKey>());
        //    }
        //}

        /// <summary>Start validity time. Not implemented.</summary>
        public override DateTime ValidFrom
        {
            get
            {
                return _validFrom;
            }
        }

        /// <summary>End validity time. Not implemented.</summary>
        public override DateTime ValidTo
        {
            get
            {
                return _validTo;
            }
        }

        /// <summary>The raw token material.</summary>
        public string Token
        {
            get
            {
                return _token;
            }
        }

        private string GetAudienceFromToken(string token)
        {
            if (!Decode(token, Decoder, Decoder, KeyValueSeparator, PairSeparator)
                .TryGetValue(AudienceFieldName, out string str))
            {
                throw new FormatException(SRClient.TokenAudience);
            }
            return str;
        }

        private void GetExpirationDateAndAudienceFromToken(
          string token,
          out DateTime expiresOn,
          out string audience)
        {
            IDictionary<string, string> dictionary = Decode(token, Decoder, Decoder, KeyValueSeparator, PairSeparator);
            string s;
            if (!dictionary.TryGetValue(ExpiresOnFieldName, out s))
            {
                throw new FormatException(SRClient.TokenExpiresOn);
            }

            if (!dictionary.TryGetValue(AudienceFieldName, out audience))
            {
                throw new FormatException(SRClient.TokenAudience);
            }
            expiresOn = EpochTime + TimeSpan.FromSeconds(double.Parse(s, (IFormatProvider)CultureInfo.InvariantCulture));
        }

        private static IDictionary<string, string> Decode(
          string encodedString,
          Func<string, string> keyDecoder,
          Func<string, string> valueDecoder,
          string keyValueSeparator,
          string pairSeparator)
        {
            IDictionary<string, string> dictionary = (IDictionary<string, string>)new Dictionary<string, string>();
            foreach (string str in (IEnumerable<string>)encodedString.Split(new string[1] { pairSeparator }, StringSplitOptions.None))
            {
                string[] strArray = str.Split(new string[1] { keyValueSeparator }, StringSplitOptions.None);
                if (strArray.Length != 2)
                {
                    throw new FormatException(SRClient.InvalidEncoding);
                }
                dictionary.Add(keyDecoder(strArray[0]), valueDecoder(strArray[1]));
            }
            return dictionary;
        }
    }
}
