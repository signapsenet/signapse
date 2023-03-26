using Signapse.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace Signapse.BlockChain
{
    /// <summary>
    /// Stream collections of blocks to and from disk
    /// </summary>
    public class BlockStream : IEnumerable<IBlock>
    {
        readonly JsonSerializerFactory jsonFactory;

        readonly string path;
        DateTimeOffset offset = DateTimeOffset.MinValue;

        public BlockStream(JsonSerializerFactory jsonFactory, string path)
        {
            this.jsonFactory = jsonFactory;
            this.path = path;
        }

        public BlockStream FromTime(DateTimeOffset time)
        {
            this.offset = time;
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<IBlock> GetEnumerator()
        {
            var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                .OrderBy(f => f);

            foreach (var fname in files)
            {
                string yearStr = Path.GetFileName(Path.GetDirectoryName(fname) ?? throw new Exception());
                if (int.TryParse(yearStr, out int year) == false
                    || year < this.offset.Year)
                {
                    continue;
                }

                var blocks = LoadBlocks(fname);
                foreach (var block in blocks)
                {
                    if (block.TimeStamp >= offset)
                    {
                        yield return block;
                    }
                }
            }
        }

        private IBlock[] LoadBlocks(string fname)
        {
            Block[] blocks = { };

            try
            {
                if (File.Exists(fname))
                {
                    using var stream = new FileStream(fname, FileMode.Open, FileAccess.Read);
                    if (jsonFactory.Deserialize<Block[]>(stream) is Block[] jsonBlocks)
                    {
                        blocks = jsonBlocks;
                    }
                }
            }
            catch { }

            return blocks;
        }

        public void Write(IBlock[] blocksToWrite)
        {
            foreach (var g in blocksToWrite.GroupBy(b => b.TimeStamp.Date))
            {
                bool bWasMerged = false;

                DateTimeOffset firstIdx = blocksToWrite.First().TimeStamp;
                DateTimeOffset lastIdx = blocksToWrite.Length > 1 ? blocksToWrite.Last().TimeStamp : DateTimeOffset.MaxValue;

                string fname = Path.Combine(path, g.Key.Year.ToString(), g.Key.ToString("yyyy-MM-dd"));
                var existingBlocks = LoadBlocks(fname);
                
                List<IBlock> mergedBlocks = new List<IBlock>();
                foreach (var block in existingBlocks)
                {
                    if (block.TimeStamp < firstIdx && block.TimeStamp > lastIdx)
                    {
                        mergedBlocks.Add(block);
                    }
                    else
                    {
                        tryMerge();
                    }
                }
                tryMerge();

                if (File.Exists(fname))
                {
                    File.Delete(fname);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(fname) ?? throw new Exception("Invalid Directory"));
                File.WriteAllText(fname, jsonFactory.Serialize(mergedBlocks));

                void tryMerge()
                {
                    if (!bWasMerged)
                    {
                        bWasMerged = true;
                        mergedBlocks.AddRange(g);
                    }
                }
            }
        }
    }
}