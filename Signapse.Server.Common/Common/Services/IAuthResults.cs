namespace Signapse.Services
{
    public interface IAuthResults
    {
        public bool IsAuthorized { get; }
        public bool IsUser { get; }
        public bool IsAdmin { get; }
        public bool IsUsersAdmin { get; }
        public bool IsAffiliatesAdmin { get; }
    }
}