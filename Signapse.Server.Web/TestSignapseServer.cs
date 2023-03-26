using Microsoft.Extensions.DependencyInjection;
using Signapse.BlockChain;
using Signapse.Data;
using Signapse.Server.Affiliate;
using Signapse.Services;
using System.Threading;

namespace Signapse.Web
{
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
            services.AddTransient<ISecureStorage, TestStorage>();
            services.AddTransient<IAppDataStorage, TestStorage>();
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