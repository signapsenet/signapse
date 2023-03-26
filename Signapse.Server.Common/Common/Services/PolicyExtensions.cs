using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Signapse.Server.Common.Services
{
    public static class PolicyExtensions
    {
        public static IServiceCollection AddSignapseAuthorization(this IServiceCollection services, params string[] authSchemes)
        {
            services.AddAuthorization(config =>
            {
                config.AddPolicy(Policies.User, new AuthorizationPolicyBuilder()
                    .RequireClaim(Claims.UserID)
                    .RequireRole(Roles.User)
                    .AddAuthenticationSchemes(authSchemes)
                    .Build());

                config.AddPolicy(Policies.Member, new AuthorizationPolicyBuilder()
                    .RequireClaim(Claims.UserID)
                    .RequireRole(Roles.Member)
                    .AddAuthenticationSchemes(authSchemes)
                    .Build());

                config.AddPolicy(Policies.Administrator, new AuthorizationPolicyBuilder()
                    .RequireClaim(Claims.UserID)
                    .RequireRole(Roles.Administrator)
                    .AddAuthenticationSchemes(authSchemes)
                    .Build());

                config.AddPolicy(Policies.UsersAdministrator, new AuthorizationPolicyBuilder()
                    .RequireClaim(Claims.UserID)
                    .RequireRole(Roles.Administrator, Roles.UsersAdministrator)
                    .AddAuthenticationSchemes(authSchemes)
                    .Build());

                config.AddPolicy(Policies.ContentAdministrator, new AuthorizationPolicyBuilder()
                    .RequireClaim(Claims.UserID)
                    .RequireRole(Roles.Administrator, Roles.ContentAdministrator)
                    .AddAuthenticationSchemes(authSchemes)
                    .Build());

                config.AddPolicy(Policies.MembersAdministrator, new AuthorizationPolicyBuilder()
                    .RequireClaim(Claims.UserID)
                    .RequireRole(Roles.Administrator, Roles.MembersAdministrator)
                    .AddAuthenticationSchemes(authSchemes)
                    .Build());

                config.AddPolicy(Policies.AffiliatesAdministrator, new AuthorizationPolicyBuilder()
                    .RequireClaim(Claims.UserID)
                    .RequireRole(Roles.Administrator, Roles.AffiliatesAdministrator)
                    .AddAuthenticationSchemes(authSchemes)
                    .Build());
            });

            return services;
        }
    }
}