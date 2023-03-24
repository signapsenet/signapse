using Signapse.Data;
using System.Security.Claims;

namespace Signapse
{
    public static class Claims
    {
        public const string UserID = "http://www.signapse.net/api/v1/claims/signapse-user";
        public const string Expiry = "http://www.signapse.net/api/v1/claims/signapse-expiry";
        public const string MemberTier = "http://www.signapse.net/api/v1/claims/signapse-member-tier";

        static Dictionary<AdministratorFlag, string> ClaimMappings = new Dictionary<AdministratorFlag, string>()
        {
            { AdministratorFlag.Content, Roles.ContentAdministrator },
            { AdministratorFlag.Member, Roles.MembersAdministrator },
            { AdministratorFlag.User, Roles.UsersAdministrator },
            { AdministratorFlag.Affiliate, Roles.AffiliatesAdministrator },
        };

        static public ClaimsPrincipal CreatePrincipal(Data.User user, string authenticationType)
        {
            var identity = new ClaimsIdentity(authenticationType);
            identity.AddClaim(new Claim(UserID, user.ID.ToString()));

            if (user.MemberTier == Signapse.Data.MemberTier.None)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, Roles.User));
            }
            else
            {
                identity.AddClaim(new Claim(MemberTier, user.MemberTier.ToString()));
                identity.AddClaim(new Claim(ClaimTypes.Role, Roles.Member));
            }

            if (ClaimMappings.Keys.All(f => user.AdminFlags.HasFlag(f)))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, Roles.Administrator));
            }
            else
            {
                foreach (var kv in ClaimMappings)
                {
                    if (user.AdminFlags.HasFlag(kv.Key))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, kv.Value));
                    }
                }
            }

            return new ClaimsPrincipal(identity);
        }
    }
}
