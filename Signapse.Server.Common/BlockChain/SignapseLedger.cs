using System.Collections.Generic;
using System.Linq;

namespace Signapse.BlockChain
{
    public class SignapseLedger
    {
        private static readonly IBlock GenesisBlock;

        private class GenesisTransaction : ITransaction { TransactionType ITransaction.TransactionType => TransactionType.Genesis; }
        static SignapseLedger()
        {
            var block = new Block()
            {
                Transaction = new GenesisTransaction()
            };
            block.Forge(new Block());

            GenesisBlock = block;
        }

        private readonly SynchronizedList<IBlock> transactions = new SynchronizedList<IBlock>();

        public SignapseLedger()
        {
            transactions.Add(GenesisBlock);
        }

        public IBlock LastBlock => transactions.Last();

        public IReadOnlyList<IBlock> Transactions
        {
            get
            {
                lock (transactions.SyncRoot)
                {
                    return transactions;
                }
            }
        }

        public bool Add(IBlock transaction)
        {
            lock (transactions)
            {
                if (transactions.Count == 0
                    || LastBlock.BlockHash == transaction.PrevBlockHash
                    || transaction.Transaction?.TransactionType is TransactionType.JoinAffiliate)
                {
                    transactions.Add(new Block(transaction));
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void Remove(IBlock transaction) => transactions.Remove(transaction);
    }
}