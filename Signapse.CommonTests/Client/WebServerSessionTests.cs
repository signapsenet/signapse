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
    [TestClass]
    public class WebServerSessionTests : HttpSessionTest<WebServerSession>
    {
        protected override WebServerSession CreateSession()
        {
            var jsonFactory = server.WebApp.Services.GetRequiredService<JsonSerializerFactory>();
            return new WebServerSession(jsonFactory, server.ServerUri);
        }

        [TestMethod]
        public void WebServerSession_Test()
        {
            Assert.Fail("Incomplete Test");
        }
    }
}