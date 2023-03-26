using Signapse.Data;
using Signapse.Services;

namespace Signapse.Client
{
    /// <summary>
    /// Communication paths from clients to Signapse endpoints
    /// </summary>
    public partial class SignapseWebSession : HttpSession
    {
        public Task<HttpResponseMessage> Login(string email, string password)
        {
            var requestData = new { email, password };

            return SendRequest(HttpMethod.Post, "/api/v1/login", new { Data = requestData });
        }

        public Task<HttpResponseMessage> Login(Guid id, string password, Uri memberServerUri)
        {
            var requestData = new { id, password, serverUri = memberServerUri };

            return SendRequest(HttpMethod.Post, "/api/v1/login", new { Data = requestData });
        }
    }
}
