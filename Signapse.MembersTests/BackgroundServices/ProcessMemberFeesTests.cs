using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.BlockChain;
using Signapse.Client;
using Signapse.Data;
using Signapse.Server.Common.BackgroundServices;
using Signapse.Server.Tests;
using Signapse.Services;
using Signapse.Test;
using Signapse.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Signapse.BackgroundServices.Tests
{
    [TestClass]
    public class ProcessMemberFeesTests : DITestClass
    {
        const float MEMBER_FEE = 100;
        CancellationTokenSource ctSource = new CancellationTokenSource();

        public override void InitServices(ServiceCollection services)
        {
            services
                .AddSingleton<IAppDataStorage, MockStorage>()
                .AddSingleton<ISecureStorage, MockStorage>()
                .AddSingleton<SignapseLedger>()
                .AddSingleton(typeof(JsonDatabase<>))
                .AddTransient<JsonSerializerFactory>()
                .AddTransient<PaymentProcessor>();

            base.InitServices(services);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            ctSource = new CancellationTokenSource();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ctSource.Cancel();

            foreach (var server in servers.Values)
            {
                server.Dispose();
            }
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

        async Task<TestServer[]> PrepareAffiliates()
        {
            string[] serverNames = { "Alice", "Joe", "Jordan" };

            for (int i = 0; i < 3; i++)
            {
                var server = new MemberFeeTestServer(serverNames[i]);
                server.Run(ctSource.Token);

                servers[serverNames[i]] = server;
            }

            foreach (var aff in servers.Values)
            {
                var jsonFactory = aff.WebApp.Services.GetRequiredService<JsonSerializerFactory>();
                using var client = new WebSession(jsonFactory, aff.ServerUri);

                var res = await client.Install(smtp, siteName, networkName, adminEmail, adminPassword);
                Assert.AreEqual(true, res.IsSuccessStatusCode);
            }

            return servers.Values.ToArray();
        }

        async Task ProcessMemberFeesTest()
        {
            using var process = ActivatorUtilities.CreateInstance<MockProcessMemberFees>(scope.ServiceProvider);

            // Create 4 servers and affiliate 3 of them
            var affiliates = PrepareAffiliates();

            // Send join request to the first affiliate

            // Accept the join request at each affiliate

            // Run the process
            await process.RunTest();

            // Validate that the ledger has been updated

            // Validate that the affiliate list contains all 3 affiliates

            // Validate that all of the affiliates now list this server

        }

        async Task ProcessFees()
        {
            await Task.CompletedTask;
        }

        Dictionary<string, MemberFeeTestServer> servers = new Dictionary<string, MemberFeeTestServer>();

        async Task WatchContent(string sourceSite, string contentSite, string memberSite)
        {
            MemberFeeTestServer sourceServer = servers[sourceSite];
            MemberFeeTestServer contentServer = servers[contentSite];
            MemberFeeTestServer memberServer = servers[memberSite];
            Data.Member member = memberServer.Members.First();
            
            // Log into the source server
            var jsonFactory = sourceServer.WebApp.Services.GetRequiredService<JsonSerializerFactory>();
            using (var session = new WebSession(jsonFactory, sourceServer.ServerUri))
            {
                await session.Login(member.ID, "password", memberServer.Descriptor.WebServerUri);
            }

            // Retrieve the streaming url from the source server

            // Stream the content from the content server


            await Task.CompletedTask;
        }

        void ValidateLedger(string serverName, string pct)
        {
            var server = servers[serverName];
            float frac = float.Parse(pct) / 100.0f;

            Assert.Fail("Incomplete Test");
        }

        async Task ValidateTestCase(string methodName)
        {
            Regex reWhen = new Regex(@"(\w+)'s member consumes (\w+)'s content from (\w+)'s site");
            Regex reThen = new Regex(@"(\w+) receives (\d+)% of the fee");

            var methodInfo = this.GetType().GetMethod(methodName)
                ?? throw new Exception($"Unable to retrieve {methodName}");

            var whenAttr = methodInfo.GetCustomAttribute<WhenAttribute>()
                ?? throw new Exception($"Missing 'When' attribute on {methodName}");

            var thenAttrs = methodInfo.GetCustomAttributes<ThenAttribute>()
                ?? throw new Exception($"Missing 'Then' attribute on {methodName}");

            MatchCollection matches = reWhen.Matches(whenAttr.Clause)
                ?? throw new Exception("Invalid Where clause");

            string memberSite = matches[0].Groups[1].Value;
            string contentSite = matches[0].Groups[2].Value;
            string sourceSite = matches[0].Groups[3].Value;
            await WatchContent(sourceSite, contentSite, memberSite);
            await ProcessFees();

            foreach (var thenAttr in thenAttrs)
            {
                MatchCollection thenMatches = reThen.Matches(thenAttr.Clause)
                    ?? throw new Exception("Invalid Then clause");

                string feeRecipient = thenMatches[0].Groups[1].Value;
                string feeAmount = thenMatches[0].Groups[2].Value;

                ValidateLedger(feeRecipient, feeAmount);

                Assert.Fail("Incomplete Test Case");
            }

            await Task.CompletedTask;
        }

        [TestMethod]
        [When("Alice's member consumes Alice's content from Alice's site")]
        [Then("Alice receives 100% of the fee")]
        public async Task Scenario_1()
        {
            await ValidateTestCase(nameof(Scenario_1));
        }

        [TestMethod]
        [When("Alice's member consumes Joe's content from Alice's site")]
        [Then("Alice receives 10% of the fee")]
        [Then("Joe receives 90% of the fee")]
        public async Task Scenario_2()
        {
            await ValidateTestCase(nameof(Scenario_2));
        }

        [TestMethod]
        [When("Alice's member consumes Alice's content from Joe's site")]
        [Then("Alice receives 0% of the fee")]
        [Then("Joe receives 0% of the fee")]
        public async Task Scenario_3()
        {
            await ValidateTestCase(nameof(Scenario_3));
        }

        [TestMethod]
        [When("Alice's member consumes Joe's content from Joe's site")]
        [Then("Alice receives 0% of the fee")]
        [Then("Joe receives 0% of the fee")]
        public async Task Scenario_4()
        {
            await ValidateTestCase(nameof(Scenario_4));
        }

        [TestMethod]
        [When("Alice's member consumes Jordan's content from Joe's site")]
        [Then("Alice receives 10% of the fee")]
        [Then("Jordan receives 90% of the fee")]
        public async Task Scenario_5()
        {
            await ValidateTestCase(nameof(Scenario_5));
        }
    }

    public class MockProcessMemberFees : ProcessMemberFees
    {
        public MockProcessMemberFees(SignapseLedger ledger, PaymentProcessor payments, JsonDatabase<Member> dbMembers)
            : base(ledger, payments, dbMembers)
        {
        }

        public Task RunTest() => this.Process(CancellationToken.None);
    }

    public class MemberFeeTestServer : TestServer
    {
        public string Name { get; }
        public Data.Member[] Members { get; } = { };

        public MemberFeeTestServer(string name)
        {
            this.Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    class WhenAttribute : Attribute
    {
        public string Clause { get; }
        public WhenAttribute(string clause)
            => Clause = clause;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    class ThenAttribute : Attribute
    {
        public string Clause { get; }
        public ThenAttribute(string clause)
            => Clause = clause;
    }
}