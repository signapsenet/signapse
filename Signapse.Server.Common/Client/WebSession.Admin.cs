using Signapse.Data;
using Signapse.Server;
using Signapse.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Signapse.Client
{
    /// <summary>
    /// Communication paths from clients to Signapse endpoints
    /// </summary>
    public partial class SignapseWebSession : HttpSession
    {
        public Task<string?> GenerateAPIKey()
        {
            return SendRequest<string>(HttpMethod.Get, "/api/v1/admin/generateAPIKey");
        }

        public Task SaveSiteConfig(string apiKey)
        {
            var data = new { ApiKey = apiKey };

            return SendRequest(HttpMethod.Put, "/api/v1/admin/siteConfig", new { Data = data });
        }

        public Task<SignapseServerDescriptor?> GetAffiliateDetails()
        {
            return SendRequest<SignapseServerDescriptor>(HttpMethod.Get, "/api/v1/server/desc");
        }

        public Task<AffiliateJoinRequest?> AddAffiliate(Uri url)
        {
            var data = new { AffiliateRequestUrl = url };

            return SendRequest<AffiliateJoinRequest>(HttpMethod.Put, "/api/v1/admin/addAffiliate", new { Data = data });
        }

        public Task AcceptAllRequests()
        {
            return SendRequest(HttpMethod.Post, "/api/v1/admin/acceptAllRequests");
        }

        public Task RejectAllRequests()
        {
            return SendRequest(HttpMethod.Post, "/api/v1/admin/rejectAllRequests");
        }

        public Task AcceptRequest(Guid requestID)
        {
            var data = new AffiliateJoinRequest() { ID = requestID, Status = AffiliateStatus.Accepted };
            return Put("affiliate_request", data);
        }

        public Task RejectRequest(Guid requestID)
        {
            var data = new AffiliateJoinRequest() { ID = requestID, Status = AffiliateStatus.Rejected };
            return Put("affiliate_request", data);
        }

        public Task<T?> Put<T>(string path, T item)
            where T : IDatabaseEntry
        {
            if (path.StartsWith("/") == false)
            {
                path = $"/api/v1/{path}";
            }

            return SendRequest<T?>(HttpMethod.Put, path, item);
        }

        public Task Delete<T>(string path, Guid id)
            where T : IDatabaseEntry
        {
            if (path.StartsWith("/") == false)
            {
                path = $"/api/v1/{path}";
            }

            return SendRequest(HttpMethod.Delete, $"{path}/{id}");
        }

        public async Task<T[]> Get<T>(string path, int page)
            where T : IDatabaseEntry
        {
            if (path.StartsWith("/") == false)
            {
                path = $"/api/v1/{path}";
            }

            return await SendRequest<T[]>(HttpMethod.Get, $"{path}?page={page}")
                ?? new T[0];
        }

        public Task<T?> Get<T>(string path, Guid id)
            where T : IDatabaseEntry
        {
            if (path.StartsWith("/") == false)
            {
                path = $"/api/v1/{path}";
            }
            
            return SendRequest<T>(HttpMethod.Get, $"{path}/{id}");
        }
    }
}
