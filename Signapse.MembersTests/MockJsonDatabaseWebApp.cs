using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Signapse.Server;
using Signapse.Server.Common.Services;
using Signapse.Services;
using System.Net;

namespace Signapse.Tests
{
    internal class MockJsonDatabaseWebApp : MockWebApp
    {
        public MockJsonDatabaseWebApp(Action<IServiceCollection>? services = null, Action<WebApplication>? app = null)
            : base(s => InitServices(s, services), a => InitApplication(a, app))
        {
        }

        private static void InitServices(IServiceCollection services, Action<IServiceCollection>? additional)
        {
            services.AddAuthentication("cookie")
                .AddCookie("cookie", opts =>
                {
                    opts.Events.OnRedirectToLogin = ctx =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/api") &&
                            ctx.Response.StatusCode == (int)HttpStatusCode.OK)
                        {
                            ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        }
                        else
                        {
                            ctx.Response.Redirect(ctx.RedirectUri);
                        }
                        return Task.FromResult(0);
                    };
                    opts.LoginPath = PathString.Empty;
                });

            services.AddAuthorization();
            services.AddSignapseAuthorization("cookie");

            services.AddSingleton<JsonDatabase<Data.User>>()
                .AddTransient<ISecureStorage, TestStorage>();

            additional?.Invoke(services);
        }

        private static void InitApplication(WebApplication app, Action<WebApplication>? additional)
        {
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapGet("/api/v1/login/{id}", async ctx =>
            {
                var id = ctx.Request.RouteValues["id"] as string;
                var user = new Data.User();

                //if (id == "0" || id == JsonDatabaseTests.USER_ID_1.ToString())
                //{
                //    user = new Data.User()
                //    {
                //        ID = JsonDatabaseTests.USER_ID_1,
                //        AdminFlags = AdministratorFlag.User
                //    };
                //}
                //else if (id == "1" || id == JsonDatabaseTests.USER_ID_2.ToString())
                //{
                //    user = new Data.User()
                //    {
                //        ID = JsonDatabaseTests.USER_ID_2
                //    };
                //}

                await ctx.SignInAsync(Claims.CreatePrincipal(user, "cookie"));
            });

            app.MapDatabaseEndpoint<Data.User, Data.UserValidator>("/api/v1/user");

            additional?.Invoke(app);
        }

        public async Task AttemptLogin(uint id = 0)
        {
            using var res = await this.SendRequest(HttpMethod.Get, $"/api/v1/login/{id}");
        }

        public Task<T?> SendTestRequest<T>(HttpMethod method, Data.User? data)
        {
            return SendRequest<T>(method, "/api/v1/user", data);
        }

        public Task<HttpResponseMessage> SendTestRequest(HttpMethod method, Data.User? data)
        {
            return SendRequest(method, "/api/v1/user", data);
        }

        public Task<HttpResponseMessage> SendTestRequest(HttpMethod method, string args)
        {
            return SendRequest(method, $"/api/v1/user/{args.Trim('/')}", null);
        }
    }

    public class TestStorage : ISecureStorage
    {
        public Task<string> ReadFile(string fname)
        {
            return Task.FromResult("");
        }

        public Task WriteFile(string fname, string content)
        {
            return Task.CompletedTask;
        }
    }
}