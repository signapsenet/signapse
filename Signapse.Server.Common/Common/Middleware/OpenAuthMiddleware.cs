using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Signapse.Data;
using Signapse.Server.Common;
using Signapse.Server.Common.Services;
using Signapse.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Signapse.Server.Middleware
{
    internal class OpenAuthMiddleware
    {
        public OpenAuthMiddleware()
        {
        }

        private RSAParameters GetPublicKeyFromIssuer(HttpContext context, Transaction<SignapseServerDescriptor> affiliates, string issuer)
        {
            // If we are on a web server, the affiliates database has no entries and we need to get the
            // descriptors from the content provider.
            if (context.RequestServices.GetService<ContentProvider>() is ContentProvider contentProvider)
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

        /// <summary>
        /// Step 1.
        ///     If the user is not authorized for this endpoint, they go to the login page, then get redirect back to this endpoint after login
        ///     If a user is authorized for this endpoint, create the auth code and go back to the redirect uri.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task HandleAuthorize(HttpContext context, CookieAuthenticationHandler cookieHandler, IdentityProvider identityProvider)
        {
            var dataProtectionProvider = context.RequestServices.GetRequiredService<IDataProtectionProvider>();
            var server = context.RequestServices.GetRequiredService<ServerBase>();

            string query(string key)
                => context.Request.Query.TryGetValue(key, out var value) ? value.First() : string.Empty;

            var responseType = query("response_type");
            var redirectUri = query("redirect_uri");

            if (identityProvider.IsValid(context.User) == false)
            {
                await context.ChallengeAsync(AuthenticationSchemes.MemberCookie);
                return;
            }

            switch (responseType)
            {
                case "code":
                    {
                        var clientId = query("client_id");
                        var codeChallenge = query("code_challenge");
                        var codeChallengeMethod = query("code_challenge_method");
                        var scope = query("scope");
                        var state = query("state");

                        var protector = dataProtectionProvider.CreateProtector("oauth");
                        var authCode = new AuthCode()
                        {
                            ClientId = clientId,
                            CodeChallenge = codeChallenge,
                            CodeChallengeMethod = codeChallengeMethod.ToEnum<CodeChallengeMethod>(),
                            RedirectUri = redirectUri,
                            Expiry = DateTime.UtcNow.AddMinutes(5)
                        };

                        var code = protector.Protect(JsonSerializer.Serialize(authCode));

                        var url = QueryHelpers.AddQueryString(redirectUri, new Dictionary<string, string?>()
                        {
                            { "code", code },
                            { "state", state },
                            { "iss", new Uri(server.ServerUri, "/oauth/token").ToString() }
                        });
                        context.Response.Redirect(url);
                    }
                    break;

                case "token":
                    {
                        var state = query("state");

                        var url = QueryHelpers.AddQueryString(redirectUri, new Dictionary<string, string?>()
                        {
                            { "access_token", "" },
                            { "token_type", "Bearer" },
                            { "state", state },
                            { "expires_in", "86400" }
                        });
                        context.Response.Redirect(url);
                    }
                    break;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Step 2a. Verify the user's password, then redirect them to the callback url
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task HandleGetLogin(HttpContext context)
        {
            context.Request.Query.TryGetValue("ReturnUrl", out var returnUrl);

            await context.Response.WriteAsync($@"<!DOCTYPE html>
<html>
<head>
    <title>Login</title>
</head>
<body>
    <form method=""post"" action=""/oauth/login"">
        <label for=""email"">Email:</label>
        <input type=""text"" placeholder=""Email"" id=""email"" name=""email""><br>

        <label for=""password"">Password:</label>
        <input type=""password"" id=""password"" name=""password""><br>

        <input type=""hidden"" name=""returnUrl"" value=""{returnUrl}"" />

        <input type=""submit"" value=""Login"">
    </form>
</body>
</html>
");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Step 2b: Sign the user in and redirect to the callback uri
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task HandleLogin(HttpContext context, IdentityProvider identityProvider)
        {
            var rsaSigner = context.RequestServices.GetRequiredService<RSASigner>();

            if (context.Request.Form.TryGetValue("email", out var email)
                && context.Request.Form.TryGetValue("password", out var password)
                && context.Request.Form.TryGetValue("returnUrl", out var returnUrl))
            {
                var db = context.RequestServices.GetRequiredService<JsonDatabase<Member>>();
                var hasher = context.RequestServices.GetRequiredService<PasswordHasher<Member>>();

                var member = db.Items
                    .Where(it => it.Email == email.First())
                    .Where(it => hasher.VerifyHashedPassword(it, it.Password, password.First()) != PasswordVerificationResult.Failed)
                    .FirstOrDefault();

                if (member != null)
                {
                    if (hasher.VerifyHashedPassword(member, member.Password, member.Password) == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        member.RequireChangePassword = true;
                    }

                    var principal = identityProvider.CreatePrincipal(AuthenticationSchemes.MemberCookie, member.ID, member.Name);
                    await context.SignInAsync(AuthenticationSchemes.MemberCookie, principal);
                }
                else
                {
                    throw new Exceptions.HttpBadRequest("Invalid User or Password");
                }

                context.Response.Redirect(returnUrl);

                //                //context.Response.Redirect(returnUrl);
                //                await context.Response.WriteAsync($@"<!DOCTYPE html>
                //<html>
                //<head>
                //    <title>Login</title>
                //</head>
                //<body>
                //    <form method=""post"" action=""/oauth/login"">
                //        <input type=""hidden"" name=""returnUrl"" value=""{returnUrl}"" />

                //        <input type=""submit"" value=""Login"">
                //    </form>
                //</body>
                //</html>
                //");
            }
            else
            {
                throw new Exceptions.HttpBadRequest("Invalid Parameters");
            }
        }

        /// <summary>
        /// Step 3: After the user is authenticated on the target server, this callback contains the auth code
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task HandleCallback(HttpContext context, RSASigner rsaSigner, IdentityProvider identityProvider)
        {
            var dbAffiliate = context.RequestServices.GetRequiredService<JsonDatabase<SignapseServerDescriptor>>();

            string readQuery(string key)
                => context.Request.Query.TryGetValue(key, out var value) ? value.First() : string.Empty;

            string code = readQuery("code");
            string state = readQuery("state");
            string iss = readQuery("iss");

            await context.Response.WriteAsync($@"
<!DOCTYPE html>
<html>
    <head>
        <title>OAuth Callback</title>
        <script>window.top.postMessage({{ action: 'closeAuthFrame', args: {{ code: '{code}', state: '{state}', iss: '{iss}' }} }}, '*')</script>
    </head>
</html>
");

            //            var handler = new HttpClientHandler();
            //            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            //            handler.ClientCertificates.Add(rsaSigner.GenerateSelfSignedCertificate("localhost"));
            //#if DEBUG
            //            // We ignore SSL errors in debug mode, because I'm too lazy to figure it out right now
            //            handler.ServerCertificateCustomValidationCallback =
            //                (httpRequestMessage, cert, cetChain, policyErrors) =>
            //                {
            //                    return true;
            //                };
            //#endif

            //            using var httpClient = new HttpClient(handler);

            //            using var request = new HttpRequestMessage(HttpMethod.Post, iss);
            //            var tokenRequestParameters = new Dictionary<string, string>()
            //            {
            //                { "grant_type", "authorization_code" },
            //                { "code", code },
            //                { "code_verifier", "" },
            //                { "redirect_url", "" },
            //            };

            //            request.Content = new FormUrlEncodedContent(tokenRequestParameters);
            //            var response = await httpClient.SendAsync(request);
            //            var accessToken = await response.Content.ReadAsStringAsync();

            //            if (response.IsSuccessStatusCode
            //                && await response.Content.ReadFromJsonAsync<AccessToken>() is AccessToken token
            //                && identityProvider.ValidateAccessToken(token.access_token) is ClaimsPrincipal principal)
            //            {
            //                await context.SignInAsync(AuthenticationSchemes.ConsumerJWT, principal);
            //                await context.Response.WriteAsync(@"
            //<!DOCTYPE html>
            //<html>
            //    <head>
            //        <title>OAuth Callback</title>
            //        <script>window.top.postMessage({ action: 'closeAuthFrame' }, '*')</script>
            //    </head>
            //</html>
            //");
            //            }
            //            else
            //            {
            //                throw new Exceptions.HttpUnauthorized();
            //            }
        }

        /// <summary>
        /// Step 3: Generate a session token
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task HandleToken(HttpContext context)
        {
            var db = context.RequestServices.GetRequiredService<JsonDatabase<Data.SignapseServerDescriptor>>();
            var dataProtectionProvider = context.RequestServices.GetRequiredService<IDataProtectionProvider>();

            string readForm(string key)
                => context.Request.Form.TryGetValue(key, out var value) ? value.First() : string.Empty;

            var grantType = readForm("grant_type");

            if (context.User.Identity?.IsAuthenticated != true)
            {
                await context.ChallengeAsync(AuthenticationSchemes.MemberCookie);
                return;
            }


            switch (grantType)
            {
                case "client_credentials":
                    {
                        var appConfig = context.RequestServices.GetRequiredService<AppConfig>();
                        var clientId = readForm("client_id");
                        var secret = readForm("client_secret");
                        var scope = readForm("scope");

                        // TODO: Figure out a decent way to assign ID's to web servers
                        if (Guid.TryParse(clientId, out var guid)
                            //&& db[guid] is AffiliateDescriptor desc
                            && secret == appConfig.APIKey)
                        {
                            await WriteJWT(context, guid);
                        }
                        else
                        {
                            throw new Exceptions.HttpUnauthorized();
                        }
                    }
                    break;
                case "authorization_code":
                    {
                        var code = readForm("code");
                        var redirectUri = readForm("redirect_uri");
                        var codeVerifier = readForm("code_verifier");
                        var scope = readForm("scope");

                        var protector = dataProtectionProvider.CreateProtector("oauth");
                        AuthCode authCode = JsonSerializer.Deserialize<AuthCode>(protector.Unprotect(code))
                            ?? throw new Exceptions.HttpBadRequest("Invalid Auth Code");

                        authCode.Validate(code, codeVerifier);

                        await WriteJWT(context);
                    }
                    break;
                default:
                    throw new Exceptions.HttpBadRequest("Invalid Grant Type");
            }
        }

        /// <summary>
        /// Set up the authentication credentials for a jwt from another affiliate
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task HandlePreAuth(HttpContext context, Transaction<SignapseServerDescriptor> affiliates)
        {
            CookieAuthenticationOptions opt = context.RequestServices
                .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
                .Get(CookieAuthenticationDefaults.AuthenticationScheme)
                ?? throw new Exceptions.HttpUnauthorized();

            async Task<bool> checkQuery()
            {
                if (context.Request.Query.TryGetValue("access_token", out var value)
                    && value.FirstOrDefault() is string accessToken
                    && ValidateAccessToken(context, affiliates, accessToken) is ClaimsPrincipal principal)
                {
                    await context.SignInAsync(AuthenticationSchemes.ConsumerJWT, principal);
                    return true;
                }

                return false;
            }

            bool checkHeader()
            {
                return context.Request.Headers.Authorization.FirstOrDefault() is string auth
                    && auth.StartsWith("Bearer ")
                    && opt.TicketDataFormat.Unprotect(auth.Substring(7)) is AuthenticationTicket ticket
                    && ticket.Principal.Identity?.IsAuthenticated == true;
            }

            bool checkCookie()
            {
                return opt.Cookie.Name is string cookieName
                    && opt.CookieManager.GetRequestCookie(context, cookieName) is string cookieValue
                    && opt.TicketDataFormat.Unprotect(cookieValue) is AuthenticationTicket ticket
                    && ticket.Principal.Identity?.IsAuthenticated == true;
            }

            if (checkHeader() || await checkQuery() || checkCookie())
            {
                await context.Response.WriteAsJsonAsync(new
                {
                    result = "success"
                });
            }
            else
            {
                await context.SignOutAsync();

                throw new Exceptions.HttpUnauthorized();
            }
        }

        private ClaimsPrincipal? ValidateAccessToken(HttpContext context, Transaction<SignapseServerDescriptor> affiliates, string accessToken)
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
                    var publicKey = GetPublicKeyFromIssuer(context, affiliates, securityToken.Issuer);

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

        /// <summary>
        /// Create an authenticated token to an affiliate
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task HandleAuthenticate(HttpContext context, IdentityProvider identityProvider)
        {
            if (identityProvider.IsValid(context.User))
            {
                await context.SignInAsync(AuthenticationSchemes.MemberCookie, context.User);
            }
        }

        /// <summary>
        /// Verify the user's password, then redirect them to the callback url
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task HandleAccept(HttpContext context)
        {
            if (context.Request.Form.TryGetValue("returnUrl", out var returnUrl))
            {
                context.Response.Redirect(returnUrl);
            }
            else
            {
                throw new Exceptions.HttpBadRequest("Invalid Parameters");
            }

            await Task.CompletedTask;
        }

        public ClaimsPrincipal CreatePrincipal(Member member, string authenticationType)
        {
            var identity = new ClaimsIdentity(authenticationType, ClaimTypes.Name, ClaimTypes.Role);
            identity.AddClaim(new Claim(Claims.UserID, member.ID.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Name, member.Name));
            identity.AddClaim(new Claim(ClaimTypes.Role, Roles.Member));
            identity.AddClaim(new Claim(ClaimTypes.Role, Roles.Consumer));

            if (member.MemberTier != MemberTier.None)
            {
                identity.AddClaim(new Claim(Claims.MemberTier, member.MemberTier.ToString()));
            }

            return new ClaimsPrincipal(identity);
        }

        /// <summary>
        /// Return the current JWT (requires authorization)
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task HandleGetToken(HttpContext context)
        {
            await WriteJWT(context);
        }

        private AuthenticationTicket? GetTicket(HttpContext context)
        {
            CookieAuthenticationOptions opt = context.RequestServices
                .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
                .Get(CookieAuthenticationDefaults.AuthenticationScheme)
                ?? throw new Exceptions.HttpUnauthorized();

            if (opt.Cookie.Name is string cookieName
                && opt.CookieManager.GetRequestCookie(context, cookieName) is string cookieValue
                && opt.TicketDataFormat.Unprotect(cookieValue) is AuthenticationTicket ticket)
            {
                return ticket;
            }
            else
            {
                return null;
            }
        }

        public async Task WriteJWT(HttpContext context, Guid clientId)
        {
            var serverUri = context.RequestServices.GetRequiredService<ServerBase>().ServerUri;
            var rsaSigner = context.RequestServices.GetRequiredService<RSASigner>();

            var handler = new JsonWebTokenHandler();
            await context.Response.WriteAsJsonAsync(new
            {
                access_token = handler.CreateToken(new SecurityTokenDescriptor()
                {
                    Claims = new Dictionary<string, object>()
                    {
                        [JwtRegisteredClaimNames.Iss] = serverUri,
                        [JwtRegisteredClaimNames.Sub] = clientId.ToString(),
                        [JwtRegisteredClaimNames.Name] = clientId.ToString(),
                        [JwtRegisteredClaimNames.Aud] = "signapse.affiliate",
                    },
                    Expires = DateTime.UtcNow.AddMinutes(5),
                    TokenType = "Bearer",
                    SigningCredentials = rsaSigner.SigningCredentials
                }),
                token_type = "Bearer"
            });
        }

        public async Task WriteJWT(HttpContext context)
        {
            var serverUri = context.RequestServices.GetRequiredService<ServerBase>().ServerUri;
            var rsaSigner = context.RequestServices.GetRequiredService<RSASigner>();

            if (context.User.Identity is ClaimsIdentity identity
                && identity.HasClaim(c => c.Type == Claims.UserID)
                && !string.IsNullOrWhiteSpace(identity.Name))
            {
                var handler = new JsonWebTokenHandler();
                await context.Response.WriteAsJsonAsync(new AccessToken
                {
                    access_token = handler.CreateToken(new SecurityTokenDescriptor()
                    {
                        Claims = new Dictionary<string, object>()
                        {
                            [JwtRegisteredClaimNames.Iss] = serverUri,
                            [JwtRegisteredClaimNames.Sub] = identity.Claims.First(c => c.Type == Claims.UserID).Value,
                            [JwtRegisteredClaimNames.Name] = identity.Name,
                            [JwtRegisteredClaimNames.Aud] = "signapse.affiliate",
                        },
                        Expires = DateTime.UtcNow.AddMinutes(5),
                        TokenType = "Bearer",
                        SigningCredentials = rsaSigner.SigningCredentials
                    }),
                    token_type = "Bearer"
                });
            }
            else
            {
                throw new Exceptions.HttpUnauthorized();
            }
        }

        /// <summary>
        /// Attempt to create a new token that can be used for generating session tokens
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task HandleRefresh()
        {
            await Task.CompletedTask;
        }

        private X509Certificate2 GenerateSelfSignedCertificate(string subjectName)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest($"CN={subjectName}", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(365));
            return new X509Certificate2(certificate.Export(X509ContentType.Pfx));
        }
    }

    public static class OpenAuthMiddlewareExtensions
    {
        public static IServiceCollection AddSignapseOpenAuth(this IServiceCollection services)
        {
            services
                .AddScoped<IdentityProvider>()
                .AddAuthentication(AuthenticationSchemes.MemberCookie)
                .AddCookie(AuthenticationSchemes.MemberCookie, options =>
                {
                    options.LoginPath = "/oauth/login";
                    options.Cookie.SameSite = SameSiteMode.None;
                })
                // A member authentication indicates the user has been authenticated
                // from this site
                .AddJwtBearer(AuthenticationSchemes.MemberJWT, config =>
                {
                    // TODO: Configure member openauth authentication
                })
                // The basic user JWT indicates a user who's been authenticated
                // from an affiliate site
                .AddJwtBearer(AuthenticationSchemes.ConsumerJWT, config =>
                {
                    // TODO: Configure basic user openauth authentication
                });

            //services.AddScoped<IAuthorizationHandler, ResourceAuthorizationHandler>();
            services.AddAuthorization(config =>
            {
                // Policy for modifying account details
                config.AddPolicy(AuthenticationSchemes.MemberCookie, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(ClaimTypes.Role, Roles.Consumer);
                    policy.RequireClaim(ClaimTypes.Role, Roles.Member);
                });
                config.AddPolicy(AuthenticationSchemes.MemberJWT, policy =>
                {
                    policy.RequireClaim(ClaimTypes.Role, Roles.Consumer);
                    policy.RequireClaim(ClaimTypes.Role, Roles.Member);
                });

                // Policy for accessing resources
                config.AddPolicy(AuthenticationSchemes.ConsumerJWT, policy =>
                {
                    policy.RequireClaim(ClaimTypes.Role, Roles.Consumer);
                });

                config.DefaultPolicy = config.GetPolicy(AuthenticationSchemes.ConsumerJWT)
                    ?? throw new Exception("Policy Not Found");
            });

            return services;
        }

        public static WebApplication UseSignapseOpenAuth(this WebApplication app)
        {
            var oauth = new OpenAuthMiddleware();

            app.MapGet("/oauth/authenticate", oauth.HandleAuthenticate)
                .RequireAuthorization(AuthenticationSchemes.MemberJWT, AuthenticationSchemes.ConsumerJWT);

            // User methods
            app.MapGet("/oauth/authorize", oauth.HandleAuthorize)
                .RequireAuthorization(AuthenticationSchemes.MemberCookie);
            app.MapGet("/oauth/login", oauth.HandleGetLogin);
            app.MapPost("/oauth/login", oauth.HandleLogin);
            //app.MapPost("/oauth/accept", ctx => handle(ctx, oauth => oauth.HandleAccept()))
            //    .RequireAuthorization(AuthenticationSchemes.MemberCookie);

            // Remote site methods
            app.MapGet("/oauth/token", oauth.HandleGetToken)
                .RequireAuthorization(AuthenticationSchemes.MemberCookie);
            app.MapPost("/oauth/token", oauth.HandleToken);
            app.MapGet("/oauth/refresh", oauth.HandleRefresh);

            // Local server method
            app.MapGet("/oauth/callback", oauth.HandleCallback);
            app.MapGet("/oauth/preauth", oauth.HandlePreAuth);

            return app;
        }

        public static ClaimsPrincipal CreatePrincipal(Member member, string authenticationType)
        {
            var identity = new ClaimsIdentity(authenticationType, ClaimTypes.Name, ClaimTypes.Role);
            identity.AddClaim(new Claim(Claims.UserID, member.ID.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Name, member.Name));
            identity.AddClaim(new Claim(ClaimTypes.Role, Roles.Member));
            identity.AddClaim(new Claim(ClaimTypes.Role, Roles.Consumer));

            if (member.MemberTier != Signapse.Data.MemberTier.None)
            {
                identity.AddClaim(new Claim(Claims.MemberTier, member.MemberTier.ToString()));
            }

            return new ClaimsPrincipal(identity);
        }

        public static async Task WriteJWT(HttpContext context)
        {
            var rsaSigner = context.RequestServices.GetRequiredService<RSASigner>();

            if (context.User.Identity is ClaimsIdentity identity
                && identity.HasClaim(c => c.Type == Claims.UserID)
                && !string.IsNullOrWhiteSpace(identity.Name))
            {
                var handler = new JsonWebTokenHandler();
                await context.Response.WriteAsJsonAsync(new AccessToken()
                {
                    access_token = handler.CreateToken(new SecurityTokenDescriptor()
                    {
                        Claims = new Dictionary<string, object>()
                        {
                            [JwtRegisteredClaimNames.Sub] = identity.Claims.First(c => c.Type == Claims.UserID).Value,
                            [JwtRegisteredClaimNames.Name] = identity.Name,
                        },
                        Expires = DateTime.UtcNow.AddMinutes(5),
                        TokenType = "Bearer",
                        SigningCredentials = rsaSigner.SigningCredentials
                    }),
                    token_type = "Bearer"
                });
            }
        }
    }

    internal enum CodeChallengeMethod
    {
        Plain,
        S256
    }

    internal class AccessToken
    {
        public string access_token { get; set; } = string.Empty;
        public string token_type { get; set; } = string.Empty;
    }

    internal class AuthCode
    {
        private static readonly Dictionary<string, AuthCode> UsedCodes = new Dictionary<string, AuthCode>();

        public string ClientId { get; set; } = string.Empty;
        public string CodeChallenge { get; set; } = string.Empty;
        public CodeChallengeMethod CodeChallengeMethod { get; set; }
        public string RedirectUri { get; set; } = string.Empty;
        public DateTime Expiry { get; set; }

        public bool Validate(string code, string codeVerifier)
        {
            if (!string.IsNullOrWhiteSpace(this.CodeChallenge))
            {
                using var sha256 = SHA256.Create();
                var codeChallenge = Base64UrlEncoder.Encode(sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier)));
                if (codeChallenge != this.CodeChallenge)
                {
                    throw new Exceptions.HttpBadRequest("Invalid Challenge Code");
                }
            }

            if (this.Expiry < DateTime.UtcNow)
            {
                throw new Exceptions.HttpBadRequest("Expired Code");
            }

            lock (UsedCodes)
            {
                // Ensure this code can only be used once
                if (UsedCodes.TryGetValue(code, out var auth))
                {
                    throw new Exceptions.HttpBadRequest("Code Has Already Been Used");
                }
                UsedCodes.Add(code, this);

                // Cleanup expired codes
                foreach (var kv in UsedCodes.ToArray())
                {
                    if (kv.Value.Expiry < DateTime.UtcNow)
                    {
                        UsedCodes.Remove(kv.Key);
                    }
                }
            }

            return true;
        }
    }
}
