using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.BlockChain;
using Signapse.BlockChain.Transactions;
using Signapse.Data;
using System.Net;
using UserDB = Signapse.Services.JsonDatabase<Signapse.Data.User>;

namespace Signapse.Server.Tests
{
    [TestClass]
    public class SignapseAffiliateServerTests
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private TestAffiliateServer localServer;
        private TestAffiliateServer remoteServer;
        private CancellationTokenSource ctSource;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [TestInitialize]
        public void Init()
        {
            ctSource = new CancellationTokenSource();

            localServer = new TestAffiliateServer();
            localServer.Run(ctSource.Token);
            createAdminUser(localServer);

            remoteServer = new TestAffiliateServer();
            remoteServer.Run(ctSource.Token);
            createAdminUser(remoteServer);

            // Add this user to the db
            void createAdminUser(TestAffiliateServer server)
            {
                var db = server.WebApp.Services.GetRequiredService<UserDB>();
                var hasher = server.WebApp.Services.GetRequiredService<PasswordHasher<Data.User>>();

                var dbUser = new Data.User()
                {
                    Name = "Admin User",
                    Email = "admin",
                    Password = "password"
                };
                dbUser.AdminFlags = AdministratorFlag.Full;
                dbUser.Password = hasher.HashPassword(dbUser, dbUser.Password);

                db.Items.Add(dbUser);
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            localServer.Dispose();
            remoteServer.Dispose();

            ctSource.Cancel();
            ctSource.Dispose();
        }

        public Block CreateJoinBlock(AffiliateJoinRequest? request = null)
        {
            var joinBlock = new Block()
            {
                Transaction = new JoinTransaction(localServer.ID, remoteServer.ID)
                {
                    JoinRequestID = request?.ID ?? Guid.Empty
                }
            };
            joinBlock.Forge(localServer.Ledger.LastBlock);

            return joinBlock;
        }

        public async Task<SignapseWebClient> AcceptJoinApplication(bool includeJoinTransaction = false)
        {
            var client = new SignapseWebClient(remoteServer.ServerUri);

            // accept the join application
            {
                var loginResult = await client.Login("admin", "password");
                Assert.AreEqual(true, loginResult, "Admin login must succeed");

                // send the join application
                var appResult = await localServer.SendAffiliateApplicationTo(client);
                Assert.AreEqual(AffiliateStatus.Waiting, appResult.Status, "App Request Must Succeed");

                var applications = await client.FetchJoinRequests();
                Assert.AreEqual(1, applications.Length, "Only one join request must exist");

                var res = await client.UpdateJoinRequest(applications[0].ID, AffiliateStatus.Accepted);
                Assert.AreEqual(true, res, "Application rejection must succeed");
            }

            if (includeJoinTransaction)
            {
                var applications = await client.FetchJoinRequests()
                    ?? new AffiliateJoinRequest[0];

                // send the join request and confirm success
                var joinBlock = CreateJoinBlock(applications.First());
                var res = await localServer.SendTransactionTo(remoteServer.ServerUri, joinBlock);
                Assert.AreEqual(true, res);
            }

            return client;
        }

        [TestMethod]
        public async Task Send_Transaction_Without_Joining_Fails()
        {
            // create the join transaction
            var joinBlock = CreateJoinBlock();

            async Task tryJoin(string msg)
            {
                var res = await localServer.SendTransactionTo(remoteServer.ServerUri, joinBlock);
                Assert.AreEqual(false, res, msg);
            }

            // send join request and confirm failure
            await tryJoin("Send before any other requests");

            // send a join application
            await localServer.SendAffiliateApplicationTo(remoteServer.ServerUri);

            // send join request and confirm failure
            await tryJoin("Send after join application");

            // reject the join application (simulate web request)
            {
                using var client = new SignapseWebClient(remoteServer.ServerUri);

                var loginResult = await client.Login("admin", "password");
                Assert.AreEqual(true, loginResult, "Admin login must succeed");

                await localServer.SendAffiliateApplicationTo(client);

                var applications = await client.FetchJoinRequests();
                Assert.AreEqual(1, applications.Length, "Only one join request must exist");

                var res = await client.UpdateJoinRequest(applications[0].ID, AffiliateStatus.Rejected);
                Assert.AreEqual(true, res, "Application rejection must succeed");

                // send join request and confirm failure
                await tryJoin("Send after join application rejection");
            }
        }

        [TestMethod]
        public async Task Accept_Join_Request_Succeeds()
        {
            using var client = await AcceptJoinApplication(true);

            Assert.AreEqual(TransactionType.JoinAffiliate, localServer.Ledger.LastBlock.Transaction?.TransactionType);
            Assert.AreEqual(TransactionType.JoinAffiliate, remoteServer.Ledger.LastBlock.Transaction?.TransactionType);
        }

        [TestMethod]
        public async Task Send_Malformed_Transaction_Fails()
        {
            // send the join request and confirm success
            using var client = await AcceptJoinApplication(true);

            {
                var malformedBlock = new Block();
                var res = await localServer.SendTransactionTo(remoteServer.ServerUri, malformedBlock);
                Assert.AreEqual(false, res, "Empty block must fail.");
            }

            {
                var malformedBlock = new Block() { TimeStamp = DateTimeOffset.UtcNow.AddMinutes(10) };
                var res = await localServer.SendTransactionTo(remoteServer.ServerUri, malformedBlock);
                Assert.AreEqual(false, res, "Future time must fail.");
            }

            {
                var malformedBlock = new Block() { TimeStamp = DateTimeOffset.UtcNow.AddMinutes(-10) };
                var res = await localServer.SendTransactionTo(remoteServer.ServerUri, malformedBlock);
                Assert.AreEqual(false, res, "Prior time must fail.");
            }

            {
                var malformedBlock = new Block() { Transaction = new ContentTransaction() };
                var res = await localServer.SendTransactionTo(remoteServer.ServerUri, malformedBlock);
                Assert.AreEqual(false, res, "Unforged block must fail.");
            }

        }

        [TestMethod]
        public async Task Ledgers_Remain_Syncronized()
        {
            using var client = await AcceptJoinApplication(true);

            // send two more transactions
            for (int i = 0; i < 2; i++)
            {
                await Task.Delay(500); // ensure there's enough delay between transactions

                var block = new Block() { Transaction = new ContentTransaction() };
                block.Forge(localServer.Ledger.LastBlock);

                var res = await localServer.SendTransactionTo(remoteServer.ServerUri, block);
                Assert.AreEqual(true, res, "Send transaction must succeed.");
            }

            // confirm both ledgers match
            var localLedger = localServer.Ledger.Serialize();
            var remoteLedger = remoteServer.Ledger.Serialize();
            Assert.AreEqual(localLedger, remoteLedger);
        }

        [TestMethod]
        public async Task Authorized_Admin_Web_Requests_Succeed()
        {
            using var client = new SignapseWebClient(remoteServer.ServerUri);

            // accept the join application
            {
                var loginResult = await client.Login("admin", "password");
                Assert.AreEqual(true, loginResult, "Admin login must succeed");

                // send the join application
                var appResult = await localServer.SendAffiliateApplicationTo(client);
                Assert.AreEqual(AffiliateStatus.Waiting, appResult.Status, "App Request Must Succeed");

                var applications = await client.FetchJoinRequests();
                Assert.AreEqual(1, applications.Length, "Only one join request must exist");

                var res = await client.UpdateJoinRequest(applications[0].ID, AffiliateStatus.Accepted);
                Assert.AreEqual(true, res, "Application rejection must succeed");
            }
        }

        [TestMethod]
        public async Task Unauthorized_Admin_Web_Requests_Fail()
        {
            using var client = new SignapseWebClient(remoteServer.ServerUri);

            // send the join application
            var appResult = await localServer.SendAffiliateApplicationTo(remoteServer.ServerUri);
            Assert.AreEqual(AffiliateStatus.Rejected, appResult.Status, "App Request Must Fail");

            // accept the join application
            {
                var res = await client.SendRequest(HttpMethod.Get, "/api/v1/join_requests");
                Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode, "Fetch join requests must fail");

                res = await client.SendRequest(HttpMethod.Put, "/api/v1/join", new
                {
                    Data = new { ID = Guid.Empty, Status = AffiliateStatus.Accepted }
                });
                Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode, "Application rejection must fail");
            }
        }
    }
}