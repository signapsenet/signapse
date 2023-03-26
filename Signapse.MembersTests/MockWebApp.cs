using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace Signapse.Tests
{
    public class MockWebApp : IDisposable
    {
        private const int DEFAULT_PORT = 45456;
        private readonly CancellationTokenSource ctSource = new CancellationTokenSource();
        private readonly HttpClient httpClient;
        private readonly WebApplicationBuilder builder;
        private readonly WebApplication webApp;

        public MockWebApp(Action<IServiceCollection> services, Action<WebApplication> app)
        {
            httpClient = new HttpClient(new SocketsHttpHandler()
            {
                UseCookies = true,
                AllowAutoRedirect = false,
            });

            builder = WebApplication.CreateBuilder();
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ConfigureEndpointDefaults(listenOptions =>
                {
                    if (listenOptions.IPEndPoint != null)
                    {
                        listenOptions.IPEndPoint.Address = IPAddress.Loopback;
                        listenOptions.IPEndPoint.Port = DEFAULT_PORT;
                    }
                });
            });
            services?.Invoke(builder.Services);

            webApp = builder.Build();
            app?.Invoke(webApp);

            webApp.RunAsync($"http://127.0.0.1:{DEFAULT_PORT}").WaitAsync(ctSource.Token);
        }

        public virtual void Dispose()
        {
            webApp.Lifetime.StopApplication();

            httpClient.Dispose();
            ctSource.Cancel();

            ctSource.Dispose();
        }

        public async Task<T?> SendRequest<T>(HttpMethod method, string absPath, object? args = null)
        {
            using var res = await SendRequest(method, absPath, args);
            try
            {
                return await res.Content.ReadFromJsonAsync<T>();
            }
            catch
            {
                return default(T);
            }
        }

        public async Task<HttpResponseMessage> SendRequest(HttpMethod method, string absPath, object? args = null)
        {
            using var request = new HttpRequestMessage(method, $"http://127.0.0.1:{DEFAULT_PORT}{absPath}");
            if (args != null)
            {
                request.Content = JsonContent.Create(args, options: new System.Text.Json.JsonSerializerOptions()
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault
                });
            }

            return await httpClient.SendAsync(request);
        }
    }
}