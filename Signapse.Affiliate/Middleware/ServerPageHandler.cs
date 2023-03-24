using Signapse.Client;
using Signapse.Data;
using Signapse.RequestData;
using Signapse.Server;
using Signapse.Server.Affiliate;
using Signapse.Server.Common.Services;
using Signapse.Services;
using System;
using System.Threading.Tasks;

namespace Signapse.Middleware
{
    public class ServerDataModel : IAffiliateRequest, IWebRequest
    {
        public Guid FromServerID { get; set; }
        public SignapseServerDescriptor Descriptor { get; set; } = new SignapseServerDescriptor();
    }

    /// <summary>
    /// This handler is responsible for all requests coming from other affiliate servers
    /// </summary>
    public class ServerPageHandler
    {
        readonly AffiliateServer server;
        readonly JsonDatabase<AffiliateJoinRequest> dbRequest;
        readonly JsonSerializerFactory jsonFactory;

        public ServerPageHandler(AffiliateServer server, JsonDatabase<AffiliateJoinRequest> dbRequest, JsonSerializerFactory jsonFactory)
        {
            this.server = server;
            this.dbRequest = dbRequest;
            this.jsonFactory = jsonFactory;
        }

        public Task<SignapseServerDescriptor> GetDescriptor()
        {
            return Task.FromResult(this.server.Descriptor.ApplyPolicyAccess(AuthResults.Empty));
        }

        public async Task<AffiliateJoinRequest> AddJoinRequest(WebRequest<AffiliateJoinRequest> request)
        {
            var joinRequest = request.Data ?? throw new Exceptions.HttpBadRequest("Invalid Request");

            // Send a request to the original server to verify the request originated there
            using var session = new WebSession(jsonFactory, joinRequest.Descriptor.AffiliateServerUri);
            var origJoinRequest = await session.Get<AffiliateJoinRequest>("affiliate_request", joinRequest.ID);
            if (origJoinRequest == null)
                throw new Exceptions.HttpBadRequest("Invalid Request");

            // Verify there isn't already a pending transaction
            if (dbRequest[request.ID] != null)
                throw new Exceptions.HttpBadRequest("Invalid Request");

            // Add the affiliate to the list of affiliates
            origJoinRequest.Status = AffiliateStatus.Waiting;
            dbRequest.Items.Add(origJoinRequest);

            // Relay this request to all other servers in the network
            _ = Task.Run(() =>
            {

            });

            await dbRequest.Save();

            return origJoinRequest;
        }
    }
}
