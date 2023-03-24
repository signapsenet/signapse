using Microsoft.AspNetCore.Http;
using Signapse.Client;
using Signapse.Data;
using Signapse.RequestData;
using Signapse.Server;
using Signapse.Server.Affiliate;
using Signapse.Server.Common.Services;
using Signapse.Server.Middleware;
using Signapse.Services;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Signapse.Middleware
{
    /// <summary>
    /// This is the data model for loading the admin.html page.
    /// </summary>
    [DataFor("/admin.html")]
    public class AdminDataModel : AffiliateMustacheData, IWebRequest
    {
        public class AffiliateRequest
        {
            public AffiliateRequest(AffiliateJoinRequest request)
            {

            }
        }

        public AdminDataModel() { }

        public AdminDataModel(IHttpContextAccessor acc,
            AppConfig appConfig,
            JsonDatabase<Server.AffiliateJoinRequest> dbRequests,
            JsonDatabase<Data.User> dbUsers,
            JsonDatabase<Data.SignapseServerDescriptor> dbAffiliates,
            AuthorizationFactory authFactory)
            : base(acc, dbUsers, authFactory)
        {
            // TODO: Replace this with endpoint authorization and security policies (for consistency)
            if (!Auth.IsAdmin)
            {
                throw new Exceptions.HttpRedirect("/index.html");
            }

            // Block users from being able to see/edit specific types of data based on their authority
            if (Auth.IsUsersAdmin)
            {
                this.Users = dbUsers.Items
                    .Select(it => it.ApplyPolicyAccess(this.Auth))
                    .ToArray();
            }

            if (Auth.IsAffiliatesAdmin)
            {
                this.Affiliates = dbAffiliates.Items
                    .Select(it => it.ApplyPolicyAccess(this.Auth))
                    .ToArray();

                this.AffiliateRequests = dbRequests.Items
                    .Select(it => new AffiliateRequest(it.ApplyPolicyAccess(this.Auth)))
                    .ToArray();
            }

            this.APIKey = appConfig.APIKey;
            this.SiteName = appConfig.SiteName;
        }

        public bool IsLoggedIn { get; } = false;
        public Data.User[] Users { get; } = { };
        public Data.SignapseServerDescriptor[] Affiliates { get; } = { };
        public AffiliateRequest[] AffiliateRequests { get; } = { };

        public string APIKey { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public string AffiliateRequestUrl { get; set; } = string.Empty;
        public Guid RequestID { get; set; }
    }

    /// <summary>
    /// Handle callbacks from the scripts on the admin.html page.  Refrain from using
    /// MVC and Controllers because html/js is fine for simple sites like this.
    /// </summary>
    public class AdminPageHandler
    {
        readonly AppConfig appConfig;
        readonly Cryptography crypto;
        readonly AffiliateServer server;
        readonly JsonDatabase<AffiliateJoinRequest> dbRequest;
        readonly JsonSerializerFactory jsonFactory;

        public AdminPageHandler(AppConfig appConfig, Cryptography crypto, AffiliateServer server,
            JsonDatabase<AffiliateJoinRequest> dbRequest, JsonSerializerFactory jsonFactory)
        {
            this.appConfig = appConfig;
            this.crypto = crypto;
            this.server = server;
            this.dbRequest = dbRequest;
            this.jsonFactory = jsonFactory;
        }

        public async Task ProcessSiteConfig(WebRequest<AdminDataModel> request)
        {
            var data = request.Data ?? throw new Exceptions.HttpBadRequest("Invalid Request");

            this.appConfig.APIKey = data.APIKey;
            this.appConfig.Save();

            await Task.CompletedTask;
        }

        public Task<string> GenerateAPIKey()
        {
            return Task.FromResult(crypto.GetRandomAlphanumericString(12));
        }

        public async Task AcceptAllRequests()
        {
            var requests = dbRequest.Items
                .Where(it => it.FromServerID != server.ID)
                .Where(it => it.Status == AffiliateStatus.Waiting);

            foreach (var req in requests)
            {
                req.Status = AffiliateStatus.Accepted;
            }

            await dbRequest.Save();
        }

        public async Task RejectAllRequests()
        {
            var requests = dbRequest.Items
                .Where(it => it.FromServerID != server.ID)
                .Where(it => it.Status == AffiliateStatus.Waiting);

            foreach (var req in requests)
            {
                req.Status = AffiliateStatus.Rejected;
            }

            await dbRequest.Save();
        }

        public async Task<AdminDataModel.AffiliateRequest?> AddAffiliate(WebRequest<AdminDataModel> request)
        {
            var data = request.Data ?? throw new Exceptions.HttpBadRequest("Invalid Request");
            using var session = new WebSession(jsonFactory, new Uri(data.AffiliateRequestUrl));

            if (await session.GetAffiliateDetails() is SignapseServerDescriptor remoteServer)
            {
                var joinRequest = new AffiliateJoinRequest()
                {
                    Descriptor = server.Descriptor,
                    FromServerID = server.ID,
                    Status = AffiliateStatus.Waiting,
                    ToServerID = remoteServer.ID
                };

                dbRequest.Items.Add(joinRequest);
                var res = await session.SendRequest<AffiliateJoinRequest>(
                    HttpMethod.Put,
                    "/api/v1/server/add_join_request",
                    new { Data = joinRequest }
                );

                if (res != null)
                {
                    // We auto-accept requests from ourselves. This will still require the other servers
                    // to accept before we can submit the join transaction
                    joinRequest.Status = AffiliateStatus.Accepted;
                    await dbRequest.Save();

                    return new AdminDataModel.AffiliateRequest(res);
                }
                else
                {
                    // Roll back the add request
                    dbRequest.Items.Remove(joinRequest);
                }
            }

            return null;
        }
    }
}
