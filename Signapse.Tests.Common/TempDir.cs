namespace Signapse.BlockChain.Tests
{
    public class TempDir : IDisposable
    {
        public string Path { get; }

        public TempDir()
        {
            string tmp = System.IO.Path.GetTempFileName();
            File.Delete(tmp);
            Directory.CreateDirectory(tmp);

            this.Path = tmp;
        }

        void IDisposable.Dispose()
        {
            Directory.Delete(this.Path, true);
        }
    }
}