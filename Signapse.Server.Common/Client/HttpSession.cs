using Signapse.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Signapse.Client
{
    /// <summary>
    /// Base class for communication with Signapse endpoints
    /// </summary>
    abstract public class HttpSession : IDisposable
    {
        readonly protected Uri serverUri;
        readonly protected JsonSerializerFactory jsonFactory;
        readonly protected HttpClient httpClient;

        protected HttpSession(JsonSerializerFactory jsonFactory, Uri serverUri)
        {
            this.serverUri = serverUri;
            this.jsonFactory = jsonFactory;

            httpClient = CreateClient();
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }

        static public HttpClient CreateClient(bool autoRedirect = true)
        {
            var handler = new HttpClientHandler()
            {
                UseCookies = true,
                AllowAutoRedirect = false,
            };
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
#if DEBUG
            // We ignore SSL errors in debug mode, because I'm too lazy to figure it out right now
            handler.ServerCertificateCustomValidationCallback =
                (httpRequestMessage, cert, cetChain, policyErrors) =>
                {
                    return true;
                };
#endif

            return new HttpClient(handler);
        }

        public class Response<T>
        {
            public T? Result { get; set; }
        }

        public async Task<T?> SendRequest<T>(HttpMethod method, string absPath, object? args = null)
        {
            using var msg = await SendRequest(method, absPath, args);
            try
            {
                var json = await msg.Content.ReadAsStringAsync();
                var opts = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };

                //if (await msg.Content.ReadFromJsonAsync<Response<T>>() is Response<T> res)
                if (System.Text.Json.JsonSerializer.Deserialize<Response<T>>(json, opts) is Response<T> res)
                {
                    return res.Result;
                }
            }
            catch { return default(T); }

            return default(T);
        }

        public async Task<HttpResponseMessage> SendRequest(HttpMethod method, string absPath, object? args = null)
        {
            var url = new Uri(serverUri, absPath);
            using var request = new HttpRequestMessage(method, url);

            if (args != null)
            {
                request.Content = JsonContent.Create(args, options: jsonFactory.Options);
            }

            var res = await httpClient.SendAsync(request);

            return res;
        }
    }
}
