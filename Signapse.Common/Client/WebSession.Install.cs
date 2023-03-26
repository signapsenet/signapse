using Signapse.Data;
using Signapse.Services;

namespace Signapse.Client
{
    /// <summary>
    /// Communication paths from clients to Signapse endpoints
    /// </summary>
    public partial class SignapseWebSession : HttpSession
    {
        public Task<HttpResponseMessage> Install(AppConfig.SMTPOptions smtp, string siteName, string networkName, string adminEmail, string adminPassword)
        {
            var requestData = new
            {
                siteName = siteName,
                networkName = networkName,
                email = adminEmail,
                password = adminPassword,
                confirmPassword = adminPassword,
                sendSignapseRequest = false,
                smtp = smtp
            };

            return SendRequest(HttpMethod.Post, "/api/v1/install", new { Data = requestData });
        }
    }
}
