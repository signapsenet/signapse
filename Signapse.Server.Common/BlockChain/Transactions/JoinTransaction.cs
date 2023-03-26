using System;

namespace Signapse.BlockChain.Transactions
{
    public class JoinTransaction : ITransaction
    {
        public TransactionType TransactionType => TransactionType.JoinAffiliate;
        public Guid AffiliateID_1 { get; set; }
        public Guid AffiliateID_2 { get; set; }

        public Guid JoinRequestID { get; set; } = Guid.Empty;

        public JoinTransaction() { }
        public JoinTransaction(Guid a, Guid b) => (AffiliateID_1, AffiliateID_2) = (a, b);
    }
}
