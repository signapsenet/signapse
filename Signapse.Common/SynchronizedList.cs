using System.Collections.Generic;
using System.Linq;

namespace Signapse
{
    public class SynchronizedList<T> : IList<T>, IReadOnlyList<T>
    {
        private readonly List<T> _list = new List<T>();

        public SynchronizedList()
        {
            SyncRoot = ((System.Collections.ICollection)_list).SyncRoot;
        }

        public object SyncRoot { get; }

        public int Count
        {
            get
            {
                lock (_list)
                    return _list.Count;
            }
        }

        public bool IsReadOnly => ((ICollection<T>)_list).IsReadOnly;

        public void Add(T item)
        {
            lock (SyncRoot)
                _list.Add(item);
        }

        public void Clear()
        {
            lock (SyncRoot)
                _list.Clear();
        }

        public bool Contains(T item)
        {
            lock (SyncRoot)
                return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (SyncRoot)
                _list.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            lock (SyncRoot)
                return _list.Remove(item);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            lock (SyncRoot)
                return _list.ToList().GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            lock (SyncRoot)
                return _list.ToList().GetEnumerator();
        }

        public T this[int index]
        {
            get
            {
                lock (SyncRoot)
                    return _list[index];
            }
            set
            {
                lock (SyncRoot)
                    _list[index] = value;
            }
        }

        public int IndexOf(T item)
        {
            lock (SyncRoot)
                return _list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            lock (SyncRoot)
                _list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            lock (SyncRoot)
                _list.RemoveAt(index);
        }
    }
}
