using Signapse.Data;
using Signapse.Services;
using System;
using System.Security.Claims;

namespace Signapse.Server
{
    public enum AffiliateStatus
    {
        Waiting,
        Accepted,
        Rejected
    }

    public class AffiliateJoinRequest : IDatabaseEntry
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public Guid FromServerID { get; set; } = Guid.Empty;
        public Guid ToServerID { get; set; } = Guid.Empty;
        public SignapseServerDescriptor Descriptor { get; set; } = new SignapseServerDescriptor();

        public AffiliateStatus Status { get; set; } = AffiliateStatus.Waiting;
    }

    public class AffiliateJoinRequestValidator : DatabaseEntryValidator<AffiliateJoinRequest>
    {
        private SignapseServerDescriptor affiliateDesc;

        public AffiliateJoinRequestValidator(ClaimsPrincipal user, IAuthResults authResults, SignapseServerDescriptor self) : base(user, authResults)
        {
            this.affiliateDesc = self;
        }

        public override bool ValidateUpdate(AffiliateJoinRequest item)
        {
            if (!authResults.IsAffiliatesAdmin)
            {
                throw new Exceptions.HttpUnauthorized();
            }

            return base.ValidateUpdate(item);
        }

        public override bool ValidateInsert(AffiliateJoinRequest item)
        {
            if (!authResults.IsAffiliatesAdmin)
            {
                throw new Exceptions.HttpUnauthorized();
            }

            return base.ValidateInsert(item);
        }

        public override bool ValidateDelete(AffiliateJoinRequest item)
        {
            if (!authResults.IsAffiliatesAdmin)
            {
                throw new Exceptions.HttpUnauthorized();
            }

            return base.ValidateDelete(item);
        }
    }
}
