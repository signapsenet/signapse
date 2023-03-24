using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.BlockChain;
using Signapse.BlockChain.Transactions;
using Signapse.Services;
using Signapse.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signapse.BlockChain.Tests
{
    [TestClass()]
    public class BlockStreamTests : DITestClass
    {
        public override void InitServices(ServiceCollection services)
        {
            services.AddTransient<JsonSerializerFactory>();
            services.AddTransient<BlockStream>();
        }

        static BlockStreamTests()
        {
            Genesis.Forge(null);
        }

        readonly static Block Genesis = new Block()
        {
            TimeStamp = new DateTimeOffset(new DateTime(2000, 12, 29, 0, 0, 0).ToUniversalTime()),
            Transaction = new GenesisTransaction(),
        };

        static IReadOnlyList<IBlock> BLOCKS = new List<IBlock>()
        {
            new Block(){
                ID = Guid.NewGuid(),
                TimeStamp = new DateTimeOffset(new DateTime(2000, 12, 30, 0, 0, 0).ToUniversalTime()),
                Transaction = new MemberTransaction(),
            },
            new Block(){
                ID = Guid.NewGuid(),
                TimeStamp = new DateTimeOffset(new DateTime(2000, 12, 30, 0, 0, 1).ToUniversalTime()),
                Transaction = new MemberTransaction(),
            },
            new Block(){
                ID = Guid.NewGuid(),
                TimeStamp = new DateTimeOffset(new DateTime(2000, 12, 31, 0, 0, 0).ToUniversalTime()),
                Transaction = new MemberTransaction(),
            },
            new Block(){
                ID = Guid.NewGuid(),
                TimeStamp = new DateTimeOffset(new DateTime(2000, 12, 31, 0, 0, 1).ToUniversalTime()),
                Transaction = new MemberTransaction(),
            },
            new Block(){
                ID = Guid.NewGuid(),
                TimeStamp = new DateTimeOffset(new DateTime(2001, 01, 01, 0, 0, 0).ToUniversalTime()),
                Transaction = new MemberTransaction(),
            },
            new Block(){
                ID = Guid.NewGuid(),
                TimeStamp = new DateTimeOffset(new DateTime(2001, 01, 01, 0, 0, 0).ToUniversalTime()),
                Transaction = new MemberTransaction(),
            },
        };

        [TestMethod]
        public void WriteForDay()
        {
            using var dir = new TempDir();

            var jsonFactory = scope.ServiceProvider.GetRequiredService<JsonSerializerFactory>();
            var stream = new BlockStream(jsonFactory, dir.Path);
            stream.Write(BLOCKS.Take(2).ToArray());

            var blocks = stream.ToList();
            Assert.AreEqual(2, blocks.Count);
            Assert.AreEqual(BLOCKS[0].ID, blocks[0].ID);
            Assert.AreEqual(BLOCKS[1].ID, blocks[1].ID);
        }

        [TestMethod]
        public void WriteAcrossDays()
        {
            using var dir = new TempDir();

            var jsonFactory = scope.ServiceProvider.GetRequiredService<JsonSerializerFactory>();
            var stream = new BlockStream(jsonFactory, dir.Path);
            stream.Write(BLOCKS.Take(4).ToArray());
            Assert.AreEqual(1, Directory.GetDirectories(dir.Path).Count());
            Assert.AreEqual(2, Directory.EnumerateFiles(dir.Path, "*", SearchOption.AllDirectories).Count());

            var blocks = stream.ToList();
            Assert.AreEqual(4, blocks.Count);
            Assert.AreEqual(BLOCKS[2].ID, blocks[2].ID);
            Assert.AreEqual(BLOCKS[3].ID, blocks[3].ID);
        }

        [TestMethod]
        public void WriteAcrossYears()
        {
            using var dir = new TempDir();

            var jsonFactory = scope.ServiceProvider.GetRequiredService<JsonSerializerFactory>();
            var stream = new BlockStream(jsonFactory, dir.Path);
            stream.Write(BLOCKS.Take(6).ToArray());
            Assert.AreEqual(2, Directory.GetDirectories(dir.Path).Count());
            Assert.AreEqual(3, Directory.EnumerateFiles(dir.Path, "*", SearchOption.AllDirectories).Count());

            var blocks = stream.ToList();
            Assert.AreEqual(6, blocks.Count);
            Assert.AreEqual(BLOCKS[4].ID, blocks[4].ID);
            Assert.AreEqual(BLOCKS[5].ID, blocks[5].ID);
        }

        [TestMethod]
        public void ReadWithOffset()
        {
            using var dir = new TempDir();

            var jsonFactory = scope.ServiceProvider.GetRequiredService<JsonSerializerFactory>();
            var stream = new BlockStream(jsonFactory, dir.Path);
            stream.Write(BLOCKS.Take(6).ToArray());

            var blocks = stream.FromTime(BLOCKS[2].TimeStamp).ToList();
            Assert.AreEqual(4, blocks.Count);
            Assert.AreEqual(BLOCKS[2].ID, blocks[0].ID);
        }

        [TestMethod]
        public void GenerateBlockChain()
        {
            using var dir = new TempDir();

            var jsonFactory = scope.ServiceProvider.GetRequiredService<JsonSerializerFactory>();
            var stream = new BlockStream(jsonFactory, dir.Path);

            List<Block> origBlocks = new List<Block>() { Genesis };

            foreach (IBlock block in BLOCKS)
            {
                var b = new Block(block);
                b.Forge(origBlocks.Last());

                origBlocks.Add(b);
            }

            stream.Write(origBlocks.ToArray());

            var newBlocks = stream.ToList();
            Assert.AreEqual(origBlocks.Serialize(), newBlocks.Serialize());
        }
    }
}