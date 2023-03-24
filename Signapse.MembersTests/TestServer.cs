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
    public class TestServer : AffiliateServer
    {
        readonly JsonDatabase<SignapseServerDescriptor> dbAffiliates;

        new public WebApplication WebApp => this.webApp;
        public SignapseLedger Ledger => this.WebApp.Services.GetRequiredService<SignapseLedger>();

        public TestServer() : base(new string[0])
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

        List<AffiliateJoinRequest> joinRequests = new List<AffiliateJoinRequest>();
        protected override void ConfigureEndpoints(WebApplication app)
        {
            //app.MapPost("/api/v1/login", async ctx =>
            //{
            //    var db = ctx.RequestServices.GetRequiredService<JsonDatabase<Data.User>>();
            //    var loginParams = await ctx.Request.ReadFromJsonAsync<Data.User>()
            //        ?? throw new Exceptions.HttpBadRequest("Invalid Parameters");

            //    var adminUser = db.Items
            //        .Where(it => it.Email == loginParams.Email)
            //        .Where(it => it.Password == loginParams.Password)
            //        .FirstOrDefault();

            //    if (adminUser != null)
            //    {
            //        await ctx.SignInAsync("cookie", Claims.CreatePrincipal(adminUser, "cookie"));
            //    }
            //});

            //app.MapPut("/api/v1/join", async ctx =>
            //{
            //    var request = await ctx.Request.ReadFromJsonAsync<WebRequest<AffiliateJoinRequest>>();
            //    var joinRequest = request?.Data
            //        ?? throw new Exceptions.HttpBadRequest("Invalid Request");

            //    var status = joinRequest.Status;
            //    joinRequest = joinRequests.FirstOrDefault(jr => jr.ID == joinRequest.ID)
            //        ?? throw new Exceptions.HttpBadRequest("Invalid Request");

            //    joinRequest.Status = status;
            //});

            //app.MapPost("/api/v1/join", async ctx =>
            //{
            //    var request = await ctx.Request.ReadFromJsonAsync<WebRequest<AffiliateDescriptor>>();
            //    var descriptor = request?.Data;

            //    if (descriptor != null)
            //    {
            //        var joinRequest = new AffiliateJoinRequest()
            //        {
            //            FromServerID = descriptor.ID,
            //            ToServerID = this.ID,
            //            Status = AffiliateStatus.Waiting,
            //            Descriptor = descriptor
            //        };
            //        joinRequests.Add(joinRequest);

            //        await ctx.Response.WriteAsJsonAsync(joinRequest);
            //    }
            //});

            //app.MapGet("/api/v1/join_requests", async ctx =>
            //{
            //    await ctx.Response.WriteAsJsonAsync(new
            //    {
            //        Result = joinRequests.ToArray()
            //    });
            //});

            //app.MapPut("/api/v1/transaction", async ctx =>
            //{
            //    var request = await ctx.Request.ReadFromJsonAsync<AffiliateRequest<Block>>();

            //    var jsonFactory = ctx.RequestServices.GetRequiredService<JsonSerializerFactory>();
            //    if (request?.Data is Block block && ValidateTransaction(block))
            //    {
            //        this.Ledger.Add(block);

            //        await ctx.Response.WriteAsJsonAsync(true);
            //    }
            //});

            base.ConfigureEndpoints(app);
        }
    }
}