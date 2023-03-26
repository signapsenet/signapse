using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Services;

namespace Signapse.Client.Tests
{
    [TestClass]
    public class WebServerSessionTests : HttpSessionTest<WebServerSession>
    {
        protected override WebServerSession CreateSession()
        {
            var jsonFactory = affiliateServer.WebApp.Services.GetRequiredService<JsonSerializerFactory>();
            return new WebServerSession(jsonFactory, webServer.ServerUri);
        }

        [TestMethod]
        public void WebServerSession_Test()
        {
            Assert.Fail("Incomplete Test");
        }
    }
}