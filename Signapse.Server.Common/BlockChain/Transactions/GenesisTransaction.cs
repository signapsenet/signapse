namespace Signapse.BlockChain.Transactions
{
    public class GenesisTransaction : ITransaction
    {
        public TransactionType TransactionType => TransactionType.Genesis;
    }
}