using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection
{
    public class ReadOnlyBigList<T> : IReadOnlyList<T>, IReadOnlyCollection<T>, IList<T>
    {
        public virtual int Count { get; }

        public bool IsReadOnly => true;

        public virtual T this[int index] { get => throw new NotImplementedException(); set => throw new NotSupportedException(); }


        public virtual int IndexOf(T item)
        {
            int index = 0;
            foreach (T x in this)
            {
                if (EqualityComparer<T>.Default.Equals(x, item))
                {
                    return index;
                }
                ++index;
            }

            // didn't find any item that matches.
            return -1;
        }

        public virtual bool Contains(T item)
        {
            return (IndexOf(item) >= 0);
        }

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            int count = this.Count;

            if (count == 0)
                return;

            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex must not be negative");
            if (arrayIndex >= array.Length || count > array.Length - arrayIndex)
                throw new ArgumentException("array too small");

            int index = arrayIndex, i = 0;
            foreach (T item in this)
            {
                if (i >= count)
                    break;

                array[index] = item;
                ++index;
                ++i;
            }
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            for(int i = 0; i < Count ; ++i)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
                yield return this[i];
        }

        public virtual void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        public virtual void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public virtual void Add(T item)
        {
            throw new NotSupportedException();
        }

        public virtual void Clear()
        {
            throw new NotSupportedException();
        }

        public virtual bool Remove(T item)
        {
            throw new NotSupportedException();
        }
    }
}
