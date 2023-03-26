using Signapse.Client;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Signapse.Server
{
    /// <summary>
    /// WebClient wrapper that clones the API from the Signapse Web UI to the Signapse server.
    /// </summary>
    public class SignapseWebClient : IDisposable
    {
        public readonly HttpClient httpClient;
        public readonly Uri serverUri;

        public SignapseWebClient(Uri serverUri)
        {
            this.serverUri = serverUri;
            this.httpClient = HttpSession.CreateClient(false);
        }

        public async Task<AffiliateJoinRequest[]> FetchJoinRequests()
        {
            return await SendRequest<AffiliateJoinRequest[]>(HttpMethod.Get, "/api/v1/join_requests")
                ?? new AffiliateJoinRequest[0];
        }

        public Task SendJoinRequest(Uri serverUri)
        {
            return Task.CompletedTask;
        }

        public async Task<bool> UpdateJoinRequest(Guid requestGU, AffiliateStatus status)
        {
            var res = await SendRequest(HttpMethod.Put, "/api/v1/join", new
            {
                Data = new { ID = requestGU, Status = status }
            });
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> Login(string email, string password)
        {
            var res = await SendRequest(HttpMethod.Post, "/api/v1/login", new
            {
                Data = new { email, password }
            });
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> Logout()
        {
            return await SendRequest<bool>(HttpMethod.Post, "/api/v1/logout");
        }

        private class Response<T>
        {
            public T? Result { get; set; }
        }

        public async Task<T?> SendRequest<T>(HttpMethod method, string absPath, object? args = null)
        {
            using var msg = await SendRequest(method, absPath, args);
            try
            {
                if (await msg.Content.ReadFromJsonAsync<Response<T>>() is Response<T> res)
                {
                    return res.Result;
                }
            }
            catch { return default(T); }

            return default(T);
        }

        public async Task<HttpResponseMessage> SendRequest(HttpMethod method, string absPath, object? args = null)
        {
            var uri = new Uri(serverUri, absPath);
            using var request = new HttpRequestMessage(method, uri);

            if (args != null)
            {
                request.Content = JsonContent.Create(args, options: new System.Text.Json.JsonSerializerOptions()
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault
                });
            }

            return await httpClient.SendAsync(request);
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }
    }
}
