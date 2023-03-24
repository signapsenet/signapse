﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Signapse.BlockChain;
using Signapse.BlockChain.Transactions;
using Signapse.Client;
using Signapse.Data;
using Signapse.Server.Common;
using Signapse.Server.Common.BackgroundServices;
using Signapse.Server.Common.Services;
using Signapse.Server.Middleware;
using Signapse.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Signapse.Server.Affiliate
{
    public class AffiliateServer : ServerBase
    {
#if DEBUG
        public WebApplication WebApp => this.webApp;
#endif
        public RSASigner RSASigner { get; }
        public SignapseServerDescriptor Descriptor { get; }

        public AffiliateServer(string[] args, bool anyPort = true) : base(args, anyPort)
        {
            var appConfig = webApp.Services.GetRequiredService<AppConfig>();
            RSASigner = webApp.Services.GetRequiredService<RSASigner>();
            Descriptor = new SignapseServerDescriptor()
            {
                ID = this.ID,
                Name = appConfig.SiteName,
                Network = appConfig.NetworkName,
                RSAPublicKey = new RSAParametersSerializable(RSASigner.PublicKey)
            };
        }

        protected override void ConfigureDependencies(IServiceCollection services)
        {
            services
                .AddSingleton(provider =>
                 {
                     var storage = provider.GetRequiredService<IAppDataStorage>();
                     var jsonFactory = provider.GetRequiredService<JsonSerializerFactory>();
                     var appConfig = new AppConfig(storage, jsonFactory);
                     appConfig.Load();

                     return appConfig;
                 })
                .AddCors(options =>
                {
                    options.DefaultPolicyName = "oauth";
                    options.AddPolicy(name: "oauth", policy =>
                    {
                        policy.AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials()
                            .SetIsOriginAllowed(origin => true);
                    });
                })
                .AddSignapseLedger()
                .AddSingleton<RSASigner>()
                .AddSingleton<Cryptography>()
                .AddSingleton(typeof(JsonDatabase<>))
                .AddScoped(typeof(Transaction<>))
                .AddScoped<AuthorizationFactory>()
                .AddHttpContextAccessor()
                .AddTransient(p => Descriptor)
                .AddTransient(p => new PasswordHasher<User>())
                .UniqueAddTransient<JsonSerializerFactory>()
                .UniqueAddTransient<PaymentProcessor>()
                .UniqueAddTransient<LinkProvider>()
                .UniqueAddTransient<MemberUtility>()
                .UniqueAddTransient<BlockChainProvider>()
                .UniqueAddTransient<EmailProvider>()
                .UniqueAddTransient<IAppDataStorage, AppDataStorage>()
                .UniqueAddTransient<ISecureStorage, SecureStorage>();

            services.AddAuthentication("cookie")
                .AddCookie("cookie", opts =>
                {
                    opts.ExpireTimeSpan = TimeSpan.FromDays(7);
                    opts.SlidingExpiration = true;

                    opts.Events.OnRedirectToLogin = ctx =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/api") &&
                            ctx.Response.StatusCode == (int)HttpStatusCode.OK)
                        {
                            ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        }
                        else
                        {
                            ctx.Response.Redirect(ctx.RedirectUri);
                        }
                        return Task.FromResult(0);
                    };
                    opts.LoginPath = "/index.html";
                });

            services.AddAuthorization();
            services.AddSignapseAuthorization("cookie");
            services.AddSignapseOpenAuth();
        }

        protected override void ConfigureEndpoints(WebApplication app)
        {
            app.UseHttpExceptionHandler();
            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseDefaultFiles();
            app.UseMustacheTemplates();
            app.UseStaticFiles();

            app.UseSignapseOpenAuth();

            app.MapDatabaseEndpoint<User, UserValidator>("user");
            //app.MapDatabaseEndpoint<Data.AffiliateDescriptor>("affiliate");
            app.MapDatabaseEndpoint<AffiliateJoinRequest, AffiliateJoinRequestValidator>("affiliate_request");

            app.MapGet("/api/v1/affiliates", async ctx =>
            {
                var dbAffiliates = ctx.RequestServices.GetRequiredService<JsonDatabase<SignapseServerDescriptor>>();

                await ctx.Response.WriteAsJsonAsync(new
                {
                    Result = dbAffiliates
                        .Items
                        .OfType<IAffiliateDescriptor>()
                        .ToArray()
                });
            });

            app.UseSignapseLedger();
        }

        public override void Run(CancellationToken token)
        {
            base.Run(token);

            Descriptor.AffiliateServerUri = this.ServerUri;

            ActivatorUtilities.CreateInstance<AuditAffiliates>(webApp.Services)
                .Start();

            ActivatorUtilities.CreateInstance<ProcessMemberFees>(webApp.Services)
                .Start();

            ActivatorUtilities.CreateInstance<ProcessAffiliateRequests>(webApp.Services)
                .Start();
        }


        readonly private Dictionary<Uri, HttpClient> clients = new Dictionary<Uri, HttpClient>();
        protected HttpClient Client(Uri serverUri)
        {
            if (clients.TryGetValue(serverUri, out var client) == false)
            {
                client = HttpSession.CreateClient(false);
                clients.Add(serverUri, client);
            }

            return client;
        }

        private async Task<T?> SendRequest<T>(Uri serverUri, HttpMethod method, string absPath, object? args = null)
        {
            using var res = await SendRequest(serverUri, method, absPath, args);
            try
            {
                return await res.Content.ReadFromJsonAsync<T>();
            }
            catch
            {
                return default;
            }
        }

        private async Task<HttpResponseMessage> SendRequest(Uri serverUri, HttpMethod method, string absPath, object? args = null)
        {
            var url = new Uri(serverUri, absPath);
            using var request = new HttpRequestMessage(method, url);

            if (args != null)
            {
                var jsonFactory = this.webApp.Services.GetRequiredService<JsonSerializerFactory>();
                request.Content = JsonContent.Create(args, options: jsonFactory.Options);
            }

            Thread thread = new Thread(() =>
            {
                Client(serverUri);
            });
            thread.Start();
            thread.Join();

            var res = await Client(serverUri).SendAsync(request);

            // Check if we need to refresh our login credentials
            if (absPath != "/api/v1/login"
                && (res.StatusCode == HttpStatusCode.Unauthorized
                    || res.StatusCode == HttpStatusCode.Forbidden))
            {
                // TODO: save/restore login credentials for this server
                await SendRequest(serverUri, HttpMethod.Post, "/api/v1/login", null);

                res = await Client(serverUri).SendAsync(request);
            }

            return res;
        }

        public async Task<AffiliateJoinRequest> SendAffiliateApplicationTo(Uri serverUri)
        {
            var res = await SendRequest<AffiliateJoinRequest>(serverUri, HttpMethod.Post, "/api/v1/join", new
            {
                Data = Descriptor
            });

            return res ?? new AffiliateJoinRequest() { Status = AffiliateStatus.Rejected };
        }

        public Task<bool> SendLoginTo(Uri serverUri)
        {
            return Task.FromResult(true);
        }

        public Task<bool> SendLogoutTo(Uri serverUri)
        {
            return Task.FromResult(true);
        }

        public async Task<bool> SendTransactionTo(Uri serverUri, IBlock block)
        {
            if (block is Block b)
            {
                b.Signatures = new[] {
                    new Signature() {
                        AffiliateID = this.ID,
                        BlockID = b.ID,
                        Data = RSASigner.Sign(b)
                    }
                };
            }

            var res = await SendRequest(serverUri, HttpMethod.Put, "/api/v1/transaction", new
            {
                Data = block
            });

            if (res.IsSuccessStatusCode)
            {
                WebApp.Services.GetRequiredService<SignapseLedger>().Add(block);
            }

            return res.IsSuccessStatusCode;
        }
    }
}