using Signapse.Data;
using Signapse.Services;

namespace Signapse.Test
{
    public class TestAuthResults : IAuthResults
    {
        public TestAuthResults(bool isUser, AdministratorFlag admin = AdministratorFlag.None)
        {
            this.IsUser = this.IsAuthorized = isUser;

            if (this.IsAuthorized)
            {
                if (admin != AdministratorFlag.None)
                {
                    this.IsAdmin = true;
                }

                if (admin.HasFlag(AdministratorFlag.User))
                {
                    this.IsUsersAdmin = true;
                }

                if (admin.HasFlag(AdministratorFlag.Affiliate))
                {
                    this.IsAffiliatesAdmin = true;
                }
            }
        }

        public bool IsAuthorized { get; }
        public bool IsUser { get; }
        public bool IsAdmin { get; }
        public bool IsUsersAdmin { get; }
        public bool IsAffiliatesAdmin { get; }
    }
}
