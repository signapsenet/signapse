using Microsoft.AspNetCore.Http;
using Signapse.Data;
using Signapse.Server.Common.Services;
using Signapse.Services;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace Signapse.Server.Middleware
{
    public class AffiliateMustacheData
    {
        [JsonIgnore] public string AppData => this.Serialize();

#if DEBUG
        public bool IsDebug => true;
#else
        public bool IsDebug => false;
#endif

        public AffiliateMustacheData() { }

        public AffiliateMustacheData(IHttpContextAccessor acc, JsonDatabase<Data.User> db, AuthorizationFactory authFactory)
        {
            if (acc.HttpContext?.User is ClaimsPrincipal principal
                && db[principal.SignapseUserID()] is Data.User user)
            {
                var authResults = authFactory.Create(principal).Result;
                this.Auth = authResults;
                this.User = user.ApplyPolicyAccess(authResults);
            }
        }

        public IAuthResults Auth { get; } = AuthResults.Empty;
        public Data.User? User { get; set; }

        public object Translate(string name)
        {
            return name;
        }
    }
}