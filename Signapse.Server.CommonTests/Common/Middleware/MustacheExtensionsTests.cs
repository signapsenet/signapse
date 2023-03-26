using Microsoft.AspNetCore.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Client;
using Signapse.Server.Common;
using Signapse.Server.Middleware;
using Signapse.Server.Tests;

namespace Signapse.Tests
{
    [TestClass]
    public class MustacheExtensionsTests
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private ServerBase server;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [TestInitialize]
        public void Initialize()
        {
            server = new TestMustacheServer();
            server.Run(CancellationToken.None);
        }

        [TestCleanup]
        public void Cleanup()
        {
            server.Dispose();
        }

        [TestMethod]
        public async Task GetRequest_Returns_Filled_Template()
        {
            const string EXPECTED_CONTENT = "<html><body>Test</body></html>";

            using HttpClient client = HttpSession.CreateClient();
            client.BaseAddress = server.ServerUri;

            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/mustache_test.html"));
            var responseContent = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(EXPECTED_CONTENT, responseContent);
        }

        [TestMethod]
        public async Task GetPost_Executes_Data_AsyncMethod()
        {
            const string EXPECTED_CONTENT = "{\"test\":\"one_value\"}";

            using HttpClient client = HttpSession.CreateClient();
            client.BaseAddress = server.ServerUri;

            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/mustache_test.html")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "method", "TestMethodAsync" },
                    { "firstArg", "one_value" }
                })
            });
            var responseContent = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(EXPECTED_CONTENT, responseContent);
        }

        [TestMethod]
        public async Task GetPost_Executes_Data_Method()
        {
            const string EXPECTED_CONTENT = "{\"test\":\"one_value\"}";

            using HttpClient client = HttpSession.CreateClient();
            client.BaseAddress = server.ServerUri;

            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/mustache_test.html")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "method", "testMethod" },
                    { "firstArg", "one_value" }
                })
            });
            var responseContent = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(EXPECTED_CONTENT, responseContent);
        }

        private class TestMustacheServer : TestWebServer
        {
            protected override void ConfigureEndpoints(WebApplication app)
            {
                base.ConfigureEndpoints(app);

                app.MapMustacheEndpoint<TestMustacheData>("/mustache_test.html");
            }
        }

        private class TestMustacheData
        {
            public string TestProperty => "Test";

            public TestMustacheData()
            {

            }

            public object TestMethod(string firstArg)
            {
                return new
                {
                    test = firstArg
                };
            }

            public Task<object> TestMethodAsync(string firstArg)
            {
                return Task.FromResult((object)new
                {
                    test = firstArg
                });
            }
        }
    }
}