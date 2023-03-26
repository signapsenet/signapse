using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Signapse.Services
{
    public interface IAppDataStorage
    {
        Task<string> ReadFile(string fname);
        Task WriteFile(string fname, string content);

        Task<string> SecureReadFile(string fname);
        Task SecureWriteFile(string fname, string content);
    }

    public class AppDataStorage : IDisposable, IAppDataStorage
    {
        private readonly string dirPath;
        private readonly Cryptography crypto;

        public AppDataStorage(Cryptography crypto)
        {
            this.crypto = crypto;

            var appName = Assembly.GetEntryAssembly()?.EntryPoint?.DeclaringType?.Namespace
                ?? throw new Exception("No Entry Assembly Found");

            this.dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName);
            Directory.CreateDirectory(dirPath);
        }

        public void Dispose()
        {
        }

        public async Task<string> SecureReadFile(string fname)
        {
            var path = Path.Combine(dirPath, fname);

            if (File.Exists(path))
            {
                var data = await File.ReadAllTextAsync(path);

                try
                {
                    // If we cannot decrypt, assume the file is not encrypted
                    return crypto.Decrypt(data);
                }
                catch
                {
                    return data;
                }
            }
            else
            {
                return string.Empty;
            }
        }

        public Task SecureWriteFile(string fname, string content)
        {
            var path = Path.Combine(dirPath, fname);
            var data = crypto.Encrypt(content);

            return File.WriteAllTextAsync(path, data);
        }

        public Task<string> ReadFile(string fname)
        {
            var path = Path.Combine(dirPath, fname);
            if (File.Exists(path))
            {
                return File.ReadAllTextAsync(path);
            }
            else
            {
                return Task.FromResult(string.Empty);
            }
        }

        public Task WriteFile(string fname, string content)
        {
            var path = Path.Combine(dirPath, fname);
            return File.WriteAllTextAsync(path, content);
        }
    }
}