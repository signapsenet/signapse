using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Signapse.Data;
using Signapse.RequestData;
using Signapse.Server.Common.Services;
using Signapse.Server.Middleware;
using Signapse.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Signapse.Middleware
{
    /// <summary>
    /// This is the data model for loading the index.html page.
    /// </summary>
    [DataFor("/index.html")]
    public class IndexData : AffiliateMustacheData
    {
        readonly AppConfig appConfig;

        public IndexData(AppConfig appConfig, IHttpContextAccessor acc, JsonDatabase<Data.User> db, AuthorizationFactory authFactory)
            :base(acc, db, authFactory)
        {
            this.appConfig = appConfig;

            if (acc.HttpContext is HttpContext context
                && appConfig.IsInstalled() == false)
            {
                throw new Exceptions.HttpRedirect("/install.html");
            }

            if (this.Auth.IsAdmin)
            {
                throw new Exceptions.HttpRedirect("/admin.html");
            }
        }

        public bool NeedToInstall => appConfig.IsInstalled() == false;
    }

    public class LoginRequestData : IWebRequest
    {
        public Guid ID { get; set; }
        public Uri? ServerUri { get; set; }

        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Process all requests from index.html
    /// </summary>
    public class IndexPageHandler
    {
        readonly JsonDatabase<Data.User> db;
        readonly PasswordHasher<Data.User> hasher;
        readonly IHttpContextAccessor contextAccessor;

        public IndexPageHandler(IHttpContextAccessor contextAccessor, JsonDatabase<Data.User> db, PasswordHasher<Data.User> hasher)
        {
            this.contextAccessor = contextAccessor;
            this.db = db;
            this.hasher = hasher;
        }

        public async Task ProcessLogin(WebRequest<LoginRequestData> request)
        {
            // TODO: Support embedded resources for streaming mustache content, so we can have common html
            // TODO: Make the login endpoint common in ServerBase, with an optional callback
            // to perform more specific tasks for each server type
            var ctx = contextAccessor.HttpContext ?? throw new Exception("Invalid Context");
            var user = request.Data ?? throw new Exceptions.HttpBadRequest("Invalid Request");

            var db = ctx.RequestServices.GetRequiredService<JsonDatabase<Data.User>>();

            var adminUser = db.Items
                .Where(it => it.Email == user.Email)
                .Where(it => hasher.VerifyHashedPassword(it, it.Password, user.Password) != PasswordVerificationResult.Failed)
                .FirstOrDefault();

            if (adminUser != null)
            {
                if (hasher.VerifyHashedPassword(adminUser, adminUser.Password, user.Password) == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    adminUser.RequireChangePassword = true;
                }

                await ctx.SignInAsync(AuthenticationSchemes.MemberCookie, Claims.CreatePrincipal(adminUser, AuthenticationSchemes.MemberCookie));
            }
            else
            {
                throw new Exceptions.HttpBadRequest("Invalid User or Password");
            }
        }

        // TODO: Remove this in favor of a common logout method
        public Task ProcessLogout()
        {
            var ctx = contextAccessor.HttpContext ?? throw new Exception("Invalid Context");
            return ctx.SignOutAsync();
        }
    }
}
