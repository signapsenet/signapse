using Signapse.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public T? this[Guid id]
        {
            get
            {
                if (updated.TryGetValue(id, out var updatedItem))
                {
                    return updatedItem;
                }
                else if (inserted.FirstOrDefault(i => i.ID == id) is T insertedItem)
                {
                    return insertedItem;
                }
                else if (db[id]?.Clone() is T clonedItem)
                {
                    updated[id] = clonedItem;
                    return clonedItem;
                }
                else
                {
                    return default(T);
                }
            }
        }

        public void Delete(Guid id)
        {
            inserted.RemoveAll(it => it.ID == id);
            updated.Remove(id);
            deleted.Add(id);
        }

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

                deleted.Clear();
                inserted.Clear();
                updated.Clear();
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
