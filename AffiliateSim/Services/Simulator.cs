using Microsoft.Extensions.DependencyInjection;
using Signapse.BlockChain;
using Signapse.Server.Affiliate;
using System.Text;

namespace AffiliateSim.Services
{
    public class SampleTransaction : ITransaction
    {
        public TransactionType TransactionType => TransactionType.Content;
    }

    internal class Simulator : IDisposable
    {
        private readonly LedgerManager ledgerMgr;
        private readonly IServiceProvider provider;
        private readonly List<AffiliateInstance> affiliates = new List<AffiliateInstance>();
        private readonly CancellationTokenSource ctSource = new CancellationTokenSource();

        public IReadOnlyList<AffiliateInstance> Affiliates => affiliates;

        public Simulator(IServiceProvider provider, LedgerManager ledgerMgr)
        {
            this.ledgerMgr = ledgerMgr;
            this.provider = provider;
        }

        public AffiliateServer CreateServer()
        {
            var server = ActivatorUtilities.CreateInstance<AffiliateServer>(provider);

            server.Run(ctSource.Token);

            return server;
        }

        public AffiliateInstance GenerateAffiliate()
        {
            var descriptor = new AffiliateDescriptor()
            {
                Name = $"Affiliate {CreateName(5)}"
            };

            var res = new AffiliateInstance(ledgerMgr, descriptor);
            affiliates.Add(res);
            return res;
        }

        public void Dispose()
        {
            while (affiliates.Count > 0)
            {
                RemoveAffiliate(affiliates.First());
            }

            ctSource.Cancel();
            ctSource.Dispose();
        }

        public void RemoveAffiliate(Guid id) => RemoveAffiliate(affiliates.FirstOrDefault(a => a.Descriptor.ID == id));
        public void RemoveAffiliate(AffiliateInstance? affiliate)
        {
            if (affiliate != null)
            {
                affiliates.Remove(affiliate);
                affiliate.Dispose();
            }
        }

        private const string CHARACTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private Random random = new Random();
        private string CreateName(int length = 8)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                sb.Append(CHARACTERS[random.Next(CHARACTERS.Length)]);
            }

            return sb.ToString();
        }
    }
}