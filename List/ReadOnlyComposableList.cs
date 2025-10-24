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

        /// <inheritdoc/>
        public T this[int index] { get => items[index]; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public int Count => this.items.Count;

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void AddRange(IEnumerable<T> collection, int collection_length = -1)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void Clear()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void CopyTo(T[] array, int arrayIndex)
        {
            //VisualStudioでのデバック用に最低限実装しないといけない
            this.items.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < items.Count; i++)
            {
                yield return items[i];
            }
        }

        /// <inheritdoc/>
        public IEnumerable<T> GetRange(int index, int count)
        {
            for (int i = index; i < index + count; i++)
            {
                yield return items[i];
            }
        }

        /// <inheritdoc/>
        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void InsertRange(int index, IEnumerable<T> collection, int collection_length = -1)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public bool QueryAddRange(IEnumerable<T> collection, int collection_length = -1)
        {
            return false;
        }

        /// <inheritdoc/>
        public bool QueryInsertRange(int index, IEnumerable<T> collection, int collection_length = -1)
        {
            return false;
        }

        /// <inheritdoc/>
        public bool QueryRemoveRange(int index, int count)
        {
            return false;
        }

        /// <inheritdoc/>
        public bool QueryUpdate(int index, T item)
        {
            return false;
        }

        /// <inheritdoc/>
        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
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
