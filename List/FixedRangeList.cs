using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection;
using Microsoft.VisualBasic;
using Slusser.Collections.Generic;

namespace FooProject.Collection
{
    public class FixedRangeList<T> : FixedList<T>, IEnumerable<T>
        where T : IRange
    {
        protected int stepRow = -1, stepLength = 0;
        protected const int STEP_ROW_IS_NONE = -1;

        public FixedRangeList(int init,int max):base(init,max) { }

        public override T this[int index]
        {
            get
            {
                return this.collection[index];
            }
            set
            {
                var oldValue = this.collection[index];

                var newValue = value;

                this.collection[index] = newValue;

                long deltaLength = newValue.length - oldValue.length;

                this.TotalCount += deltaLength;

                for (int i = index + 1; i < this.collection.Count; i++)
                {
                    this.collection[i].start += deltaLength;
                }
            }
        }

        public long TotalCount
        {
            get; private set;
        }

        public override void Add(T item)
        {
            this.AddRange(new T[1] { item }, 1);
        }

        public override void AddRange(IEnumerable<T> collection, int collection_length = -1)
        {
            long deltaLength = 0;
            foreach (var item in collection)
            {
                deltaLength += item.length;
            }

            int updateStartIndex = this.collection.Count;

            long newIndexIntoRange = this.TotalCount;

            this.TotalCount += deltaLength;

            base.AddRange(collection,collection_length);

            for (int i = updateStartIndex; i < this.collection.Count; i++)
            {
                this.collection[i].start = newIndexIntoRange;
                newIndexIntoRange += this.collection[i].length;
            }
        }

        public override void Insert(int index, T item)
        {
            this.InsertRange(index, new T[1] { item }, 1);
        }

        public override void InsertRange(int index, IEnumerable<T> collection, int collection_length = -1)
        {
            long deltaLength = 0;
            foreach (var item in collection)
            {
                deltaLength += item.length;
            }

            this.TotalCount += deltaLength;

            for (int i = index; i < this.collection.Count; i++)
            {
                this.collection[i].start += deltaLength;
            }

            base.InsertRange(index, collection, collection_length);

            int previousIndex = index;
            long newIndexIntoRange = 0;
            if (index > 0)
            {
                previousIndex = index - 1;
                newIndexIntoRange = this.collection[index - 1].start + this.collection[index - 1].length;
            }
            int insert_collection_count;
#if NET8_0_OR_GREATER
            if (collection.TryGetNonEnumeratedCount(out insert_collection_count) == false)
            {
                insert_collection_count = collection.Count();
            }
#else
            insert_collection_count = collection.Count();
#endif
            int end = index + insert_collection_count - 1;
            for (int i = index; i <= end; i++)
            {
                this.collection[i].start = newIndexIntoRange;
                newIndexIntoRange += this.collection[i].length;
            }
        }

        public override void RemoveRange(int index,int count)
        {
            long deltaLength = 0;
            foreach (var item in collection.GetRage(index,count))
            {
                deltaLength += item.length;
            }

            this.TotalCount -= deltaLength;

            int updateStartIndex = index + count;
            for (int i = updateStartIndex; i < this.collection.Count; i++)
            {
                this.collection[i].start -= deltaLength;
            }

            base.RemoveRange(index, count);
        }

        public override void Clear()
        {
            base.Clear();
            this.stepRow = STEP_ROW_IS_NONE;
            this.stepLength = 0;
            this.TotalCount = 0;
            //DebugLog.WriteLine("Clear");
        }

        public void UpdateStartIndex(int deltaLength, int startRow)
        {
            if (this.collection.Count == 0)
            {
                this.stepRow = STEP_ROW_IS_NONE;
                this.stepLength = 0;
                return;
            }

            if (this.stepRow == STEP_ROW_IS_NONE)
            {
                this.stepRow = startRow;
                this.stepLength = deltaLength;
                return;
            }


            if (startRow < this.stepRow)
            {
                //ドキュメントの後半部分をごっそり削除した場合、this.stepRow >= this.Lines.Countになる可能性がある
                if (this.stepRow >= this.collection.Count)
                    this.stepRow = this.collection.Count - 1;
                for (int i = this.stepRow; i > startRow; i--)
                    this.collection[i].start -= this.stepLength;
            }
            else if (startRow > this.stepRow)
            {
                for (int i = this.stepRow + 1; i < startRow; i++)
                    this.collection[i].start += this.stepLength;
            }

            this.stepRow = startRow;
            this.stepLength += deltaLength;
        }

        /// <summary>
        /// 今までの変更をすべて反映させる
        /// </summary>
        public void CommiteChange()
        {
            for (int i = this.stepRow + 1; i < this.collection.Count; i++)
                this.collection[i].start += this.stepLength;

            this.stepRow = STEP_ROW_IS_NONE;
            this.stepLength = 0;
        }

        /// <summary>
        /// 当該行の先頭インデックスを取得する
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public long GetLineHeadIndex(int row)
        {
            if (this.collection.Count == 0)
                return 0;
            if (this.stepRow != STEP_ROW_IS_NONE && row > this.stepRow)
                return this.collection[row].start + this.stepLength;
            else
                return this.collection[row].start;
        }
    }

}
