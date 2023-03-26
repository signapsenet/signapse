using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Signapse.Data;
using Signapse.Server.Web;
using Signapse.Server.Web.Services;
using Signapse.Services;
using System.Threading;
using System.Threading.Tasks;

namespace Signapse.Web
{
    class TestWebServer : WebServer
    {
        readonly public TestSignapseServer signapseServer;
        public WebApplication WebApp => this.webApp;

        public TestWebServer(string[] args) : base(args, true)
        {
            signapseServer = new TestSignapseServer();
            signapseServer.Run(CancellationToken.None);

            var webConfig = this.WebApp.Services.GetRequiredService<WebServerConfig>();
            webConfig.SignapseServerUri = signapseServer.ServerUri;
            webConfig.SignapseServerAPIKey = TestSignapseServer.API_KEY;
        }

        public override void Run(CancellationToken token)
        {
            base.Run(token);

            signapseServer.Descriptor.WebServerUri = this.ServerUri;
        }

        protected override void ConfigureDependencies(IServiceCollection services)
        {
            services.AddTransient<ISecureStorage, TestStorage>();
            services.AddTransient<IAppDataStorage, TestStorage>();
            services.AddTransient<IconBuilder>();

            base.ConfigureDependencies(services);
        }

        protected override void ConfigureEndpoints(WebApplication app)
        {
            base.ConfigureEndpoints(app);

            app.MapGet("/images/logo.png", getLogo);

            async Task getLogo(HttpContext context, IconBuilder builder)
            {
                var data = builder.LogoImageData(this.ServerUri.ToString());
                
                context.Response.ContentType = "image/png";
                await context.Response.Body.WriteAsync(data);
            }
        }

        public void AddAffiliate(SignapseServerDescriptor descriptor)
        {
            this.signapseServer.AddAffiliate(descriptor);
        }

        public void AddAffiliate(TestWebServer server)
        {
            this.signapseServer.AddAffiliate(server.signapseServer.Descriptor);
        }
    }
}