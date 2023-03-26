using Microsoft.AspNetCore.Builder;
using Signapse.Middleware;
using Signapse.Server.Middleware;
using Signapse.Server.Web;
using System.Net.Http;

namespace Signapse.Web
{
    class LocalWebServer : WebServer
    {
#if DEBUG
        public WebApplication WebApp => this.webApp;
#endif

        public LocalWebServer(string[] args, bool anyPort = true) : base(args, anyPort)
        {
        }

        protected override void ConfigureEndpoints(WebApplication app)
        {
            base.ConfigureEndpoints(app);

            // TODO: Replace this with our actual index.html, and move all content to common embedded resources
            app.MapMustacheEndpoint<IndexPageData>("/index.html");

            app.UseMustacheTemplates();
        }
    }
}
