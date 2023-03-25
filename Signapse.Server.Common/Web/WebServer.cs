using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Signapse.Data;
using Signapse.Server.Common;
using Signapse.Server.Common.Services;
using Signapse.Server.Extensions;
using Signapse.Server.Middleware;
using Signapse.Server.Web.Services;
using Signapse.Services;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Signapse.Server.Web
{
    public class WebServer : ServerBase
    {
        public WebServer(string[] args, bool anyPort = true) : base(args, anyPort)
        {
        }

        protected override void ConfigureDependencies(IServiceCollection services)
        {
            services
                .AddCors(options =>
                {
                    options.DefaultPolicyName = "oauth";
                    options.AddPolicy(name: "oauth", policy =>
                    {
                        policy.AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials()
                            .SetIsOriginAllowed(origin => true);
                    });
                })
                .AddSingleton(provider =>
                 {
                     var storage = provider.GetRequiredService<IAppDataStorage>();
                     var jsonFactory = provider.GetRequiredService<JsonSerializerFactory>();
                     var webConfig = new WebServerConfig(storage, jsonFactory);
                     webConfig.Load();

                     return webConfig;
                 })
                .AddSignapseContentProvider()
                .AddSignapseOpenAuth();
        }

        protected override void ConfigureEndpoints(WebApplication app)
        {
            app.UseHttpExceptionHandler();
            app.UseHttpsRedirection();

            app.UseCors("oauth");
            app.UseAuthentication();
            app.UseAuthorization();

            app.Use(async (context, next) =>
            {
                var corsPolicyProvider = context.RequestServices.GetRequiredService<ICorsPolicyProvider>();
                var contentProvider = context.RequestServices.GetRequiredService<ContentProvider>();
                var corsPolicy = await corsPolicyProvider.GetPolicyAsync(context, "oauth")
                    ?? throw new System.Exception("CORS policy not found");

                lock (corsPolicy)
                {
                    if (corsPolicy.Origins.Count != contentProvider.Affiliates.Count)
                    {
                        corsPolicy.Origins.Clear();

                        foreach (var desc in contentProvider.Affiliates)
                        {
                            var url = desc.Uri.ToString();
                            if (corsPolicy.Origins.Contains(url) == false)
                            {
                                corsPolicy.Origins.Add(url);
                            }
                        }
                    }
                }

                await next(context);
            });

            app.UseDefaultPath("index.html");
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseSignapseOpenAuth();

            app.MapGet("/api/v1/content", async ctx =>
            {
                var contentProvider = ctx.RequestServices.GetRequiredService<ContentProvider>();
                await ctx.Response.WriteAsJsonAsync(contentProvider.CurrentContent);
            });

            app.MapPost("/api/v1/logout", handleLogout);

            async Task handleLogout(HttpContext context, IOptionsMonitor<CookieAuthenticationOptions> cookieOptions)
            {
                await context.SignOutAsync(AuthenticationSchemes.MemberCookie);
                if (cookieOptions.Get(AuthenticationSchemes.MemberCookie)?.Cookie?.Name is string cookieName)
                {
                    context.Response.Cookies.Delete(cookieName);
                }
            }
        }
    }
}