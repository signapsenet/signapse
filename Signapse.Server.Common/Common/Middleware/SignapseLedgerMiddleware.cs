using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Signapse.Data;
using Signapse.Services;
using Signapse.RequestData;
using Signapse.BlockChain.Transactions;
using Signapse.BlockChain;
using System.Security.Cryptography;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace Signapse.Server.Middleware
{
    class SignapseLedgerMiddleware
    {
        readonly Transaction<SignapseServerDescriptor> affiliates;
        readonly Transaction<AffiliateJoinRequest> joinRequests;
        readonly SignapseServerDescriptor server;
        readonly SignapseLedger ledger;
        readonly RSASigner rsaSigner;
        readonly HttpContext context;

        public SignapseLedgerMiddleware(
            Transaction<SignapseServerDescriptor> dbAffiliates,
            Transaction<AffiliateJoinRequest> joinRequests,
            SignapseServerDescriptor server,
            SignapseLedger ledger,
            RSASigner rsaSigner,
            HttpContext context)
        {
            this.affiliates = dbAffiliates;
            this.joinRequests = joinRequests;
            this.server = server;
            this.ledger = ledger;
            this.rsaSigner = rsaSigner;
            this.context = context;
        }

        async Task<bool> ValidateTransaction(IBlock block)
        {
            RSAParameters publicKey(Guid affiliateID)
            {
                var affiliate = affiliates[affiliateID]
                    ?? throw new Exceptions.HttpBadRequest("Unknown Affiliate");

                return affiliate.RSAPublicKey.RSAParameters;
            }

            if (!block.IsValid(this.ledger.LastBlock))
            {
                throw new Exceptions.HttpBadRequest("Invalid Block");
            }
            else if (block.Transaction is JoinTransaction joinTransaction)
            {
                var req = joinRequests
                    .Where(r => r.Status == AffiliateStatus.Accepted)
                    .Where(r => r.FromServerID == joinTransaction.AffiliateID_1)
                    .Where(r => r.ToServerID == joinTransaction.AffiliateID_2)
                    .Where(r => r.ID == joinTransaction.JoinRequestID)
                    .FirstOrDefault() ?? throw new Exceptions.HttpBadRequest("No Join Request Found");

                joinRequests.Delete(req.ID);
                affiliates.Insert(req.Descriptor);

                await joinRequests.Commit();
                await affiliates.Commit();
            }
            else
            {
                var isSigned = block.Signatures.Length > 0
                    && block.Signatures.All(sig => this.rsaSigner.Verify(publicKey(sig.AffiliateID), block, sig.Data));

                if (block.Signatures.Length < 1 || !isSigned)
                {
                    throw new Exceptions.HttpBadRequest("Invalid Signature");
                }
                else if (block.Transaction == null)
                {
                    throw new Exceptions.HttpBadRequest("Invalid Transaction");
                }
            }

            return true;
        }

        async public Task HandleJoinPut()
        {
            var request = await context.Request.ReadFromJsonAsync<WebRequest<AffiliateJoinRequest>>();
            var joinRequest = request?.Data
                ?? throw new Exceptions.HttpBadRequest("Invalid Request");

            var status = joinRequest.Status;
            joinRequest = joinRequests[joinRequest.ID]
                ?? throw new Exceptions.HttpBadRequest("Invalid Request");

            joinRequest.Status = status;

            await joinRequests.Commit();
        }

        async public Task HandleJoinPost()
        {
            var request = await context.Request.ReadFromJsonAsync<WebRequest<SignapseServerDescriptor>>();
            var descriptor = request?.Data;

            if (descriptor != null)
            {
                var joinRequest = new AffiliateJoinRequest()
                {
                    FromServerID = descriptor.ID,
                    ToServerID = this.server.ID,
                    Status = AffiliateStatus.Waiting,
                    Descriptor = descriptor
                };
                joinRequests.Insert(joinRequest);

                await context.Response.WriteAsJsonAsync(joinRequest);
                
                await joinRequests.Commit();
            }
        }

        async public Task HandleJoinRequestGet()
        {
            await context.Response.WriteAsJsonAsync(new
            {
                Result = joinRequests.ToArray()
            });
        }

        async public Task HandleTransactionPut()
        {
            var request = await context.Request.ReadFromJsonAsync<AffiliateRequest<Block>>();

            if (request?.Data is Block block && await ValidateTransaction(block))
            {
                this.ledger.Add(block);

                await context.Response.WriteAsJsonAsync(true);
            }
        }

        async public Task HandleContentGet()
        {
            var ledger = context.RequestServices.GetRequiredService<SignapseLedger>();
            await context.Response.WriteAsJsonAsync(new
            {
                Result = ledger.Transactions
                    .Select(trn => trn.Transaction)
                    .OfType<ISignapseContent>()
                    .ToArray()
            });
        }
    }

    static public class SignapseLedgerExtensions
    {
        static public IServiceCollection AddSignapseLedger(this IServiceCollection services)
        {
            services.AddSingleton<SignapseLedger>();

            return services;
        }

        static public WebApplication UseSignapseLedger(this WebApplication app)
        {
            async Task handleRequest(HttpContext context, Func<SignapseLedgerMiddleware, Task> callback)
            {
                using var scope = context.RequestServices.CreateScope();
                var middleware = ActivatorUtilities.CreateInstance<SignapseLedgerMiddleware>(scope.ServiceProvider, context);

                await callback(middleware);
            }

            app.MapPut("/api/v1/join", ctx => handleRequest(ctx, s => s.HandleJoinPut()));
            app.MapPost("/api/v1/join", ctx => handleRequest(ctx, s => s.HandleJoinPost()));

            app.MapGet("/api/v1/join_requests", ctx => handleRequest(ctx, s => s.HandleJoinRequestGet()));
            app.MapPut("/api/v1/transaction", ctx => handleRequest(ctx, s => s.HandleTransactionPut()));
            app.MapGet("/api/v1/content", ctx => handleRequest(ctx, s => s.HandleContentGet()));

            return app;
        }
    }

}
