using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Signapse.BlockChain;
using Signapse.BlockChain.Transactions;
using Signapse.Data;
using Signapse.Server.Affiliate;
using Signapse.Server.Web;
using Signapse.Server.Web.Services;
using Signapse.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Signapse.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using var ctSource = new CancellationTokenSource();
            using var server = new LocalWebServer(args, false);

            server.Run(ctSource.Token);

#if DEBUG
            AddMember(server.WebApp, "admin");

            var webConfig = server.WebApp.Services.GetRequiredService<WebServerConfig>();
            InitTestServers(webConfig, server.ServerUri, ctSource.Token);
#endif

            server.WaitForShutdown();
        }

#if DEBUG
        static void InitTestServers(WebServerConfig webConfig, Uri webServerUri, CancellationToken token)
        {
            var signapseServer = new TestSignapseServer();
            signapseServer.Run(token);

            signapseServer.Descriptor.WebServerUri = webServerUri;
            webConfig.SignapseServerUri = signapseServer.ServerUri;
            webConfig.SignapseServerAPIKey = TestSignapseServer.API_KEY;

            List<TestWebServer> webServers = new List<TestWebServer>();
            for (int i = 0; i < 3; i++)
            {
                var affiliate = new TestWebServer(new string[0]);
                affiliate.Run(token);

                webServers.Add(affiliate);
            }

            foreach (var affiliate in webServers)
            {
                AddMember(affiliate.WebApp, "admin");

                signapseServer.AddAffiliate(affiliate.signapseServer.Descriptor);

                affiliate.AddAffiliate(signapseServer.Descriptor);
                foreach (var aff in webServers)
                {
                    if (aff != affiliate)
                    {
                        affiliate.AddAffiliate(aff);
                    }
                }
            }

            var ledger = CreateTestLedger(webServers);
            signapseServer.AddTestContent(ledger);
            foreach (var affiliate in webServers)
            {
                affiliate.signapseServer.AddTestContent(ledger);
            }
        }

        static SignapseLedger CreateTestLedger(List<TestWebServer> webServers)
        {
            SignapseLedger res = new SignapseLedger();

            foreach (var webServer in webServers)
            {
                var builder = ActivatorUtilities.CreateInstance<IconBuilder>(webServer.WebApp.Services);
                var descriptor = webServer.signapseServer.Descriptor;

                for (int i = 0; i < 5; i++)
                {
                    var transaction = new ContentTransaction()
                    {
                        AffiliateID = descriptor.ID,
                        Network = descriptor.Network,
                        Title = $"Entry {descriptor.WebServerUri}:{i}",
                        Description = $"Description for entry {i}, for {descriptor.WebServerUri}",
                    };
                    transaction.PreviewImage = getLogo(transaction.ID);

                    var contentBlock = new BlockChain.Block()
                    {
                        Transaction = transaction
                    };
                    contentBlock.Forge(res.LastBlock);

                    res.Add(contentBlock);
                }

                string getLogo(Guid id)
                {
                    var data = builder.LogoImageData(id.ToString());
                    return $"data:image/png;base64,{Convert.ToBase64String(data)}";
                }
            }

            return res;
        }

        static void AddMember(WebApplication webApp, string email)
        {
            const string PASSWORD = "password";

            var hasher = webApp.Services.GetRequiredService<PasswordHasher<Data.Member>>();
            var dbMember = webApp.Services.GetRequiredService<JsonDatabase<Data.Member>>();

            var member = new Data.Member()
            {
                Name = email,
                Email = email,
                MemberTier = Data.MemberTier.Free
            };
            member.Password = hasher.HashPassword(member, PASSWORD);

            dbMember.Items.Add(member);
        }
#endif
    }

    class MemoryStorage : ISecureStorage, IAppDataStorage
    {
        public Task<string> ReadFile(string fname)
        {
            return Task.FromResult("");
        }

        public Task<string> SecureReadFile(string fname)
        {
            return Task.FromResult("");
        }

        public Task SecureWriteFile(string fname, string content)
        {
            return Task.CompletedTask;
        }

        public Task WriteFile(string fname, string content)
        {
            return Task.CompletedTask;
        }
    }

    class TestWebServer : WebServer
    {
        readonly public TestSignapseServer signapseServer;
        public WebApplication WebApp => this.webApp;

        public TestWebServer(string[] args) : base(args, true)
        {
            signapseServer = new TestSignapseServer();
            signapseServer.Run(CancellationToken.None);

            var webConfig = this.WebApp.Services.GetRequiredService<WebServerConfig>();
            webConfig.SignapseServerUri = signapseServer.ServerUri;
            webConfig.SignapseServerAPIKey = TestSignapseServer.API_KEY;
        }

        public override void Run(CancellationToken token)
        {
            base.Run(token);

            signapseServer.Descriptor.WebServerUri = this.ServerUri;
        }

        protected override void ConfigureDependencies(IServiceCollection services)
        {
            services.AddTransient<ISecureStorage, MemoryStorage>();
            services.AddTransient<IAppDataStorage, MemoryStorage>();
            services.AddTransient<IconBuilder>();

            base.ConfigureDependencies(services);
        }

        protected override void ConfigureEndpoints(WebApplication app)
        {
            base.ConfigureEndpoints(app);

            app.MapGet("/images/logo.png", getLogo);

            async Task getLogo(HttpContext context, IconBuilder builder)
            {
                var data = builder.LogoImageData(this.ServerUri.ToString());
                
                context.Response.ContentType = "image/png";
                await context.Response.Body.WriteAsync(data);
            }
        }

        public void AddAffiliate(SignapseServerDescriptor descriptor)
        {
            this.signapseServer.AddAffiliate(descriptor);
        }

        public void AddAffiliate(TestWebServer server)
        {
            this.signapseServer.AddAffiliate(server.signapseServer.Descriptor);
        }
    }

    class TestSignapseServer : AffiliateServer
    {
        public const string API_KEY = "api_key";

        public TestSignapseServer() : base(new string[0], true)
        {
            var appConfig = this.WebApp.Services.GetRequiredService<AppConfig>();
            appConfig.APIKey = API_KEY;
        }

        protected override void ConfigureDependencies(IServiceCollection services)
        {
            services.AddTransient<ISecureStorage, MemoryStorage>();
            services.AddTransient<IAppDataStorage, MemoryStorage>();
            base.ConfigureDependencies(services);
        }

        public override void Run(CancellationToken token)
        {
            base.Run(token);

            var appConfig = this.WebApp.Services.GetRequiredService<AppConfig>();
            appConfig.NetworkName = "Signapse";
            appConfig.SiteName = this.ServerUri.ToString().TrimEnd('/');
            appConfig.SiteName = appConfig.SiteName.Substring(appConfig.SiteName.IndexOf(':') + 3);

            this.Descriptor.Network = appConfig.NetworkName;
            this.Descriptor.Name = appConfig.SiteName;
        }

        public void AddAffiliate(SignapseServerDescriptor descriptor)
        {
            var db = this.WebApp.Services.GetRequiredService<JsonDatabase<SignapseServerDescriptor>>();
            db.Items.Add(descriptor);
        }

        public void AddTestContent(SignapseLedger updatedLedger)
        {
            var ledger = this.WebApp.Services.GetRequiredService<SignapseLedger>();

            ledger.Remove(ledger.LastBlock);
            foreach (var block in updatedLedger.Transactions)
            {
                ledger.Add(block);
            }
        }
    }
}