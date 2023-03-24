namespace Signapse.BlockChain
{
    public class MemberTransaction : ITransaction
    {
        public TransactionType TransactionType => TransactionType.Member;
    }
}