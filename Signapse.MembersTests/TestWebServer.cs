using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Signapse.Server.Web;
using Signapse.Services;
using Signapse.Test;

namespace Signapse.Server.Tests
{
    public class TestWebServer : WebServer
    {
#if DEBUG
        public WebApplication WebApp => this.webApp;
#endif

        public TestWebServer() : base(new string[0])
        {
        }

        protected override void ConfigureDependencies(IServiceCollection services)
        {
            services
                .AddSingleton<IAppDataStorage, MockStorage>()
                .AddSingleton<ISecureStorage, MockStorage>();

            base.ConfigureDependencies(services);
        }

        protected override void ConfigureEndpoints(WebApplication app)
        {
            base.ConfigureEndpoints(app);
        }

        public void AddMember(string email, string password)
        {
            var hasher = this.webApp.Services.GetRequiredService<PasswordHasher<Data.Member>>();
            var dbMember = this.webApp.Services.GetRequiredService<JsonDatabase<Data.Member>>();

            var member = new Data.Member()
            {
                Name = email,
                Email = email,
                MemberTier = Data.MemberTier.Free
            };
            member.Password = hasher.HashPassword(member, password);

            dbMember.Items.Add(member);
        }
    }
}