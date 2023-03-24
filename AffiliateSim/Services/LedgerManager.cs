using Signapse.BlockChain;
using Signapse.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AffiliateSim.Services
{
    internal class LedgerManager
    {
        readonly Dictionary<Guid, SignapseLedger> ledgers = new Dictionary<Guid, SignapseLedger>();
        readonly IServiceProvider provider;

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
