using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;

namespace Signapse.Services
{
    public interface ISecureStorage
    {
        Task<string> ReadFile(string fname);
        Task WriteFile(string fname, string content);
    }

    public class SecureStorage : IDisposable, ISecureStorage
    {
        readonly IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

        public SecureStorage()
        {
        }

        void IDisposable.Dispose()
        {
            isoStore.Dispose();
        }

        public async Task<string> ReadFile(string fname)
        {
            if (isoStore.FileExists(fname))
            {
                using IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(fname, FileMode.Open, isoStore);
                using StreamReader reader = new StreamReader(isoStream);

                return await reader.ReadToEndAsync();
            }
            else
            {
                return string.Empty;
            }
        }

        public async Task WriteFile(string fname, string content)
        {
            using IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(fname, FileMode.Create, isoStore);
            using StreamWriter writer = new StreamWriter(isoStream);

            await writer.WriteAsync(content);
        }
    }
}