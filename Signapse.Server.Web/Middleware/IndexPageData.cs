using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Signapse.BlockChain.Transactions;
using Signapse.RequestData;
using Signapse.Server.Common;
using Signapse.Server.Common.Services;
using Signapse.Server.Middleware;
using Signapse.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Signapse.Middleware
{
    public class IndexPageData : MustacheData
    {
        private readonly IHttpContextAccessor contextAccessor;
        private readonly ContentProvider contentProvider;
        private readonly ServerBase server;
        private readonly JsonDatabase<Data.Member> db;
        private readonly PasswordHasher<Data.Member> hasher;

        public IndexPageData(IHttpContextAccessor contextAccessor, ContentProvider contentProvider,
            ServerBase server, JsonDatabase<Data.Member> db, PasswordHasher<Data.Member> hasher)
        {
            this.contextAccessor = contextAccessor;
            this.contentProvider = contentProvider;
            this.server = server;
            this.db = db;
            this.hasher = hasher;
        }


        public string ServerUri => server.ServerUri.ToString().TrimEnd('/');

        public bool IsAuthenticated => contextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

        public UserIdentity Identity => new UserIdentity(contextAccessor.HttpContext?.User);

        public ISignapseContent[] Posts => this.contentProvider
            .CurrentContent
            .ToArray();

        public AffiliateData[] Affiliates => this.contentProvider
            .Affiliates
            .Select(a => new AffiliateData(a))
            .ToArray();

        public async Task ProcessLogin(WebRequest<LoginRequestData> request)
        {
            var ctx = contextAccessor.HttpContext ?? throw new Exception("Invalid Context");
            var user = request.Data ?? throw new Exceptions.HttpBadRequest("Invalid Request");

            var db = ctx.RequestServices.GetRequiredService<JsonDatabase<Data.Member>>();

            var member = db.Items
                .Where(it => it.Email == user.Email)
                .Where(it => hasher.VerifyHashedPassword(it, it.Password, user.Password) != PasswordVerificationResult.Failed)
                .FirstOrDefault();

            if (member != null)
            {
                if (hasher.VerifyHashedPassword(member, member.Password, user.Password) == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    member.RequireChangePassword = true;
                }

                await ctx.SignInAsync(
                    AuthenticationSchemes.MemberCookie,
                    OpenAuthMiddlewareExtensions.CreatePrincipal(member, AuthenticationSchemes.MemberCookie));

                await OpenAuthMiddlewareExtensions.WriteJWT(ctx);
            }
            else
            {
                throw new Exceptions.HttpBadRequest("Invalid User or Password");
            }
        }

        public Task ProcessLogout()
        {
            var ctx = contextAccessor.HttpContext ?? throw new Exception("Invalid Context");
            return ctx.SignOutAsync();
        }
    }

    public class UserIdentity
    {
        public Guid UserID { get; set; }
        public string Name { get; set; } = string.Empty;

        public UserIdentity(ClaimsPrincipal? principle)
        {
            if (principle != null)
            {
                this.UserID = principle.SignapseUserID();
                this.Name = principle.Identity?.Name ?? string.Empty;
            }
        }
    }

    public class AffiliateData
    {
        public string Title { get; }
        public string BaseUrl { get; }
        public string LogoUrl { get; }
        public string PreAuthUrl { get; }

        public AffiliateData(Data.IAffiliateDescriptor affiliate)
        {
            this.Title = affiliate.Name;
            // TODO: Convert Logo URL into data URL
            this.LogoUrl = new Uri(affiliate.Uri, "/images/logo.png").ToString();
            this.PreAuthUrl = new Uri(affiliate.Uri, "/oauth/preauth").ToString();
            this.BaseUrl = affiliate.Uri.ToString().TrimEnd('/');
        }
    }

    public class LoginRequestData : IWebRequest
    {
        public Guid ID { get; set; }
        public Uri? ServerUri { get; set; }

        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
