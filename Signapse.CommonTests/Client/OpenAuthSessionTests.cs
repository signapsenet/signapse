using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Server.Extensions;
using Signapse.Server.Tests;

namespace Signapse.Client.Tests
{
    [TestClass]
    public class OpenAuthSessionTests
    {
        [TestMethod]
        public async Task Invalid_Authentication_Fails()
        {
            using CancellationTokenSource ctSource = new CancellationTokenSource();
            using TestWebServer webServer1 = new TestWebServer();
            webServer1.Run(ctSource.Token);

            using TestWebServer webServer2 = new TestWebServer();
            webServer2.Run(ctSource.Token);

            using OpenAuthHttpClient session = new OpenAuthHttpClient(webServer1.ServerUri, webServer2.ServerUri, string.Empty);

            bool authorized = await session.Authorize();
            Assert.IsFalse(authorized);
        }

        [TestMethod]
        public async Task Can_Authenticate()
        {
            const string email = "user@admin.com";
            const string password = "password";

            using CancellationTokenSource ctSource = new CancellationTokenSource();
            using TestWebServer webServer1 = new TestWebServer();
            webServer1.Run(ctSource.Token);

            using TestWebServer webServer2 = new TestWebServer();
            webServer2.Run(ctSource.Token);
            webServer2.AddMember(email, password);

            using OpenAuthHttpClient session = new OpenAuthHttpClient(webServer1.ServerUri, webServer2.ServerUri, string.Empty);
            await session.Login(email, password);

            bool authorized = await session.Authorize();
            Assert.IsTrue(authorized);
        }
    }
}