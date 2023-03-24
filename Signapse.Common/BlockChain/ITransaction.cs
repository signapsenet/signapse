namespace Signapse.BlockChain
{
    public enum TransactionType
    {
        Genesis,
        Member,
        SubscriptionFee,
        Content,
        JoinAffiliate,
    }

    public interface ITransaction
    {
        TransactionType TransactionType { get; }
    }
}