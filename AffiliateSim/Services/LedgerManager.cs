using Signapse.BlockChain;

namespace AffiliateSim.Services
{
    internal class LedgerManager
    {
        private readonly Dictionary<Guid, SignapseLedger> ledgers = new Dictionary<Guid, SignapseLedger>();
        private readonly IServiceProvider provider;

        public LedgerManager(IServiceProvider provider)
        {
            this.provider = provider;
        }

        public SignapseLedger Create(Guid id)
        {
            if (ledgers.TryGetValue(id, out var ledger) == false)
            {
                ledger = new SignapseLedger();
                ledgers.Add(id, ledger);
            }

            return ledger;
        }

        public void Remove(Guid id)
        {
            this.ledgers.Remove(id);
        }
    }
}
