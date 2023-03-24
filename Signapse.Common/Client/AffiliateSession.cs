using Microsoft.AspNetCore.WebUtilities;
using Signapse.BlockChain.Transactions;
using Signapse.Data;
using Signapse.Services;
using System.Security.Cryptography;
using System.Text;

namespace Signapse.Client
{
    /// <summary>
    /// Communication paths between Signapse endpoints
    /// </summary>
    public class AffiliateSession : HttpSession
    {
        readonly Guid clientId;
        readonly string apiKey;

        public AffiliateSession(JsonSerializerFactory jsonFactory, Uri serverUri, Guid clientId, string apiKey)
            : base(jsonFactory, serverUri)
        {
            this.clientId = clientId;
            this.apiKey = apiKey;
        }

        public async Task<bool> Connect()
        {
            using var httpClient = CreateClient(false);

            string tokenUrl = $"{serverUri.ToString().TrimEnd('/')}/oauth/token";
            var tokenRequestData = new Dictionary<string, string?>()
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId.ToString(),
                ["client_secret"] = apiKey,
                ["scope"] = "read",
            };

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
            tokenRequest.Content = new FormUrlEncodedContent(tokenRequestData);

            var tokenResponse = await httpClient.SendAsync(tokenRequest);
            if (tokenResponse.IsSuccessStatusCode)
            {
                string json = await tokenResponse.Content.ReadAsStringAsync();
                var auth = jsonFactory.Deserialize<Dictionary<string, string>>(json);
                if (true == auth?.TryGetValue("access_token", out var accessToken)
                    && true == auth?.TryGetValue("token_type", out var tokenType))
                {
                    this.httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(tokenType, accessToken);
                    return true;
                }
                else
                {
                    this.httpClient.DefaultRequestHeaders.Authorization = null;
                }
            }

            return false;
        }

        public async Task<ISignapseContent[]?> GetContent()
        {
            var res = await SendRequest<ContentTransaction[]>(HttpMethod.Get, "/api/v1/content");
            return res;
        }

        public async Task<SignapseServerDescriptor[]?> GetAffiliates()
        {
            var res = await SendRequest<SignapseServerDescriptor[]>(HttpMethod.Get, "/api/v1/affiliates");
            return res;
        }
    }
}
