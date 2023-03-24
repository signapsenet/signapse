using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;

namespace Signapse.Tests
{
    abstract public class WebAppTestClass
    {
        CancellationTokenSource ctSource;
        WebApplicationBuilder builder;
        WebApplication app;

        protected WebAppTestClass()
        {
            ctSource = new CancellationTokenSource();
            builder = WebApplication.CreateBuilder();
            app = builder.Build();
        }

        virtual protected void InitServices(IServiceCollection services)
        {
        }

        virtual protected void InitApplication(WebApplication app)
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
        readonly IServiceScope scope;
        
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