namespace Signapse.Data
{
    public class Member : IDatabaseEntry
    {
        public Guid ID { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public MemberTier MemberTier { get; set; } = MemberTier.None;
        public bool RequireChangePassword { get; set; } = true;
    }
}
