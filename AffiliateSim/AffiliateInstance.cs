using AffiliateSim.Services;
using Signapse.BlockChain;
using Signapse.BlockChain.Transactions;
using Signapse.Services;
using System.Diagnostics;
using System.Transactions;

namespace AffiliateSim
{
    public class AffiliateDescriptor
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
    }

    internal class AffiliateInstance : IDisposable
    {
        readonly LedgerManager ledgerMgr;
        public AffiliateDescriptor Descriptor { get; }
        public SignapseLedger Ledger { get; }

        readonly HashSet<AffiliateInstance> allAffiliates = new HashSet<AffiliateInstance>();
        readonly CancellationTokenSource ctSource = new CancellationTokenSource();
        readonly Random rand = new Random();

        public IReadOnlySet<AffiliateInstance> AllAffiliates => allAffiliates;

        public AffiliateInstance(LedgerManager mgr, AffiliateDescriptor descriptor)
        {
            this.ledgerMgr = mgr;
            this.Ledger = mgr.Create(descriptor.ID);

            this.Descriptor = descriptor;
            this.allAffiliates.Add(this);

            var token = ctSource.Token;
            Task.Run(async () =>
            {
                while (token.IsCancellationRequested == false)
                {
                    await Task.Delay(rand.Next(500, 5000), token);

                    lock (this.Ledger)
                    {
                        SubmitTransaction(CreateRandomTransaction(this));
                    }
                }
            }, ctSource.Token);
        }

        public Block CreateRandomTransaction(AffiliateInstance affiliate)
        {
            var block = new Block()
            {
                Transaction = new SampleTransaction()
            };
            block.Forge(affiliate.Ledger.LastBlock);

            return block;
        }

        public override string ToString() => Descriptor.Name;

        // TODO: Before calling this, either the "other" affiliate, or a consensus of affiliates, need to accept
        // and sign off on the leave operation
        public void LeaveAffiliation()
        {
            foreach (var affiliate in this.allAffiliates.Where(a => a != this))
            {
                this.allAffiliates.Remove(affiliate);
                affiliate.allAffiliates.Remove(this);
            }
        }

        // TODO: Before calling this, require a consensus of affiliates (from current network and other network)
        // to accept and sign off on the join operation
        public void JoinAffiliate(AffiliateInstance other)
        {
            HashSet<AffiliateInstance> processed = new HashSet<AffiliateInstance>() { this, other };

            Queue<AffiliateInstance> affiliates = new Queue<AffiliateInstance>();
            affiliates.Enqueue(this);
            affiliates.Enqueue(other);

            while (affiliates.Count > 0)
            {
                var affiliate = affiliates.Dequeue();

                this.allAffiliates.Add(affiliate);

                foreach (var a in affiliate.allAffiliates)
                {
                    if (processed.Add(a))
                    {
                        affiliates.Enqueue(a);
                    }
                }
            }

            foreach (var affiliate in this.allAffiliates)
            {
                affiliate.allAffiliates.UnionWith(this.allAffiliates);
            }

            this.SubmitTransaction(this.CreateJoinTransaction(this, other));
        }

        public void Dispose()
        {
            ctSource.Cancel();
            ctSource.Dispose();

            ledgerMgr.Remove(this.Descriptor.ID);
        }

        private bool VerifySignatures(IBlock transaction)
        {
            // TODO: Use the public key to validate each signature
            return true;
        }

        private bool ValidateTransaction(IBlock transaction)
        {
            if (VerifySignatures(transaction))
            {
                if (transaction.Transaction is JoinTransaction jt
                    && transaction.Signatures.Any(s => s.AffiliateID == jt.AffiliateID_1)
                    && transaction.Signatures.Any(s => s.AffiliateID == jt.AffiliateID_2)
                )
                {
                    return true;
                }

                var block = new Block(transaction);
                block.Forge(this.Ledger.LastBlock);

                return block.BlockHash == transaction.BlockHash;
            }

            return false;
        }

        public Block CreateJoinTransaction(AffiliateInstance a, AffiliateInstance b)
        {
            var lastBlockA = a.Ledger.LastBlock;
            var lastBlockB = b.Ledger.LastBlock;
            var maxBlock = lastBlockA.TimeStamp < lastBlockB.TimeStamp ? lastBlockB : lastBlockA;

            var block = new Block()
            {
                Transaction = new JoinTransaction(a.Descriptor.ID, b.Descriptor.ID),
            };
            block.Forge(maxBlock);

            return block;
        }

        private Signature? SignTransaction(IBlock transaction)
        {
            if (transaction.Transaction is JoinTransaction jt)
            {
                if (jt.AffiliateID_1 == this.Descriptor.ID || jt.AffiliateID_2 == this.Descriptor.ID)
                {
                    return new Signature()
                    {
                        AffiliateID = this.Descriptor.ID,
                        BlockID = transaction.ID,
                        Data = "AAA"
                    };
                }
            }
            else if (ValidateTransaction(transaction))
            {
                return new Signature()
                {
                    AffiliateID = this.Descriptor.ID,
                    BlockID = transaction.ID,
                    Data = "AAA"
                };
            }

            return null;
        }

        // TODO: Attempt to resolve failed transactions
        internal List<Block> FailedTransactions { get; } = new List<Block>();

        private void SubmitTransaction(Block transaction)
        {
            if (false == this.SignTransaction(transaction) is Signature sig)
            {
                FailedTransactions.Add(transaction);
                return;
            }

            transaction.Signatures = new[] { sig };

            uint failedCount = 0;
            var affiliates = this.AllAffiliates.ToArray();
            foreach (var affiliate in affiliates)
            {
                if (!affiliate.Ledger.Add(transaction))
                {
                    failedCount++;
                }
            }

            int consensusThreshold = Math.Max(affiliates.Length, affiliates.Length / 2);
            if (failedCount >= consensusThreshold)
            {
                ResyncLedger();
                this.FailedTransactions.Add(transaction);
            }
        }

        private void ResyncLedger()
        {
            while (true)
            {
                this.Ledger.Remove(this.Ledger.LastBlock);

                var affiliateTransactions = this.AllAffiliates
                    .Where(a => a != this)
                    .Randomize()
                    .Take(3)
                    .Select(a => a.GetTransactionsAfter(this.Ledger.LastBlock.BlockHash))
                    .ToArray();

                if (affiliateTransactions.Length > 0)
                {
                    // Ensure we only get the transactions that are agreed upon
                    var verifiedTransactions = affiliateTransactions.First();
                    for (var i = 1; i < affiliateTransactions.Length; i++)
                    {
                        var len = Math.Min(verifiedTransactions.Length, affiliateTransactions[i].Length);
                        verifiedTransactions = affiliateTransactions[i].Take(len)
                            .Zip(verifiedTransactions.Take(len))
                            .TakeWhile(a => a.First.BlockHash == a.Second.BlockHash)
                            .Select(a => a.First)
                            .ToArray();
                    }

                    // If we have agreement, write these transactions to our list
                    if (verifiedTransactions.Length > 0)
                    {
                        foreach (var trn in verifiedTransactions)
                        {
                            // add the transaction to the block chain
                            this.Ledger.Add(trn);
                        }
                        break;
                    }
                }

                if (this.Ledger.Transactions.Count == 0)
                {
                    throw new Exception("Syncronizations from the Genesis block failed.");
                }
            }
        }

        private IBlock[] GetTransactionsAfter(string blockHash) => this.Ledger
            .Transactions
            .SkipWhile(b => b.BlockHash != blockHash)
            .Skip(1)
            .Select(b => new Block(b))
            .ToArray();
    }
}
