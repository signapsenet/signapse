using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Signapse.BlockChain;
using Signapse.BlockChain.Transactions;
using Signapse.Data;
using Signapse.RequestData;
using Signapse.Server.Affiliate;
using Signapse.Services;
using Signapse.Test;
using System.Security.Cryptography;

namespace Signapse.Server.Tests
{
    /// <summary>
    /// This is the intermediate location for the final server design
    /// </summary>
    public class TestAffiliateServer : LocalAffiliateServer
    {
        readonly JsonDatabase<SignapseServerDescriptor> dbAffiliates;

        new public WebApplication WebApp => this.webApp;
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