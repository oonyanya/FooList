using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection.DataStore;

namespace FooProject.Collection
{
    /// <summary>
    /// 範囲を表す
    /// </summary>
    public interface IRange
    {
        /// <summary>
        /// 開始位置
        /// </summary>
        long start { get; set; }
        /// <summary>
        /// 長さ
        /// </summary>
        long length { get; set; }

        /// <summary>
        /// ディープコピーを行う
        /// </summary>
        /// <returns>複製したクラスのインスタンスを返す</returns>
        IRange DeepCopy();
    }

    /// <summary>
    /// 二次元であらわされた位置から一次元であらわされた位置に変換するためのテーブルです。例えば、行と桁であらわされたカーソルをバイト数であらわすカーソルに変換することができます。
    /// </summary>
    /// <typeparam name="T">IRangeを実装したT</typeparam>
    /// <remarks>連続した範囲でないとうまく動きません</remarks>
    public class BigRangeList<T> : BigList<T> where T : IRange
    {
        /// <summary>
        /// コンストラクター
        /// </summary>
        public BigRangeList() :base()
        {
            var custom = new RangeConverter<T>();
            custom.DataStore = new MemoryPinableContentDataStore<IComposableList<T>>();
            this.LeastFetchStore = custom;
            this.CustomBuilder = custom;
        }

        protected override bool IsAllowDirectUseCollection(IComposableList<T> collection)
        {
            if (collection is FixedRangeList<T>)
                return true;
            throw new NotSupportedException("FixedRangeList以外を使用することはできません");
        }

        /// <summary>
        /// 要素を取得する
        /// </summary>
        /// <param name="index">0から始まる数値</param>
        /// <returns>Tを返す。IRangeインターフェイスのstartの値は変換されない</returns>
        public override T Get(long index)
        {
            var result = GetRawData(index);
            return result;
        }

        /// <summary>
        /// 要素を設定する
        /// </summary>
        /// <param name="index">0から始まる数値</param>
        /// <param name="value">設定したいT</param>
        /// <remarks>valueの値は内部で相対的インデックスに変換されるので、変換する必要はない</remarks>
        public override void Set(long index, T value)
        {
            var args = new BigListArgs<T>(CustomBuilder, LeastFetchStore, this.BlockSize, UpdateType.Overwrite);
            Root.SetAtInPlace(index, value, leafNodeEnumrator, args);
        }

        [Obsolete]
        public T GetIndexIntoRange(long index)
        {
            return GetWithConvertAbsolteIndex(index);
        }
        /// <summary>
        /// 要素を返す
        /// </summary>
        /// <param name="index">0から始まる数値</param>
        /// <returns>Tを返すが、IRangeインターフェイスのstartの値が絶対的な位置に変換される</returns>
        public T GetWithConvertAbsolteIndex(long index)
        {
            var CustomConverter = (RangeConverter<T>)LeastFetchStore;

            var item = GetRawData(index);

            T result = (T)item.DeepCopy();
            result.start = item.start + CustomConverter.customLeastFetch.absoluteIndexIntoRange;
            result.length = item.length;
            return result;
        }

        /// <summary>
        /// 要素を取得する
        /// </summary>
        /// <param name="index">0から始まる数値</param>
        /// <returns>Tを返す。IRangeインターフェイスのstartの値は変換されない</returns>
        public T GetRawData(long index)
        {
            RangeConverter<T> myCustomConverter = (RangeConverter<T>)LeastFetchStore;
            long relativeIndex;
            LeafNode<T> leafNode;
            if (LeastFetchStore.LeastFetch != null)
            {
                relativeIndex = index - LeastFetchStore.LeastFetch.TotalLeftCount;
                if (relativeIndex >= 0 && relativeIndex < LeastFetchStore.LeastFetch.Node.Count)
                {
                    leafNode = (LeafNode<T>)LeastFetchStore.LeastFetch.Node;
                    using (var pinnedContent = CustomBuilder.DataStore.Get(leafNode.container))
                    {
                        var leafNodeItems = pinnedContent.Content;
                        checked
                        {
                            return leafNodeItems[(int)relativeIndex];
                        }
                    }
                }
            }

            relativeIndex = index;
            var node = WalkNode((current, leftCount) => {
                if (relativeIndex < leftCount)
                {
                    return NodeWalkDirection.Left;
                }
                else
                {
                    relativeIndex -= leftCount;
                    var customNode = (IRangeNode)current.Left;
                    myCustomConverter.customLeastFetch.absoluteIndexIntoRange += customNode.TotalRangeCount;
                    return NodeWalkDirection.Right;
                }
            });
            leafNode = (LeafNode<T>)LeastFetchStore.LeastFetch.Node;
            using (var pinnedContent = CustomBuilder.DataStore.Get(leafNode.container))
            {
                var leafNodeItems = pinnedContent.Content;
                checked
                {
                    return leafNodeItems[(int)relativeIndex];
                }
            }
        }

        [Obsolete]
        public long GetIndexFromIndexIntoRange(long indexIntoRange)
        {
            return GetIndexFromAbsoluteIndexIntoRange(indexIntoRange);
        }

        /// <summary>
        /// 絶対的な位置、すなわちインデックスに対応する要素の番号を返す
        /// </summary>
        /// <param name="index">0から始まる数値。絶対的な位置を指定しないといけない</param>
        /// <returns>0から始まる要素の番号。見つからない場合は-1を返す</returns>
        public long GetIndexFromAbsoluteIndexIntoRange(long indexIntoRange)
        {
            RangeConverter<T> myCustomConverter = (RangeConverter<T>)LeastFetchStore;
            long relativeIndexIntoRange = indexIntoRange;

            var node = WalkNode((current, leftCount) => {
                var rangeLeftNode = (IRangeNode)current.Left;
                long leftTotalSumCount = rangeLeftNode.TotalRangeCount;

                if (relativeIndexIntoRange < leftTotalSumCount)
                {
                    return NodeWalkDirection.Left;
                }
                else
                {
                    relativeIndexIntoRange -= leftTotalSumCount;
                    myCustomConverter.customLeastFetch.TotalLeftCount += leftCount;
                    myCustomConverter.customLeastFetch.absoluteIndexIntoRange += leftTotalSumCount;
                    return NodeWalkDirection.Right;
                }
            });

            long relativeIndex, relativeNearIndex;
            var leafNode = (LeafNode<T>)node;
            using (var pinnedContent = CustomBuilder.DataStore.Get(leafNode.container))
            {
                var leafNodeItems = pinnedContent.Content;
                relativeIndex = this.IndexOfNearest(leafNodeItems, relativeIndexIntoRange, out relativeNearIndex);
            }

            if (relativeIndex == -1)
            {
                myCustomConverter.ResetState();
                return -1;
            }
            return relativeIndex + myCustomConverter.customLeastFetch.TotalLeftCount;
        }

        int IndexOfNearest(IList<T> collection,long start, out long nearIndex)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException("indexに負の値を設定することはできません");

            if (start > Int32.MaxValue - 1)
                throw new ArgumentOutOfRangeException("要素数が大きすぎます");

            nearIndex = -1;
            if (collection.Count == 0)
                return -1;

            if (start == 0 && collection.Count > 0)
                return 0;

            T line;
            long lineHeadIndex;

            int left = 0, right = collection.Count - 1, mid;
            while (left <= right)
            {
                mid = (left + right) / 2;
                line = collection[mid];
                lineHeadIndex = line.start;
                if (start >= lineHeadIndex && start < lineHeadIndex + line.length)
                {
                    return mid;
                }
                if (start < lineHeadIndex)
                {
                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }

            System.Diagnostics.Debug.Assert(left >= 0 || right >= 0);
            nearIndex = left >= 0 ? left : right;
            if (nearIndex > collection.Count - 1)
                nearIndex = right;

            return -1;
        }

        /// <summary>
        /// 列挙子を取得する
        /// </summary>
        /// <returns>列挙子を取得する</returns>
        /// <remarks>IRangeインターフェイスのstartの値は変換される</remarks>
        public override IEnumerator<T> GetEnumerator()
        {
            for(int i = 0; i < this.Count; i++)
            {
                yield return GetWithConvertAbsolteIndex(i);
            }
        }
    }

    internal interface IRangeNode
    {
        long TotalRangeCount { get; }
    }

    internal class RangeConcatNode<T> : ConcatNode<T>, IRangeNode where T : IRange
    {

        public RangeConcatNode(Node<T> left, Node<T> right) : base(left, right)
        {
        }

        public long TotalRangeCount { get; private set; }

        protected override void OnNewNode(Node<T> newLeft, Node<T> newRight)
        {
            var customNodeLeft = (IRangeNode)newLeft;
            var customNodeRight = (IRangeNode)newRight;
            if (customNodeLeft != null)
                TotalRangeCount = customNodeLeft.TotalRangeCount;
            if (customNodeRight != null)
                TotalRangeCount += customNodeRight.TotalRangeCount;
        }
    }

    internal class RangeLeafNode<T> : LeafNode<T>, IRangeNode where T: IRange
    {

        public RangeLeafNode() : base()
        {
            TotalRangeCount = 0;
        }

        public RangeLeafNode(long count, IPinableContainer<IComposableList<T>> container) : base(count, container)
        {
        }

        public override void NotifyUpdate(long startIndex, long count, BigListArgs<T> args)
        {
            using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
            {
                var items = pinnedContent.Content;
                this.TotalRangeCount = ProcessItems(items, startIndex, count, this.TotalRangeCount, args);
            }
        }

        private long ProcessItems(IComposableList<T> collection, long index,long count, long oldTotalRangeCount, BigListArgs<T> args)
        {
            switch(args.Type)
            {
                case UpdateType.Overwrite:
                    {
                        int updateStartIndex = (int)index;
                        long newIndexIntoRange = 0;
                        if (index > 0)
                        {
                            newIndexIntoRange = collection[updateStartIndex - 1].start + collection[updateStartIndex - 1].length;
                        }
                        int end = collection.Count - 1;
                        for (int i = updateStartIndex; i <= end; i++)
                        {
                            collection[i].start = newIndexIntoRange;
                            newIndexIntoRange += collection[i].length;
                        }
                        return newIndexIntoRange;

                    }
                case UpdateType.Add:
                    {
                        int updateStartIndex = (int)index;
                        long newIndexIntoRange = 0;
                        if (updateStartIndex > 0)
                            newIndexIntoRange = collection[updateStartIndex - 1].start + collection[updateStartIndex - 1].length;

                        long deltaLength = 0;
                        for(int i = updateStartIndex; i < updateStartIndex + count; i++)
                        {
                            deltaLength += collection[i].length;
                        }

                        for (int i = updateStartIndex; i < collection.Count; i++)
                        {
                            collection[i].start = newIndexIntoRange;
                            newIndexIntoRange += collection[i].length;
                        }

                        return oldTotalRangeCount + deltaLength;
                    }
                case UpdateType.Insert:
                    {
                        int insert_collection_count = (int)count;

                        long deltaLength = 0;
                        for (int i = (int)index; i < index + insert_collection_count; i++)
                        {
                            deltaLength += collection[i].length;
                        }

                        for (int i = (int)index + insert_collection_count; i < collection.Count; i++)
                        {
                            collection[i].start += deltaLength;
                        }

                        int previousIndex = (int)index;
                        long newIndexIntoRange = 0;
                        if (index > 0)
                        {
                            previousIndex--;
                            newIndexIntoRange = collection[previousIndex].start + collection[previousIndex].length;
                        }
                        int end = collection.Count - 1;
                        for (int i = (int)index; i <= end; i++)
                        {
                            collection[i].start = newIndexIntoRange;
                            newIndexIntoRange += collection[i].length;
                        }

                        return oldTotalRangeCount + deltaLength;
                    }
                case UpdateType.Remove:
                    {
                        long deltaLength = 0;
                        if (index < collection.Count)
                        {
                            int updateStartIndex = (int)index;
                            if (index > 0)
                            {
                                deltaLength = collection[updateStartIndex].start - (collection[updateStartIndex - 1].start + collection[updateStartIndex - 1].length);
                            }
                            else
                            {
                                //開始位置は常に0から始まる
                                deltaLength = collection[updateStartIndex].start;
                            }

                            for (int i = updateStartIndex; i < collection.Count; i++)
                            {
                                collection[i].start -= deltaLength;
                            }

                            return oldTotalRangeCount - deltaLength;
                        }
                        else
                        {
                            int lastIndex = collection.Count - 1;
                            return collection[lastIndex].start + collection[lastIndex].length;
                        }
                    }
            }
            return oldTotalRangeCount;
        }

        public long TotalRangeCount { get; private set; }
    }

    internal class RangeLeastFetch<T> : ILeastFetch<T> where T : IRange
    {
        public Node<T> Node { get; set; }

        public long TotalLeftCount { get; set; }

        public long absoluteIndexIntoRange { get; set; }

        public RangeLeastFetch()
        {
        }
    }

    internal class RangeConverter<T> : CustomConverterBase<T> where T : IRange
    {
        public override ILeastFetch<T> LeastFetch { get { return customLeastFetch; } }

        public RangeLeastFetch<T> customLeastFetch { get; set; }

        public override IComposableList<T> CreateList(long init, long max, IEnumerable<T> collection = null)
        {
            var list = new FixedRangeList<T>((int)init, (int)max);
            if (collection != null)
            {
                list.AddRange(collection);
            }
            return list;
        }

        protected override ConcatNode<T> OnCreateConcatNode(Node<T> left, Node<T> right)
        {
            return new RangeConcatNode<T>(left, right);
        }

        protected override LeafNode<T> OnCreateLeafNode()
        {
            return new RangeLeafNode<T>();
        }

        protected override LeafNode<T> OnCreateLeafNode(long count, IPinableContainer<IComposableList<T>> container)
        {
            if (container.Content is FixedRangeList<T>)
            {
                return new RangeLeafNode<T>(count, container);
            }
            throw new NotSupportedException("FixedRangeListを継承したクラスをcontainerのContentに設定する必要があります");
        }

        public override void SetState(Node<T> current, long totalLeftCountInList)
        {
            if (current == null)
            {
                this.customLeastFetch = new RangeLeastFetch<T>();
            }
            else
            {
                this.customLeastFetch.Node = current;
                this.customLeastFetch.TotalLeftCount = totalLeftCountInList;
            }
        }

        public override void ResetState()
        {
            this.customLeastFetch = null;
        }
    }
}
