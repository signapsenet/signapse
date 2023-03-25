using Microsoft.AspNetCore.Builder;
using Signapse.Middleware;
using Signapse.Server;
using Signapse.Server.Affiliate;
using Signapse.Server.Middleware;
using System.Linq;
using System.Net.Http;

namespace Signapse
{
    public class LocalAffiliateServer : AffiliateServer
    {
        public LocalAffiliateServer(string[] args) : base(args, false)
        {
        }

        protected override void ConfigureEndpoints(WebApplication app)
        {
            base.ConfigureEndpoints(app);

            // TODO: Use the new mustache template mapping
            //app.MapMustacheEndpoint<IndexDataModel>("/index.html", config => { });
            //app.UseMustacheTemplates();

            app.MapWebRequestHandler<InstallPageHandler, InstallDataModel>(HttpMethod.Post, "/api/v1/install", (handler, installData) => handler.ProcessInstallation(installData));

            app.MapWebRequestHandler<IndexPageHandler, LoginRequestData>(HttpMethod.Post, "/api/v1/login", (handler, user) => handler.ProcessLogin(user));
            app.MapWebRequestHandler<IndexPageHandler, Data.User>(HttpMethod.Post, "/api/v1/logout", (handler) => handler.ProcessLogout());

            app.MapWebRequestHandler<AdminPageHandler, AdminDataModel>(HttpMethod.Put, "/api/v1/admin/siteConfig", (handler, adminData) => handler.ProcessSiteConfig(adminData));
            app.MapWebRequestHandler<AdminPageHandler, AdminDataModel>(HttpMethod.Put, "/api/v1/admin/addAffiliate", (handler, adminData) => handler.AddAffiliate(adminData));
            app.MapWebRequestHandler<AdminPageHandler, AdminDataModel>(HttpMethod.Get, "/api/v1/admin/generateAPIKey", (handler) => handler.GenerateAPIKey());
            app.MapWebRequestHandler<AdminPageHandler, AdminDataModel>(HttpMethod.Post, "/api/v1/admin/acceptAllRequests", (handler) => handler.AcceptAllRequests());
            app.MapWebRequestHandler<AdminPageHandler, AdminDataModel>(HttpMethod.Post, "/api/v1/admin/rejectAllRequests", (handler) => handler.RejectAllRequests());

            app.MapWebRequestHandler<ServerPageHandler, AffiliateJoinRequest>(HttpMethod.Put, "/api/v1/server/add_join_request", (handler, adminData) => handler.AddJoinRequest(adminData));
            app.MapWebRequestHandler<ServerPageHandler, ServerDataModel>(HttpMethod.Get, "/api/v1/server/desc", (handler) => handler.GetDescriptor());
        }
    }
}