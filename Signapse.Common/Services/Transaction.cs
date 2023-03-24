using Signapse.Data;
using System.Collections;

namespace Signapse.Services
{
    public class Transaction<T> : IDisposable, IEnumerable<T>
        where T : class, IDatabaseEntry
    {
        readonly JsonDatabase<T> db;

        bool commitChanges = true;
        
        List<T> inserted = new List<T>();
        HashSet<Guid> deleted = new HashSet<Guid>();
        Dictionary<Guid, T> updated = new Dictionary<Guid, T>();

        public Transaction(JsonDatabase<T> db)
        {
            this.db = db;
        }

        public void Dispose()
        {
            Commit().Wait();
        }

        public void Rollback()
        {
            updated.Clear();
            inserted.Clear();
            deleted.Clear();
        }

        public T? this[Guid id] => db[id]?.Clone();

        public void Delete(Guid id) => deleted.Add(id);

        public void Insert(T item) => inserted.Add(item);

        public void Update(T item) => updated[item.ID] = item;

        public async Task Commit()
        {
            lock (db)
            {
                db.Items.RemoveAll(it => deleted.Contains(it.ID));
                db.Items.AddRange(inserted);

                for (int i = 0; i < db.Items.Count; i++)
                {
                    var item = db.Items[i];
                    if (updated.TryGetValue(item.ID, out var it))
                    {
                        db.Items.Insert(i, it);
                        db.Items.Remove(item);
                    }
                }
            }

            await db.Save();
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (db)
            {
                return db.Items
                    .Where(it => deleted.Contains(it.ID) == false)
                    .Concat(inserted)
                    .Select(it =>
                    {
                        if (updated.TryGetValue(it.ID, out var res))
                            return res;
                        else
                            return it.Clone();
                    })
                    .GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
