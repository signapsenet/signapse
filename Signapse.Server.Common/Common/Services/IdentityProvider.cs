using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Signapse.Data;
using Signapse.Server.Middleware;
using Signapse.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;

namespace Signapse.Server.Common.Services
{
    public class IdentityProvider
    {
        readonly ContentProvider? contentProvider;
        readonly Transaction<SignapseServerDescriptor> affiliates;
        readonly RSASigner rsaSigner;

        public IdentityProvider(IServiceProvider provider, Transaction<SignapseServerDescriptor> affiliates,  RSASigner rsaSigner)
        {
            this.contentProvider = provider.GetService<ContentProvider>();
            this.affiliates = affiliates;
            this.rsaSigner = rsaSigner;
        }

        public bool IsValid(ClaimsPrincipal principal)
        {
            return principal.Identity?.IsAuthenticated == true
                && principal.SignapseUserID() != Guid.Empty
                && principal.ClaimValue(ClaimTypes.Expiration) is string expiry
                && DateTime.TryParse(expiry, out var dt)
                && dt > DateTime.UtcNow;
        }

        public ClaimsPrincipal CreatePrincipal(string authenticationType, Guid userID, string name)
            => CreatePrincipal(authenticationType, userID, name, config => { });

        public ClaimsPrincipal CreatePrincipal(string authenticationType, Guid userID, string name, Action<ClaimsIdentity> config)
        {
            var identity = new ClaimsIdentity(authenticationType, ClaimTypes.Name, ClaimTypes.Role);
            identity.AddClaim(new Claim(Claims.UserID, userID.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Name, name));
            identity.AddClaim(new Claim(ClaimTypes.Role, Roles.Member));
            identity.AddClaim(new Claim(ClaimTypes.Role, Roles.Consumer));
            identity.AddClaim(new Claim(ClaimTypes.Expiration, DateTime.Now.AddDays(7).ToString()));

            config(identity);

            return new ClaimsPrincipal(identity);
        }

        public string CreateJWT(string userID, string name, int expMinutes = 60 * 24 * 7)
        {
            var handler = new JsonWebTokenHandler();
            return handler.CreateToken(new SecurityTokenDescriptor()
            {
                Claims = new Dictionary<string, object>()
                {
                    [JwtRegisteredClaimNames.Sub] = userID,
                    [JwtRegisteredClaimNames.Name] = name,
                    [JwtRegisteredClaimNames.Exp] = DateTime.UtcNow.AddMinutes(expMinutes)
                },
                Expires = DateTime.UtcNow.AddMinutes(expMinutes),
                TokenType = "Bearer",
                SigningCredentials = rsaSigner.SigningCredentials
            });
        }

        public ClaimsPrincipal? ValidateAccessToken(string accessToken)
        {
            // Decode the JWT access token to get the issuer
            var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(accessToken);
            string issuer = jwt.Issuer;

            // Set up the JWT validation parameters
            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = issuer,
                ValidAudience = "signapse.affiliate",
                IssuerSigningKeyResolver = (s, securityToken, keyId, validationParameters) =>
                {
                    // Look up the public key for the given issuer
                    var publicKey = GetPublicKeyFromIssuer(securityToken.Issuer);

                    // Convert the public key from a string to an RSA key
                    var rsaKey = new RsaSecurityKey(publicKey);

                    // Return the RSA key
                    return new[] { rsaKey };
                },
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            // Validate the JWT access token
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            try
            {
                return handler.ValidateToken(accessToken, validationParameters, out var _);
            }
            catch (SecurityTokenException)
            {
            }
            return null;
        }

        RSAParameters GetPublicKeyFromIssuer(string issuer)
        {
            // If we are on a web server, the affiliates database has no entries and we need to get the
            // descriptors from the content provider.
            if (contentProvider != null)
            {
                return contentProvider.Affiliates
                    .Where(aff => aff.Uri.ToString().TrimEnd('/') == issuer.TrimEnd('/'))
                    .Select(aff => aff.RSAPublicKey.RSAParameters)
                    .FirstOrDefault();
            }
            else
            {
                return affiliates
                    .Where(aff => aff.WebServerUri.ToString().TrimEnd('/') == issuer.TrimEnd('/')
                        || aff.AffiliateServerUri.ToString().TrimEnd('/') == issuer.TrimEnd('/'))
                    .Select(aff => aff.RSAPublicKey.RSAParameters)
                    .FirstOrDefault();
            }
        }
    }
}
