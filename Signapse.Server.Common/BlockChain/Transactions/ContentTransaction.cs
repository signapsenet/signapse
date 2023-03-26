using Signapse.Data;
using System;

namespace Signapse.BlockChain.Transactions
{
    public interface ISignapseContent
    {
        Guid AffiliateID { get; }
        string Network { get; }

        string Title { get; }
        string Description { get; }
        string PreviewImage { get; }
    }

    public class ContentTransaction : ISignapseContent, ITransaction
    {
        public TransactionType TransactionType => TransactionType.Content;
        public Guid ID { get; set; } = Guid.NewGuid();

        public Guid AffiliateID { get; set; }
        public string Network { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PreviewImage { get; set; } = string.Empty;
    }
}