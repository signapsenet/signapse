using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Signapse.Data;
using Signapse.Server.Extensions;
using Signapse.Server.Middleware;
using Signapse.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Signapse.Server.Common
{
    /// <summary>
    /// Initialize an run a web application for managing Signapse communications
    /// 
    /// Also includes utility methods for sending requests to other signapse servers
    /// </summary>
    abstract public class ServerBase : IDisposable
    {
        readonly protected WebApplication webApp;
        CancellationTokenSource ctSource = new CancellationTokenSource();

        CancellationTokenSource? ctSourceCombined;
        public Uri ServerUri { get; private set; } = new Uri("http://localhost");

        public Guid ID { get; } = Guid.NewGuid();

        abstract protected void ConfigureDependencies(IServiceCollection services);
        abstract protected void ConfigureEndpoints(WebApplication app);

        public ServerBase(string[] args, bool anyPort = true)
        {
            var builder = WebApplication.CreateBuilder(args);
            if (anyPort)
            {
                builder.WebHost
                    .UseUrls($"https://{IPAddress.Loopback}:0");
            }

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

            // Configure the HTTP request pipeline.
            if (!webApp.Environment.IsDevelopment())
            {
                webApp.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                webApp.UseHsts();
            }

            ConfigureEndpoints(webApp);
        }

        virtual public void Run(CancellationToken token)
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

            webApp.RunAsync(ctSourceCombined.Token);
            //webApp.Run(); // Uncomment to determine why server is disposed

            if ((webApp as IApplicationBuilder)?.ServerFeatures is FeatureCollection features
                && features.Get<IServerAddressesFeature>() is IServerAddressesFeature addrFeature)
            {
                ServerUri = new Uri(addrFeature.Addresses.First());
            }
        }

        public void WaitForShutdown()
        {
            webApp.WaitForShutdown();
        }

        public void Dispose()
        {
            ctSource.Cancel();
            ctSource.Dispose();

            ctSourceCombined?.Dispose();
        }
    }
}
