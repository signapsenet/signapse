using System.Runtime.CompilerServices;

namespace Signapse.BlockChain
{
    public interface IBlock
    {
        Guid ID { get; }
        DateTimeOffset TimeStamp { get; }
        Signature[] Signatures { get; }

        string BlockHash { get; }
        string PrevBlockHash { get; }
        ITransaction? Transaction { get; }
    }

    static public class BlockExtensions
    {
        static public bool IsValid(this IBlock block, IBlock prevBlock)
        {
            var newBlock = new Block(block);
            newBlock.Forge(prevBlock);

            return block.PrevBlockHash == prevBlock.BlockHash
                && block.BlockHash == newBlock.BlockHash;
        }
    }
}