#define CATCH_STARTUP_ERRORS
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Signapse.Server.Middleware;
using Signapse.Services;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Signapse.Server.Common
{
    /// <summary>
    /// Initialize an run a web application for managing Signapse communications
    /// 
    /// Also includes utility methods for sending requests to other signapse servers
    /// </summary>
    public abstract class ServerBase : IDisposable
    {
        protected readonly WebApplication webApp;
        private CancellationTokenSource ctSource = new CancellationTokenSource();
        private ManualResetEvent startupCompleted = new ManualResetEvent(false);
        private CancellationTokenSource? ctSourceCombined;
        public Uri ServerUri { get; private set; } = new Uri("http://localhost");

        public Guid ID { get; } = Guid.NewGuid();

        protected event Action ServerStarted;
        protected abstract void ConfigureDependencies(IServiceCollection services);
        protected abstract void ConfigureEndpoints(WebApplication app);

        public ServerBase(string[] args, bool anyPort = true)
        {
            var builder = WebApplication.CreateBuilder(args);

            if (anyPort)
            {
                builder.WebHost
                    .UseUrls($"https://{IPAddress.Loopback}:0");
            }

            builder.WebHost.CaptureStartupErrors(true);
            ConfigureDependencies(builder.Services);

            builder.Services
                .AddHttpContextAccessor()
                .AddSingleton(this)
                .AddSingleton<RSASigner>()
                .AddSingleton<Cryptography>()
                .AddSingleton(typeof(JsonDatabase<>))
                .AddScoped(typeof(Transaction<>))
                .AddTransient(typeof(PasswordHasher<>))
                .UniqueAddTransient<JsonSerializerFactory>()
                .UniqueAddTransient<IAppDataStorage, AppDataStorage>()
                .UniqueAddTransient<ISecureStorage, SecureStorage>();

            webApp = builder.Build();

            webApp.Lifetime.ApplicationStarted.Register(() =>
            {
                ServerStarted?.Invoke();
                startupCompleted?.Set();
            });

            // Configure the HTTP request pipeline.
            if (!webApp.Environment.IsDevelopment())
            {
                webApp.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                webApp.UseHsts();
            }

            ServerStarted += () =>
            {
                // Get the full uri that was used for startup
                if ((webApp as IApplicationBuilder)?.ServerFeatures is FeatureCollection features
                    && features.Get<IServerAddressesFeature>() is IServerAddressesFeature addrFeature)
                {
                    ServerUri = new Uri(addrFeature.Addresses
                        .OrderBy(a => a.StartsWith("https:") ? 0 : 1)
                        .First());
                }
            };

            ConfigureEndpoints(webApp);

            webApp.UseEmbeddedResources(config => config.Assembly = typeof(ServerBase).Assembly);
        }

        public virtual void Run(CancellationToken token)
        {
            ctSourceCombined?.Cancel();
            ctSourceCombined?.Dispose();

            ctSource = new CancellationTokenSource();
            if (token != CancellationToken.None)
            {
                ctSourceCombined = CancellationTokenSource.CreateLinkedTokenSource(ctSource.Token, token);
            }
            else
            {
                ctSourceCombined = ctSource;
            }

            startupCompleted?.Dispose();
            startupCompleted = new ManualResetEvent(false);

#if CATCH_STARTUP_ERRORS
            Task.Run(async () =>
            {
                await webApp.RunAsync(ctSourceCombined.Token);
            }, ctSourceCombined.Token);
#else
            webApp.RunAsync(ctSourceCombined.Token);
#endif

            startupCompleted.WaitOne();
        }

        public void WaitForShutdown()
        {
            try
            {
                webApp.WaitForShutdown();
            }
            catch (ObjectDisposedException) { }
        }

        public void Dispose()
        {
            ctSource.Cancel();
            ctSource.Dispose();

            ctSourceCombined?.Dispose();
        }
    }
}
