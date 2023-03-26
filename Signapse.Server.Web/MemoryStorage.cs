using Signapse.Services;
using System.Threading.Tasks;

namespace Signapse.Web
{
    class TestStorage : ISecureStorage, IAppDataStorage
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