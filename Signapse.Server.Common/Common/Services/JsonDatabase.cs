using Signapse.Data;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Signapse.Services
{
    public class JsonDatabase<T> : IDisposable
        where T : IDatabaseEntry
    {
        readonly JsonSerializerFactory jsonFactory;
        readonly SemaphoreSlim saveSem = new SemaphoreSlim(1, 1);
        readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        readonly IAppDataStorage storage;
        public List<T> Items { get; }

        public JsonDatabase(IAppDataStorage storage, JsonSerializerFactory jsonFactory)
        {
            this.storage = storage;
            this.jsonFactory = jsonFactory;

            this.Items = LoadItems();
        }

        List<T> LoadItems()
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

        async public Task Save()
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
