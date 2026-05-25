using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection
{
    public class BigRleArrayRange<T> : IRleArrayRange<T>
    {
        public T Value { get; set; }
        public long start { get; set; }
        public long length { get; set; }

        public BigRleArrayRange()
        {
        }

        public BigRleArrayRange(T v, long index, long length)
        {
            this.Value = v;
            this.start = index;
            this.length = length;
            this.OnInit(v, index, length);
        }

        public BigRleArrayRange(T v, long length)
        {
            this.Value = v;
            this.length = length;
            this.OnInit(v, start, length);
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
            var new_item = new BigRleArrayRange<T>();
            new_item.start = start;
            new_item.Value = Value;
            new_item.length = length;
            return new_item;
        }

        public override bool Equals(object obj)
        {
            var other = obj as BigRleArrayRange<T>;
            if (other == null) return false;
            if (other.Value.Equals(this.Value) && other.length == this.length && other.start == this.start) return true;
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class BigRleArray<T> : BigRleArrayBase<T>
    {
        protected override IRleArrayRange<T> CreateItem(T value, long start = -1, long length = -1)
        {
            if (start == -1)
            {
                return new BigRleArrayRange<T>(value, length);
            }
            else
            {
                return new BigRleArrayRange<T>(value, start, length);
            }
        }
    }
}
