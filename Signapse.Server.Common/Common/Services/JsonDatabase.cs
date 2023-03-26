using Signapse.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signapse.Services
{
    public class JsonDatabase<T> : IDisposable
        where T : IDatabaseEntry
    {
        private readonly JsonSerializerFactory jsonFactory;
        private readonly SemaphoreSlim saveSem = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private readonly IAppDataStorage storage;
        public List<T> Items { get; }

        public JsonDatabase(IAppDataStorage storage, JsonSerializerFactory jsonFactory)
        {
            this.storage = storage;
            this.jsonFactory = jsonFactory;

            this.Items = LoadItems();
        }

        private List<T> LoadItems()
        {
            var json = storage.SecureReadFile($"{typeof(T).Name}.json").Result;
            if (!string.IsNullOrEmpty(json))
            {
                return jsonFactory.Deserialize<List<T>>(json) ?? new List<T>();
            }

            return new List<T>();
        }

        public T? this[Guid id] => this.Items.FirstOrDefault(it => it.ID == id);

        public SemaphorSlimLock Lock() => new SemaphorSlimLock(semaphore);

        public async Task Save()
        {
            try
            {
                await saveSem.WaitAsync();
                await storage.SecureWriteFile($"{typeof(T).Name}.json", jsonFactory.Serialize(this.Items));
            }
            finally
            {
                saveSem.Release();
            }
        }

        void IDisposable.Dispose()
        {
            semaphore.Dispose();
        }
    }
}
