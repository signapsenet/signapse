using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Signapse.BlockChain;
using Signapse.BlockChain.Transactions;
using Signapse.Server.Web.Services;
using Signapse.Services;
using System;
using System.Collections.Generic;
using System.Threading;

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
        private static void InitTestServers(WebServerConfig webConfig, Uri webServerUri, CancellationToken token)
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

        private static SignapseLedger CreateTestLedger(List<TestWebServer> webServers)
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

        private static void AddMember(WebApplication webApp, string email)
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
}