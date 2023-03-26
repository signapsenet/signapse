using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Services;

namespace Signapse.Client.Tests
{
    [TestClass()]
    public class AffiliateSessionTests : HttpSessionTest<AffiliateSession>
    {
        protected override AffiliateSession CreateSession()
        {
            var jsonFactory = affiliateServer.WebApp.Services.GetRequiredService<JsonSerializerFactory>();
            return new AffiliateSession(jsonFactory, affiliateServer.ServerUri, Guid.Empty, "");
        }

        [TestMethod]
        public void AffiliateSession_Test()
        {
            Assert.Fail("Incomplete Test");
        }
    }
}