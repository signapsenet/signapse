using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Server.Tests;

namespace Signapse.Client.Tests
{
    abstract public class HttpSessionTest<T>
        where T : HttpSession
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        CancellationTokenSource ctSource;
        protected TestServer server;
        protected T session;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        abstract protected T CreateSession();

        [TestInitialize]
        public void Initialize()
        {
            ctSource = new CancellationTokenSource();
            server = new TestServer();
            server.Run(ctSource.Token);

            session = CreateSession();
        }

        [TestCleanup]
        public void Cleanup()
        {
            session.Dispose();

            ctSource.Cancel();
            server.WaitForShutdown();

            server.Dispose();
            ctSource.Dispose();
        }
   }
}