using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection.DataStore;

namespace FooProject.Collection
{
    /// <summary>
    /// 範囲を表す
    /// </summary>
    public interface IRangeWithHeight : IRange
    {
        /// <summary>
        /// 高さの累計
        /// </summary>
        double sumHeight { get; set; }
        /// <summary>
        /// 高さ
        /// </summary>
        double Height { get; }
    }

    /// <summary>
    /// IRangeWithHeightに対応するインデックスを返すためのテーブル
    /// </summary>
    /// <typeparam name="T">IRangeWithHeightを実装したT</typeparam>
    /// <remarks>連続した範囲でないとうまく動きません</remarks>
    public class BigIndexAndHeightList<T> : BigRangeListBase<T> where T : IRangeWithHeight
    {
        /// <summary>
        /// コンストラクター
        /// </summary>
        public BigIndexAndHeightList() : base()
        {
            var custom = new RangeAndHeightConverter<T>();
            custom.DataStore = new MemoryPinableContentDataStore<IComposableList<T>>();
            this.LeastFetchStore = custom;
            this.CustomBuilder = custom;
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
            var CustomConverter = (RangeAndHeightConverter<T>)LeastFetchStore;

            var item = GetRawData(index);

            T result = (T)item.DeepCopy();
            result.start = item.start + CustomConverter.customLeastFetch.absoluteIndexIntoRange;
            result.sumHeight = item.sumHeight + CustomConverter.customLeastFetch.absoluteSumHeight;
            return result;
        }

        /// <summary>
        /// 要素を取得する
        /// </summary>
        /// <param name="index">0から始まる数値</param>
        /// <returns>Tを返す。IRangeインターフェイスのstartの値は変換されない</returns>
        public override T GetRawData(long index)
        {
            RangeAndHeightConverter<T> myCustomConverter = (RangeAndHeightConverter<T>)LeastFetchStore;
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
                    var customNode = (IRangeAndHeightNode)current.Left;
                    myCustomConverter.customLeastFetch.absoluteIndexIntoRange += customNode.TotalRangeCount;
                    myCustomConverter.customLeastFetch.absoluteSumHeight = customNode.TotalHeightCount;
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

        public long GetIndexFromAbsoluteIndexIntoRange(long indexIntoRange)
        {
            return this.GetIndexFromAbsoluteIndexIntoRange(indexIntoRange, out _);
        }

        /// <summary>
        /// 絶対的な位置、すなわちインデックスに対応する要素の番号を返す
        /// </summary>
        /// <param name="indexIntoRange">0から始まる数値。絶対的な位置を指定しないといけない</param>
        /// <param name="outAbsoulteSumHeight">対応する範囲を起点とする位置</param>
        /// <returns>0から始まる要素の番号。見つからない場合は-1を返す</returns>
        public long GetIndexFromAbsoluteIndexIntoRange(long indexIntoRange,out double outAbsoulteSumHeight)
        {
            RangeAndHeightConverter<T> myCustomConverter = (RangeAndHeightConverter<T>)LeastFetchStore;
            long relativeIndexIntoRange = indexIntoRange;

            var node = WalkNode((current, leftCount) => {
                var rangeLeftNode = (IRangeAndHeightNode)current.Left;
                long leftTotalSumCount = rangeLeftNode.TotalRangeCount;
                double leftTotalSumHeight = rangeLeftNode.TotalHeightCount;

                if (relativeIndexIntoRange < leftTotalSumCount)
                {
                    return NodeWalkDirection.Left;
                }
                else
                {
                    relativeIndexIntoRange -= leftTotalSumCount;
                    myCustomConverter.customLeastFetch.TotalLeftCount += leftCount;
                    myCustomConverter.customLeastFetch.absoluteIndexIntoRange += leftTotalSumCount;
                    myCustomConverter.customLeastFetch.absoluteSumHeight += leftTotalSumHeight;
                    return NodeWalkDirection.Right;
                }
            });

            long relativeIndex, relativeNearIndex;
            var leafNode = (LeafNode<T>)node;
            using (var pinnedContent = CustomBuilder.DataStore.Get(leafNode.container))
            {
                var leafNodeItems = pinnedContent.Content;
                relativeIndex = this.IndexOfNearest(leafNodeItems, relativeIndexIntoRange, out relativeNearIndex);

                outAbsoulteSumHeight = leafNodeItems[(int)relativeIndex].sumHeight + myCustomConverter.customLeastFetch.absoluteSumHeight;
            }

            if (relativeIndex == -1)
            {
                myCustomConverter.ResetState();
                return -1;
            }
            return relativeIndex + myCustomConverter.customLeastFetch.TotalLeftCount;
        }

        /// <summary>
        /// 絶対的な位置、すなわちインデックスに対応する要素の番号を返す
        /// </summary>
        /// <param name="sumHeight">0から始まる数値。絶対的な位置を指定しないといけない</param>
        /// <returns>0から始まる要素の番号。見つからない場合は-1を返す</returns>
        public long GetIndexFromAbsoluteSumHeight(double sumHeight)
        {
            return GetIndexFromAbsoluteSumHeight(sumHeight, out _);
        }

        /// <summary>
        /// 絶対的な位置、すなわちインデックスに対応する要素の番号を返す
        /// </summary>
        /// <param name="sumHeight">0から始まる数値。絶対的な位置を指定しないといけない</param>
        /// <param name="outRelativeSumHeight">対応する範囲を起点とする相対的な位置</param>
        /// <returns>0から始まる要素の番号。見つからない場合は-1を返す</returns>
        public long GetIndexFromAbsoluteSumHeight(double sumHeight,out double outRelativeSumHeight)
        {
            RangeAndHeightConverter<T> myCustomConverter = (RangeAndHeightConverter<T>)LeastFetchStore;
            double relativeSumHeight = sumHeight;

            var node = WalkNode((current, leftCount) => {
                var rangeLeftNode = (IRangeAndHeightNode)current.Left;
                long leftTotalSumCount = rangeLeftNode.TotalRangeCount;
                double leftTotalSumHeight = rangeLeftNode.TotalHeightCount;

                if (relativeSumHeight < leftTotalSumCount)
                {
                    return NodeWalkDirection.Left;
                }
                else
                {
                    relativeSumHeight -= leftTotalSumHeight;
                    myCustomConverter.customLeastFetch.TotalLeftCount += leftCount;
                    myCustomConverter.customLeastFetch.absoluteIndexIntoRange += leftTotalSumCount;
                    myCustomConverter.customLeastFetch.absoluteSumHeight += leftTotalSumHeight;
                    return NodeWalkDirection.Right;
                }
            });

            long relativeIndex, relativeNearIndex;
            var leafNode = (LeafNode<T>)node;
            using (var pinnedContent = CustomBuilder.DataStore.Get(leafNode.container))
            {
                var leafNodeItems = pinnedContent.Content;
                relativeIndex = this.IndexOfNearest(leafNodeItems, relativeSumHeight, out relativeNearIndex);

                //訳が分からなくなるのでこう書いたほうがいい
                outRelativeSumHeight = sumHeight - myCustomConverter.customLeastFetch.absoluteSumHeight - leafNodeItems[(int)relativeIndex].sumHeight;
            }


            if (relativeIndex == -1)
            {
                myCustomConverter.ResetState();
                return -1;
            }
            return relativeIndex + myCustomConverter.customLeastFetch.TotalLeftCount;
        }

        int IndexOfNearest(IList<T> collection, long start, out long nearIndex)
        {
            return this.IndexOfNearest(collection, start, (s, line) => {
                var lineHeadIndex = line.start;
                if (s >= lineHeadIndex && s < lineHeadIndex + line.length)
                {
                    return 0;
                }
                if (start < lineHeadIndex)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }, out nearIndex);
        }

        int IndexOfNearest(IList<T> collection, double start, out long nearIndex)
        {
            return this.IndexOfNearest(collection, start, (s, line) => {
                var lineHeadIndex = line.sumHeight;
                if (s >= lineHeadIndex && s < lineHeadIndex + line.Height)
                {
                    return 0;
                }
                if (start < lineHeadIndex)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }, out nearIndex);
        }

        /// <summary>
        /// 列挙子を取得する
        /// </summary>
        /// <returns>列挙子を取得する</returns>
        /// <remarks>IRangeインターフェイスのstartの値は変換される</remarks>
        public override IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < this.Count; i++)
            {
                yield return GetWithConvertAbsolteIndex(i);
            }
        }
    }

    internal interface IRangeAndHeightNode
    {
        long TotalRangeCount { get; }
        double TotalHeightCount { get; }
    }

    internal class RangeAndHeightConcatNode<T> : ConcatNode<T>, IRangeAndHeightNode where T : IRangeWithHeight
    {

        public RangeAndHeightConcatNode(Node<T> left, Node<T> right) : base(left, right)
        {
        }

        public long TotalRangeCount { get; private set; }
        public double TotalHeightCount { get; private set; }

        protected override void OnNewNode(Node<T> newLeft, Node<T> newRight)
        {
            var customNodeLeft = (IRangeAndHeightNode)newLeft;
            var customNodeRight = (IRangeAndHeightNode)newRight;
            if (customNodeLeft != null)
            {
                TotalRangeCount = customNodeLeft.TotalRangeCount;
                TotalHeightCount = customNodeLeft.TotalHeightCount;
            }
            if (customNodeRight != null)
            {
                TotalRangeCount += customNodeRight.TotalRangeCount;
                TotalHeightCount += customNodeRight.TotalHeightCount;
            }
        }
    }

    internal class RangeAndHeightLeafNode<T> : LeafNode<T>, IRangeAndHeightNode where T : IRangeWithHeight
    {

        public RangeAndHeightLeafNode() : base()
        {
            TotalRangeCount = 0;
        }

        public RangeAndHeightLeafNode(long count, IPinableContainer<IComposableList<T>> container) : base(count, container)
        {
        }

        public override void NotifyUpdate(long startIndex, long count, BigListArgs<T> args)
        {
            using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
            {
                var items = pinnedContent.Content;
                (this.TotalRangeCount, this.TotalHeightCount) = ProcessItems(items, startIndex, count, this.TotalRangeCount, this.TotalHeightCount, args);
            }
        }

        private (long totalRange,double totalHeight) ProcessItems(IComposableList<T> collection, long index, long count, long oldTotalRangeCount,double oldTotalHeightCount, BigListArgs<T> args)
        {
            switch (args.Type)
            {
                case UpdateType.Overwrite:
                    {
                        int updateStartIndex = (int)index;
                        long newIndexIntoRange = 0;
                        double newHeightIntoRange = 0;
                        if (index > 0)
                        {
                            newIndexIntoRange = collection[updateStartIndex - 1].start + collection[updateStartIndex - 1].length;
                            newHeightIntoRange = collection[updateStartIndex - 1].sumHeight + collection[updateStartIndex - 1].Height;
                        }
                        int end = collection.Count - 1;
                        for (int i = updateStartIndex; i <= end; i++)
                        {
                            collection[i].start = newIndexIntoRange;
                            newIndexIntoRange += collection[i].length;
                            collection[i].sumHeight = newHeightIntoRange;
                            newHeightIntoRange += collection[i].Height;
                        }
                        return (newIndexIntoRange, newHeightIntoRange);

                    }
                case UpdateType.Add:
                    {
                        int updateStartIndex = (int)index;
                        long newIndexIntoRange = 0;
                        double newHeightIntoRange = 0;
                        if (updateStartIndex > 0)
                        {
                            newIndexIntoRange = collection[updateStartIndex - 1].start + collection[updateStartIndex - 1].length;
                            newHeightIntoRange = collection[updateStartIndex - 1].sumHeight + collection[updateStartIndex - 1].Height;
                        }

                        long deltaLength = 0;
                        double deltaHeight = 0;
                        for (int i = updateStartIndex; i < updateStartIndex + count; i++)
                        {
                            deltaLength += collection[i].length;
                            deltaHeight += collection[i].Height; 
                        }

                        for (int i = updateStartIndex; i < collection.Count; i++)
                        {
                            collection[i].start = newIndexIntoRange;
                            newIndexIntoRange += collection[i].length;
                            collection[i].sumHeight = newHeightIntoRange;
                            newHeightIntoRange += collection[i].Height;
                        }

                        return (oldTotalRangeCount + deltaLength, oldTotalHeightCount + deltaHeight);
                    }
                case UpdateType.Insert:
                    {
                        int insert_collection_count = (int)count;

                        long deltaLength = 0;
                        double deltaHeight = 0;
                        for (int i = (int)index; i < index + insert_collection_count; i++)
                        {
                            deltaLength += collection[i].length;
                            deltaHeight += collection[i].Height;
                        }

                        for (int i = (int)index + insert_collection_count; i < collection.Count; i++)
                        {
                            collection[i].start += deltaLength;
                            collection[i].sumHeight += deltaHeight;
                        }

                        int previousIndex = (int)index;
                        long newIndexIntoRange = 0;
                        double newHeightIntoRange = 0;
                        if (index > 0)
                        {
                            previousIndex--;
                            newIndexIntoRange = collection[previousIndex].start + collection[previousIndex].length;
                            newHeightIntoRange = collection[previousIndex].sumHeight + collection[previousIndex].Height;
                        }
                        int end = collection.Count - 1;
                        for (int i = (int)index; i <= end; i++)
                        {
                            collection[i].start = newIndexIntoRange;
                            newIndexIntoRange += collection[i].length;
                            collection[i].sumHeight = newHeightIntoRange;
                            newHeightIntoRange += collection[i].Height;
                        }

                        return (oldTotalRangeCount + deltaLength, oldTotalHeightCount + deltaHeight);
                    }
                case UpdateType.Remove:
                    {
                        long deltaLength = 0;
                        double deltaHeight = 0;
                        if (index < collection.Count)
                        {
                            int updateStartIndex = (int)index;
                            if (index > 0)
                            {
                                deltaLength = collection[updateStartIndex].start - (collection[updateStartIndex - 1].start + collection[updateStartIndex - 1].length);
                                deltaHeight = collection[updateStartIndex].sumHeight - (collection[updateStartIndex - 1].sumHeight + collection[updateStartIndex - 1].Height);
                            }
                            else
                            {
                                //開始位置は常に0から始まる
                                deltaLength = collection[updateStartIndex].start;
                                deltaHeight = collection[updateStartIndex].sumHeight;
                            }

                            for (int i = updateStartIndex; i < collection.Count; i++)
                            {
                                collection[i].start -= deltaLength;
                                collection[i].sumHeight -= deltaHeight;
                            }

                            return (oldTotalRangeCount - deltaLength,oldTotalHeightCount - deltaHeight);
                        }
                        else
                        {
                            int lastIndex = collection.Count - 1;
                            return (
                                collection[lastIndex].start + collection[lastIndex].length,
                                collection[lastIndex].sumHeight + collection[lastIndex].Height
                                );
                        }
                    }
            }
            return (oldTotalRangeCount,oldTotalHeightCount);
        }

        public long TotalRangeCount { get; private set; }
        public double TotalHeightCount { get; private set; }
    }

    internal class RangeAndHeightLeastFetch<T> : ILeastFetch<T> where T : IRangeWithHeight
    {
        public Node<T> Node { get; set; }

        public long TotalLeftCount { get; set; }

        public long absoluteIndexIntoRange { get; set; }

        public double absoluteSumHeight { get; set; }

        public RangeAndHeightLeastFetch()
        {
        }
    }

    internal class RangeAndHeightConverter<T> : CustomConverterBase<T> where T : IRangeWithHeight
    {
        public override ILeastFetch<T> LeastFetch { get { return customLeastFetch; } }

        public RangeAndHeightLeastFetch<T> customLeastFetch { get; set; }

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
            return new RangeAndHeightConcatNode<T>(left, right);
        }

        protected override LeafNode<T> OnCreateLeafNode()
        {
            return new RangeAndHeightLeafNode<T>();
        }

        protected override LeafNode<T> OnCreateLeafNode(long count, IPinableContainer<IComposableList<T>> container)
        {
            if (container.Content is FixedRangeList<T>)
            {
                return new RangeAndHeightLeafNode<T>(count, container);
            }
            throw new NotSupportedException("FixedRangeListを継承したクラスをcontainerのContentに設定する必要があります");
        }

        public override void SetState(Node<T> current, long totalLeftCountInList)
        {
            if (current == null)
            {
                this.customLeastFetch = new RangeAndHeightLeastFetch<T>();
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
