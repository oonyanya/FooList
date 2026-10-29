using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection;

namespace FooProject.Collection
{
    public class BigRleArrayRangeItem<T> : IRleArrayRangeItem
    {
        public T Value { get; set; }
        public long start { get; set; }
        public long length { get; set; }

        public BigRleArrayRangeItem()
        {
        }

        public BigRleArrayRangeItem(T v, long index, long length)
        {
            this.Value = v;
            this.start = index;
            this.length = length;
            this.OnInit(v, index, length);
        }

        public BigRleArrayRangeItem(T v, long length)
        {
            this.Value = v;
            this.length = length;
            this.OnInit(v, start, length);
        }

        public BigRleArrayRangeItem(T v)
        {
            this.Value = v;
            this.length = 1;
            this.OnInit(v, start, 1);
        }

        /// <summary>
        /// 初期化時に呼び出される
        /// </summary>
        /// <param name="v"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        public virtual void OnInit(T v, long index, long length)
        {
        }

        public IRange DeepCopy()
        {
            var new_item = new BigRleArrayRangeItem<T>();
            new_item.start = start;
            new_item.Value = Value;
            new_item.length = length;
            return new_item;
        }

        public override bool Equals(object obj)
        {
            var other = obj as BigRleArrayRangeItem<T>;
            if (other == null) return false;
            if (other.Value.Equals(this.Value) && other.length == this.length && other.start == this.start) return true;
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool EqualsValue(IRleArrayRangeItem other)
        {
            BigRleArrayRangeItem<T> other_item = (BigRleArrayRangeItem<T>)other;
            return this.Value.Equals(other_item.Value);
        }
    }

    public class BigRleArrayCollection<J> : BigRleArrayCollectionBase<BigRleArrayRangeItem<J>>
    {
        public BigRleArrayCollection(int block_size = 0) : base(block_size) { }

        protected override BigRleArrayRangeItem<J> CreateItem(BigRleArrayRangeItem<J> value, long start = -1, long length = -1)
        {
            if (start == -1)
            {
                return new BigRleArrayRangeItem<J>(value.Value, length);
            }
            else
            {
                return new BigRleArrayRangeItem<J>(value.Value, start, length);
            }
        }

        public void AddRange(J item,int count = -1)
        {
            if (count == -1)
                this.AddRange(new BigRleArrayRangeItem<J>(item, 1));
            else
                this.AddRange(new BigRleArrayRangeItem<J>(item,count));
        }

        public void InsertRange(int index,J item, int count = -1)
        {
            if (count == -1)
                this.InsertRange(index, new BigRleArrayRangeItem<J>(item, 1));
            else
                this.InsertRange(index, new BigRleArrayRangeItem<J>(item, count));
        }
        public void UpdateRange(int index, J item, int count = -1)
        {
            if (count == -1)
                this.UpdateRange(index, new BigRleArrayRangeItem<J>(item, 1));
            else
                this.UpdateRange(index, new BigRleArrayRangeItem<J>(item, count));
        }
    }
}
