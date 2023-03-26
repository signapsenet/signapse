using System;

namespace Signapse.BlockChain
{
    public class Signature
    {
        public Guid BlockID { get; set; }
        public Guid AffiliateID { get; set; } = Guid.Empty;
        public string Data { get; set; } = string.Empty;
    }
}