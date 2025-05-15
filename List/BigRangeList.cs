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
    /// 範囲変換テーブル
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
            custom.DataStore = new MemoryPinableContentDataStore<FixedList<T>>();
            this.CustomConverter = custom;
            this.CustomBuilder = custom;
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
        public override void Set(long index, T value)
        {
            var args = new BigListArgs<T>(CustomBuilder, CustomConverter, this.BlockSize);
            Root.SetAtInPlace(index, value, args);
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
            return CustomConverter.ConvertBack(GetRawData(index));
        }

        /// <summary>
        /// 要素を取得する
        /// </summary>
        /// <param name="index">0から始まる数値</param>
        /// <returns>Tを返す。IRangeインターフェイスのstartの値は変換されない</returns>
        public T GetRawData(long index)
        {
            RangeConverter<T> myCustomConverter = (RangeConverter<T>)CustomConverter;
            long relativeIndex;
            LeafNode<T> leafNode;
            if (CustomConverter.LeastFetch != null)
            {
                relativeIndex = index - CustomConverter.LeastFetch.TotalLeftCount;
                if (relativeIndex >= 0 && relativeIndex < CustomConverter.LeastFetch.Node.Count)
                {
                    leafNode = (LeafNode<T>)CustomConverter.LeastFetch.Node;
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
            leafNode = (LeafNode<T>)CustomConverter.LeastFetch.Node;
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
            RangeConverter<T> myCustomConverter = (RangeConverter<T>)CustomConverter;
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
        public RangeConcatNode(ConcatNode<T> node) : base(node)
        {
        }
        public RangeConcatNode(Node<T> left, Node<T> right) : base(left, right)
        {
            var customNodeLeft = (IRangeNode)left;
            var customNodeRight = (IRangeNode)right;
            TotalRangeCount = customNodeLeft.TotalRangeCount + customNodeRight.TotalRangeCount;
        }

        public long TotalRangeCount { get; private set; }

        protected override Node<T> NewNodeInPlace(Node<T> newLeft, Node<T> newRight)
        {
            var customNodeLeft = (IRangeNode)newLeft;
            var customNodeRight = (IRangeNode)newRight;
            if (customNodeLeft != null)
                TotalRangeCount = customNodeLeft.TotalRangeCount;
            if (customNodeRight != null)
                TotalRangeCount += customNodeRight.TotalRangeCount;
            return base.NewNodeInPlace(newLeft, newRight);
        }
    }

    internal class RangeLeafNode<T> : LeafNode<T>, IRangeNode where T: IRange
    {

        public RangeLeafNode() : base()
        {
            TotalRangeCount = 0;
        }

        public RangeLeafNode(long count, PinableContainer<FixedList<T>> container) : base(count, container)
        {
        }

        public override void NotifyUpdate(long startIndex, long count, BigListArgs<T> args)
        {
            using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
            {
                var items = pinnedContent.Content;
                var fixedRangeList = (FixedRangeList<T>)items;
                TotalRangeCount = fixedRangeList.TotalCount;
            }
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

    internal class RangeConverter<T> : ICustomConverter<T>,ICustomBuilder<T> where T : IRange
    {
        public IPinableContainerStore<FixedList<T>> DataStore { get; set; }

        public ILeastFetch<T> LeastFetch { get { return customLeastFetch; } }

        public RangeLeastFetch<T> customLeastFetch { get; set; }

        public T Convert(T item)
        {
            var result = item;
            result.start -= customLeastFetch.absoluteIndexIntoRange;
            return result;
        }

        public T ConvertBack(T item)
        {
            T result = (T)item.DeepCopy();
            result.start = item.start + customLeastFetch.absoluteIndexIntoRange;
            result.length = item.length;
            return result;
        }

        public FixedList<T> CreateList(long init, long max)
        {
            return new FixedRangeList<T>((int)init, (int)max);
        }

        public ConcatNode<T> CreateConcatNode(ConcatNode<T> node)
        {
            return new RangeConcatNode<T>(node);
        }

        public ConcatNode<T> CreateConcatNode(Node<T> left, Node<T> right)
        {
            return new RangeConcatNode<T>(left, right);
        }

        public LeafNode<T> CreateLeafNode(int blocksize)
        {
            var newLeafNode = new RangeLeafNode<T>();
            var container = new PinableContainer<FixedList<T>>(this.CreateList(4, blocksize));
            newLeafNode.container = container;
            this.DataStore.Set(container);
            return newLeafNode;
        }

        public LeafNode<T> CreateLeafNode(T item,int blocksize)
        {
            var list = this.CreateList(4, blocksize);
            list.Add(item);
            var container = new PinableContainer<FixedList<T>>(list);
            this.DataStore.Set(container);
            return new RangeLeafNode<T>(list.Count, container);
        }

        public LeafNode<T> CreateLeafNode(long count, FixedList<T> items)
        {
            var container = new PinableContainer<FixedList<T>>(items);
            this.DataStore.Set(container);
            return new RangeLeafNode<T>(count, container);
        }

        public void SetState(Node<T> current, long totalLeftCountInList)
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

        public void ResetState()
        {
            this.customLeastFetch = null;
        }
    }
}
