using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Signapse.Services
{
#if FALSE
    public class AuthorizationFactory
    {
        readonly IServiceProvider provider;

        public AuthorizationFactory(IServiceProvider provider)
            => this.provider = provider;

        public Task<IAuthResults> Create(ClaimsPrincipal user)
            => AuthResults.Create(provider, user);
    }

    public class AuthResults : IAuthResults
    {
        class EmptyAuthResults : IAuthResults
        {
            public bool IsAuthorized => false;
            public bool IsUser => false;
            public bool IsAdmin => false;
            public bool IsUsersAdmin => false;
            public bool IsAffiliatesAdmin => false;
        }
        readonly static public IAuthResults Empty = new EmptyAuthResults();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private AuthResults() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public bool IsAuthorized => this.User.Succeeded;
        public bool IsUser => this.User.Succeeded;
        public bool IsAdmin => this.Admin.Succeeded;
        public bool IsUsersAdmin => this.UsersAdmin.Succeeded;
        public bool IsAffiliatesAdmin => this.AffiliatesAdmin.Succeeded;

        public AuthorizationResult User { get; private set; }
        public AuthorizationResult Admin { get; private set; }
        public AuthorizationResult UsersAdmin { get; private set; }
        public AuthorizationResult AffiliatesAdmin { get; private set; }

        static internal async Task<IAuthResults> Create(IServiceProvider services, ClaimsPrincipal user)
        {
            var auth = services.GetRequiredService<IAuthorizationService>();
            var policyProvider = services.GetRequiredService<IAuthorizationPolicyProvider>();

            var userPolicy = await policyProvider.GetPolicyAsync(Policies.User)
                ?? throw new Exception("Invalid Policy Name");

            var adminPolicy = await policyProvider.GetPolicyAsync(Policies.Administrator)
                ?? throw new Exception("Invalid Policy Name");

            var usersAdminPolicy = await policyProvider.GetPolicyAsync(Policies.UsersAdministrator)
                ?? throw new Exception("Invalid Policy Name");

            var affiliatesAdminPolicy = await policyProvider.GetPolicyAsync(Policies.AffiliatesAdministrator)
                ?? throw new Exception("Invalid Policy Name");

            return new AuthResults()
            {
                User = await auth.AuthorizeAsync(user, userPolicy),
                Admin = await auth.AuthorizeAsync(user, adminPolicy),
                UsersAdmin = await auth.AuthorizeAsync(user, usersAdminPolicy),
                AffiliatesAdmin = await auth.AuthorizeAsync(user, affiliatesAdminPolicy),
            };
        }
    }
#endif
}