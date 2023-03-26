using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Signapse.BlockChain;
using Signapse.BlockChain.Transactions;
using Signapse.Data;
using Signapse.RequestData;
using Signapse.Services;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Signapse.Server.Middleware
{
    internal class SignapseLedgerMiddleware
    {
        private readonly Transaction<SignapseServerDescriptor> affiliates;
        private readonly Transaction<AffiliateJoinRequest> joinRequests;
        private readonly SignapseServerDescriptor server;
        private readonly SignapseLedger ledger;
        private readonly RSASigner rsaSigner;
        private readonly HttpContext context;

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

        private async Task<bool> ValidateTransaction(IBlock block)
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

        public async Task HandleJoinPut()
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

        public async Task HandleJoinPost()
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

        public async Task HandleJoinRequestGet()
        {
            await context.Response.WriteAsJsonAsync(new
            {
                Result = joinRequests.ToArray()
            });
        }

        public async Task HandleTransactionPut()
        {
            var request = await context.Request.ReadFromJsonAsync<AffiliateRequest<Block>>();

            if (request?.Data is Block block && await ValidateTransaction(block))
            {
                this.ledger.Add(block);

                await context.Response.WriteAsJsonAsync(true);
            }
        }

        public async Task HandleContentGet()
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

    public static class SignapseLedgerExtensions
    {
        public static IServiceCollection AddSignapseLedger(this IServiceCollection services)
        {
            services.AddSingleton<SignapseLedger>();

            return services;
        }

        public static IEndpointConventionBuilder UseSignapseLedger(this WebApplication app)
        {
            async Task handleRequest(HttpContext context, Func<SignapseLedgerMiddleware, Task> callback)
            {
                using var scope = context.RequestServices.CreateScope();
                var middleware = ActivatorUtilities.CreateInstance<SignapseLedgerMiddleware>(scope.ServiceProvider, context);

                await callback(middleware);
            }

            return new EndpointConventionCombiner()
            {
                app.MapPut("/api/v1/join", ctx => handleRequest(ctx, s => s.HandleJoinPut())),
                app.MapPost("/api/v1/join", ctx => handleRequest(ctx, s => s.HandleJoinPost())),

                app.MapGet("/api/v1/join_requests", ctx => handleRequest(ctx, s => s.HandleJoinRequestGet())),
                app.MapPut("/api/v1/transaction", ctx => handleRequest(ctx, s => s.HandleTransactionPut())),
                app.MapGet("/api/v1/content", ctx => handleRequest(ctx, s => s.HandleContentGet())),
            };
        }
    }

}
