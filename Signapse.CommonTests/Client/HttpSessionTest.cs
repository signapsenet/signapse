using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Server.Tests;
using Signapse.Server.Web.Services;

namespace Signapse.Client.Tests
{
    public abstract class HttpSessionTest<T>
        where T : HttpSession
    {
        protected const string API_KEY = "api_key";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private CancellationTokenSource ctSource;
        protected TestWebServer webServer;
        protected TestAffiliateServer affiliateServer;
        protected T session;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        protected abstract T CreateSession();

        [TestInitialize]
        public void Initialize()
        {
            ctSource = new CancellationTokenSource();
            webServer = new TestWebServer();
            webServer.Run(ctSource.Token);

            affiliateServer = new TestAffiliateServer();
            affiliateServer.Run(ctSource.Token);
            affiliateServer.Descriptor.WebServerUri = webServer.ServerUri;

            var webConfig = webServer.WebApp.Services.GetRequiredService<WebServerConfig>();
            webConfig.SignapseServerUri = affiliateServer.ServerUri;
            webConfig.SignapseServerAPIKey = API_KEY;

            session = CreateSession();
        }

        [TestCleanup]
        public void Cleanup()
        {
            session.Dispose();

            ctSource.Cancel();
            webServer.WaitForShutdown();
            affiliateServer.WaitForShutdown();

            webServer.Dispose();
            affiliateServer.Dispose();
            ctSource.Dispose();
        }
    }
}