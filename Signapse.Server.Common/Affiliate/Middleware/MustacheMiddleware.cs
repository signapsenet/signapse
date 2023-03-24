using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Mustache;
using Signapse.Data;
using Signapse.Exceptions;
using Signapse.Server;
using Signapse.Server.Common.Services;
using Signapse.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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