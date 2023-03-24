using Signapse.RequestData;
using Signapse.Services;
using System.Security.Claims;

namespace Signapse.Data
{
    public enum MemberTier
    {
        None,
        Free,
        Level1
    }

    [Flags]
    public enum AdministratorFlag
    {
        None = 0,
        Content = 0x01,
        Member = 0x02,
        User = 0x04,
        Affiliate = 0x08,
        Full = 0xFF
    }

    public class User : IDatabaseEntry
    {
        readonly public static Guid PrimaryAdminGU = Guid.Parse("00000000-0000-0000-0000-000000000001");

        public Guid ID { get; set; } = Guid.Empty;

        public string? Name { get; set; }

        [PolicyAccess]
        public bool RequireChangePassword { get; set; } = false;

        [PolicyAccess]
        public string Email { get; set; } = string.Empty;

        [NoPolicyAccess]
        public string Password { get; set; } = string.Empty;

        [PolicyAccess]
        public bool NewsSubscription { get; set; } = false;

        [PolicyAccess]
        public MemberTier MemberTier { get; set; } = MemberTier.None;

        [PolicyAccess]
        public AdministratorFlag AdminFlags { get; set; } = AdministratorFlag.None;
    }

    public class UserValidator : DatabaseEntryValidator<Data.User>
    {
        public UserValidator(ClaimsPrincipal user, IAuthResults authResults) : base(user, authResults)
        {
        }

        public override bool ValidateInsert(User item)
        {
            if (base.ValidateInsert(item))
            {
                if (!string.IsNullOrWhiteSpace(item.Name)
                    && !string.IsNullOrWhiteSpace(item.Email)
                    && !string.IsNullOrWhiteSpace(item.Password))
                {
                    return true;
                }
                else
                    throw new Exceptions.HttpBadRequest("Invalid User");
            }

            return false;
        }
    }
}
