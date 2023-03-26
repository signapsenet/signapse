using Signapse.BlockChain.Transactions;
using Signapse.RequestData;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signapse.BlockChain
{
    public class Block : IBlock, IAffiliateRequest
    {
        static public Block Genesis => new Block()
        {
            TimeStamp = DateTimeOffset.Now,
            Transaction = new GenesisTransaction()
        };

        public Guid ID { get; set; } = Guid.NewGuid();
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.Now;
        public Signature[] Signatures { get; set; } = { };

        public string BlockHash { get; set; } = string.Empty;
        public string PrevBlockHash { get; set; } = string.Empty;

        [JsonConverter(typeof(TransactionConverter))]
        public ITransaction? Transaction { get; set; } = null;

        public Block() { }
        public Block(IBlock copy)
        {
            this.ID = copy.ID;
            this.TimeStamp = copy.TimeStamp;
            this.Signatures = copy.Signatures.ToArray();
            this.BlockHash = copy.BlockHash;
            this.PrevBlockHash = copy.PrevBlockHash;
            this.Transaction = copy.Transaction;
        }

        public void Forge(IBlock? prev)
        {
            if (prev?.BlockHash != null)
            {
                this.PrevBlockHash = prev.BlockHash;
                this.BlockHash = prev.BlockHash;
            }

            this.BlockHash += this.TimeStamp.ToString() + (this.Transaction?.Serialize() ?? string.Empty);
            this.BlockHash = this.BlockHash.ToMD5_2();
        }

        public override int GetHashCode() => HashCode.Combine(ID, TimeStamp, BlockHash);

        public override bool Equals(object? obj)
        {
            return obj is IBlock block
                && block.TimeStamp == this.TimeStamp
                && block.BlockHash == this.BlockHash
                && block.ID == this.ID;
        }
    }
}