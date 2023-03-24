using Signapse.Services;

namespace Signapse.Test
{
    public class MockStorage : ISecureStorage, IAppDataStorage
    {
        public Task<string> ReadFile(string fname)
        {
            return Task.FromResult("");
        }

        public Task<string> SecureReadFile(string fname)
        {
            return Task.FromResult("");
        }

        public Task SecureWriteFile(string fname, string content)
        {
            return Task.CompletedTask;
        }

        public Task WriteFile(string fname, string content)
        {
            return Task.CompletedTask;
        }
    }
}