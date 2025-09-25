using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection
{
    public class ReadOnlyComposableList<T> : IComposableList<T>
    {
        List<T> items;

        public T this[int index] { get => items[index]; set => throw new NotImplementedException(); }

        public int Count => this.items.Count;

        public bool IsReadOnly => true;

        public ReadOnlyComposableList(IEnumerable<T> collection)
        {
            if (collection != null)
            {
                items = new List<T>(collection);
            }
            else
            {
                items = new List<T>();
            }
        }

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public void AddRange(IEnumerable<T> collection, int collection_length = -1)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            //VisualStudioでのデバック用に最低限実装しないといけない
            this.items.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < items.Count; i++)
            {
                yield return items[i];
            }
        }

        public IEnumerable<T> GetRange(int index, int count)
        {
            for (int i = index; i < index + count; i++)
            {
                yield return items[i];
            }
        }

        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public void InsertRange(int index, IEnumerable<T> collection, int collection_length = -1)
        {
            throw new NotImplementedException();
        }

        public bool QueryAddRange(IEnumerable<T> collection, int collection_length = -1)
        {
            return false;
        }

        public bool QueryInsertRange(int index, IEnumerable<T> collection, int collection_length = -1)
        {
            return false;
        }

        public bool QueryRemoveRange(int index, int count)
        {
            return false;
        }

        public bool QueryUpdate(int index, T item)
        {
            return false;
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public void RemoveRange(int index, int count)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
