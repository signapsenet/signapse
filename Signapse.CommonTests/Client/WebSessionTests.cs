using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Client;
using Signapse.Data;
using Signapse.Server;
using Signapse.Server.Common.Services;
using Signapse.Server.Tests;
using Signapse.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signapse.Client.Tests
{
    [TestClass]
    public class WebSessionTests : HttpSessionTest<SignapseWebSession>
    {
        protected override SignapseWebSession CreateSession()
        {
            var jsonFactory = affiliateServer.WebApp.Services.GetRequiredService<JsonSerializerFactory>();
            return new SignapseWebSession(jsonFactory, affiliateServer.ServerUri);
        }

        // User details and site configuration
        const string siteName = "signapse.net";
        const string networkName = "signapse.net";
        const string adminEmail = "admin@email.com";
        const string adminPassword = "password";
        readonly static AppConfig.SMTPOptions smtp = new AppConfig.SMTPOptions()
        {
            Address = siteName,
            User = adminEmail,
            Password = adminPassword,
            ReplyTo = adminEmail,
        };

        async Task Install()
        {
            var res = await session.Install(smtp, siteName, networkName, adminEmail, adminPassword);
            Assert.AreEqual(true, res.IsSuccessStatusCode);
        }

        // Initial join requests
        static readonly Guid OtherServerID = Guid.Parse("933764B2-A215-44CB-8096-FF24C397800E");
        AffiliateJoinRequest[]? _joinRequestsToAdd;
        AffiliateJoinRequest[] JoinRequestsToAdd => _joinRequestsToAdd = _joinRequestsToAdd ?? new AffiliateJoinRequest[]
        {
            new () {
                Descriptor = affiliateServer.Descriptor,
                FromServerID = OtherServerID,
                ToServerID = affiliateServer.Descriptor.ID,
                Status = AffiliateStatus.Waiting
            },
            new () {
                Descriptor = affiliateServer.Descriptor,
                FromServerID = OtherServerID,
                ToServerID = affiliateServer.Descriptor.ID,
                Status = AffiliateStatus.Waiting
            },
            new () {
                Descriptor = affiliateServer.Descriptor,
                FromServerID = OtherServerID,
                ToServerID = affiliateServer.Descriptor.ID,
                Status = AffiliateStatus.Accepted
            },
            new () {
                Descriptor = affiliateServer.Descriptor,
                FromServerID = OtherServerID,
                ToServerID = affiliateServer.Descriptor.ID,
                Status = AffiliateStatus.Rejected
            },
            new () {
                Descriptor = affiliateServer.Descriptor,
                FromServerID = affiliateServer.Descriptor.ID,
                ToServerID = OtherServerID,
                Status = AffiliateStatus.Waiting
            },
            new () {
                Descriptor = affiliateServer.Descriptor,
                FromServerID = affiliateServer.Descriptor.ID,
                ToServerID = OtherServerID,
                Status = AffiliateStatus.Waiting
            },
        };

        void AddJoinRequests()
        {
            var db = affiliateServer.WebApp.Services.GetRequiredService<JsonDatabase<AffiliateJoinRequest>>();
            foreach (var jr in JoinRequestsToAdd)
            {
                db.Items.Add(jr);
            }
        }

        [TestMethod]
        public async Task Installation_Updates_AppConfig()
        {
            await Install();

            var jsonFactory = affiliateServer.WebApp.Services.GetRequiredService<JsonSerializerFactory>();
            var appConfig = affiliateServer.WebApp.Services.GetRequiredService<AppConfig>();
            var expectedSmtp = jsonFactory.Serialize(smtp);
            var actualSmtp = jsonFactory.Serialize(appConfig.SMTP);

            Assert.AreEqual(expectedSmtp, actualSmtp);
        }

        [TestMethod]
        public async Task Second_Install_Attempt_Fails()
        {
            await Install();

            var res = await session.Install(smtp, siteName, networkName, adminEmail, adminPassword);

            Assert.AreEqual(false, res.IsSuccessStatusCode);
        }

        [TestMethod]
        public async Task Installation_Inserts_AdminUser()
        {
            await Install();

            var dbUser = affiliateServer.WebApp.Services.GetRequiredService<JsonDatabase<Data.User>>();
            var adminUser = dbUser[Data.User.PrimaryAdminGU];

            Assert.IsNotNull(adminUser);
            Assert.AreEqual(adminEmail, adminUser.Email);
        }

        [TestMethod]
        public async Task Update_Configuration_Updates_AppConfig()
        {
            const string apiKey = "!@#API_KEY_103";
            var appConfig = affiliateServer.WebApp.Services.GetRequiredService<AppConfig>();
            
            await Install();
            await session.SaveSiteConfig(apiKey);

            Assert.AreEqual(apiKey, appConfig.APIKey);
        }

        [TestMethod]
        public async Task AcceptAllRequests_Accepts_Only_Waiting_Requests_To_Server()
        {
            var db = affiliateServer.WebApp.Services.GetRequiredService<JsonDatabase<AffiliateJoinRequest>>();
            
            await Install();
            AddJoinRequests();
            await session.AcceptAllRequests();

            Assert.AreEqual(3, db.Items.Count(it => it.Status == AffiliateStatus.Accepted));
        }

        [TestMethod]
        public async Task RejectAllRequests_Rejects_Only_Waiting_Requests_To_Server()
        {
            var db = affiliateServer.WebApp.Services.GetRequiredService<JsonDatabase<AffiliateJoinRequest>>();
            
            await Install();
            AddJoinRequests();
            await session.RejectAllRequests();

            Assert.AreEqual(3, db.Items.Count(it => it.Status == AffiliateStatus.Rejected));
        }

        [TestMethod]
        public async Task AcceptRequest_Accepts_Only_Indicated_Request()
        {
            var db = affiliateServer.WebApp.Services.GetRequiredService<JsonDatabase<AffiliateJoinRequest>>();
            
            await Install();
            AddJoinRequests();
            await session.Login(adminEmail, adminPassword);
            await session.AcceptRequest(db.Items[0].ID);

            Assert.AreEqual(AffiliateStatus.Accepted, db.Items[0].Status);
            Assert.AreEqual(AffiliateStatus.Waiting, db.Items[1].Status);
        }

        [TestMethod]
        public async Task RejectRequest_Rejects_Only_Indicated_Request()
        {
            var db = affiliateServer.WebApp.Services.GetRequiredService<JsonDatabase<AffiliateJoinRequest>>();
            
            await Install();
            AddJoinRequests();
            await session.Login(adminEmail, adminPassword);
            await session.RejectRequest(db.Items[0].ID);

            Assert.AreEqual(AffiliateStatus.Rejected, db.Items[0].Status);
            Assert.AreEqual(AffiliateStatus.Waiting, db.Items[1].Status);
        }

        [TestMethod]
        public async Task Login_With_Valid_Password_Succeeds()
        {
            await Install();

            var res = await session.Login(adminEmail, adminPassword);

            Assert.IsTrue(res.IsSuccessStatusCode);
        }

        [TestMethod]
        public async Task Login_With_Invalid_Password_Fails()
        {
            await Install();
            var res = await session.Login(adminEmail, $"!{adminPassword}");

            Assert.IsFalse(res.IsSuccessStatusCode);
        }

        [TestMethod]
        public async Task Get_Affiliate_Details_Matches_Server_Descriptor()
        {
            var jsonFactory = affiliateServer.WebApp.Services.GetRequiredService<JsonSerializerFactory>();
            
            await Install();
            var res = await session.GetAffiliateDetails();
            Assert.IsNotNull(res);
            
            string jsonActualDesc = jsonFactory.Serialize(affiliateServer.Descriptor.ApplyPolicyAccess(AuthResults.Empty));
            string jsonResponseDesc = jsonFactory.Serialize(res);
            Assert.AreEqual(jsonActualDesc, jsonResponseDesc);
        }

        [TestMethod]
        public async Task GenerateAPIKey_Returns_Secure_Password()
        {
            await Install();
            var apiKey = await session.GenerateAPIKey();

            Assert.IsNotNull(apiKey);
            Assert.IsTrue(apiKey.Length > 8);
        }

        [TestMethod]
        public async Task AddAffiliate_Creates_Join_Requests()
        {
            using var ctSource = new CancellationTokenSource();
            using var remoteServer = new TestAffiliateServer();
            remoteServer.Run(ctSource.Token);

            var db = affiliateServer.WebApp.Services.GetRequiredService<JsonDatabase<AffiliateJoinRequest>>();
            var remoteDB = remoteServer.WebApp.Services.GetRequiredService<JsonDatabase<AffiliateJoinRequest>>();

            await Install();
            await session.Login(adminEmail, adminPassword);
            var joinRequest = await session.AddAffiliate(remoteServer.ServerUri);

            Assert.IsTrue(db.Items.Count == 1);
            Assert.IsTrue(remoteDB.Items.Count == 1);
            Assert.AreEqual(db.Items[0].ID, remoteDB.Items[0].ID);
        }

        [TestMethod]
        public async Task AddAffiliate_Accepts_Only_Local_Join_Request()
        {
            using var ctSource = new CancellationTokenSource();
            using var remoteServer = new TestAffiliateServer();
            remoteServer.Run(ctSource.Token);

            var db = affiliateServer.WebApp.Services.GetRequiredService<JsonDatabase<AffiliateJoinRequest>>();
            var remoteDB = remoteServer.WebApp.Services.GetRequiredService<JsonDatabase<AffiliateJoinRequest>>();

            await Install();
            await session.Login(adminEmail, adminPassword);
            var joinRequest = await session.AddAffiliate(remoteServer.ServerUri);

            Assert.IsTrue(db.Items.Count == 1);
            Assert.IsTrue(db.Items[0].Status == AffiliateStatus.Accepted);
            Assert.IsTrue(remoteDB.Items.Count == 1);
            Assert.IsTrue(remoteDB.Items[0].Status == AffiliateStatus.Waiting);
            Assert.AreEqual(db.Items[0].ID, remoteDB.Items[0].ID);
        }

        [TestMethod]
        public async Task AddAffiliate_Returns_Waiting_Request()
        {
            using var ctSource = new CancellationTokenSource();
            using var remoteServer = new TestAffiliateServer();
            remoteServer.Run(ctSource.Token);

            var db = affiliateServer.WebApp.Services.GetRequiredService<JsonDatabase<AffiliateJoinRequest>>();
            var remoteDB = remoteServer.WebApp.Services.GetRequiredService<JsonDatabase<AffiliateJoinRequest>>();

            await Install();
            var joinRequest = await session.AddAffiliate(remoteServer.ServerUri);

            Assert.IsNotNull(joinRequest);
            Assert.AreEqual(joinRequest.Status, AffiliateStatus.Waiting);
        }
    }
}