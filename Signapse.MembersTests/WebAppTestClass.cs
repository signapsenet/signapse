using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Signapse.Tests
{
    public abstract class WebAppTestClass
    {
        private CancellationTokenSource ctSource;
        private WebApplicationBuilder builder;
        private WebApplication app;

        protected WebAppTestClass()
        {
            ctSource = new CancellationTokenSource();
            builder = WebApplication.CreateBuilder();
            app = builder.Build();
        }

        protected virtual void InitServices(IServiceCollection services)
        {
        }

        protected virtual void InitApplication(WebApplication app)
        {
        }

        [TestInitialize]
        public void Init()
        {
            builder = WebApplication.CreateBuilder();
            InitServices(builder.Services);

            app = builder.Build();
            InitApplication(app);

            app.StartAsync(ctSource.Token);
        }

        [TestCleanup]
        public void Cleanup()
        {
            ctSource.Cancel();
            ctSource.Dispose();
        }

        public WebAppConnection CreateConnection()
        {
            return new WebAppConnection(app.Services.CreateScope());
        }
    }

    public class WebAppConnection : IDisposable
    {
        private readonly IServiceScope scope;

        public WebAppConnection(IServiceScope scope)
        {
            this.scope = scope;
        }

        void IDisposable.Dispose()
        {
            scope.Dispose();
        }
    }
}