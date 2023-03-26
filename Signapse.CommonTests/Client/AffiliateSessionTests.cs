using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Client;
using Signapse.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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