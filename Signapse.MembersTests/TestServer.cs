using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Signapse.BlockChain;
using Signapse.Data;
using Signapse.Services;
using Signapse.Test;

namespace Signapse.Server.Tests
{
    /// <summary>
    /// This is the intermediate location for the final server design
    /// </summary>
    public class TestAffiliateServer : LocalAffiliateServer
    {
        private readonly JsonDatabase<SignapseServerDescriptor> dbAffiliates;

        public new WebApplication WebApp => this.webApp;
        public SignapseLedger Ledger => this.WebApp.Services.GetRequiredService<SignapseLedger>();

        public TestAffiliateServer() : base(new string[0], true)
        {
            this.dbAffiliates = webApp.Services.GetRequiredService<JsonDatabase<SignapseServerDescriptor>>();
        }

        protected override void ConfigureDependencies(IServiceCollection services)
        {
            services
                .AddSingleton<IAppDataStorage, MockStorage>()
                .AddSingleton<ISecureStorage, MockStorage>();

            base.ConfigureDependencies(services);
        }
    }
}