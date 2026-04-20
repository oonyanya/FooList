/*
 一部コードはGrokの支援を受けて作成しています。
 プロンプトは「RleArrayをc#で作成せよ」と「Insert,Remove,Addを追加せよ」です。
 ただし、何も考えずに指示をするとGrokのコードはO(N)になってしまう、計算量を減らすために適宜改変しています。
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection
{
    public interface IRleArrayRange<J> : IRange
    {
        J Value { get; set; }
    }
    public class BigRleArray<T, J>: IEnumerable<T> where  T : IRleArrayRange<J>, new()
    {
        BigRangeList<T> _rleData = new BigRangeList<T>();

        /// <summary>
        /// アイテムを追加する
        /// </summary>
        /// <param name="item">追加するアイテム</param>
        /// <remarks>既に存在するアイテムの場合、アイテムの長さが変わります</remarks>
        public void AddRange(J item,int count = 1)
        {
            if (_rleData.Count > 0)
            {
                var last = _rleData[_rleData.Count - 1];
                if (last.Value.Equals(item))
                {
                    last.length += count;
                    _rleData[_rleData.Count - 1] = last;
                }
                else
                {
                    var new_value = new T();
                    new_value.Value = item;
                    new_value.length = count;
                    _rleData.Add(new_value);
                }
            }
            else
            {
                var new_value = new T();
                new_value.Value = item;
                new_value.length = count;
                _rleData.Add(new_value);
            }
        }

        /// <summary>
        /// アイテムを取得します
        /// </summary>
        /// <param name="absolute_index">取得対象の絶対インデックス</param>
        /// <returns>絶対インデックスに該当するアイテムを返す</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public T Get(long absolute_index)
        {
            var i = _rleData.GetIndexFromAbsoluteIndexIntoRange(absolute_index);
            if (i == -1)
                throw new InvalidOperationException("absoulte range is invaild or not found");

            var container = _rleData.Get(i);
            return container;
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach(var item in _rleData)
                yield return item;
        }

        /// <summary>
        /// アイテムを挿入します
        /// </summary>
        /// <param name="absolute_index">挿入対象の絶対インデックス</param>
        /// <param name="item">挿入対象のアイテム</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>既に存在するアイテムの場合、アイテムの長さが変わります。また、既に存在するアイテムに別のアイテムを挿入した場合、分割されます</remarks>
        public void InsertRange(int absolute_index, J item,int count = 1)
        {
            if (absolute_index == _rleData.Count)
            {
                AddRange(item);
                return;
            }

            var i = _rleData.GetIndexFromAbsoluteIndexIntoRange(absolute_index);
            if(i == -1)
                throw new InvalidOperationException("absoulte range is invaild or not found");

            var container = _rleData.Get(i);
            if (container.Value.Equals(item))
            {
                container.length++;
                _rleData.Set(i, container);
            }
            else
            {
                var CustomConverter = (RangeConverter<T>)_rleData.LeastFetchStore;
                var converter = CustomConverter.customLeastFetch.absoluteIndexIntoRange;
                var offset = absolute_index - container.start;
                var length = container.length - offset;

                if (offset > 0)
                {
                    _rleData.Set(i, new T() { Value = container.Value, length = offset });
                    _rleData.Insert(i + 1, new T() { Value = item, length = count });
                    _rleData.Insert(i + 2, new T() { Value = container.Value, length = length });
                }
                else
                {
                    _rleData.Set(i, new T() { Value = item, length = count });
                    _rleData.Insert(i + 1, new T() { Value = container.Value, length = length });
                }
            }
        }

        /// <summary>
        /// アイテムを削除します
        /// </summary>
        /// <param name="absolute_index">削除するインデックス</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>アイテムの長さを変えます。0になった場合、アイテム自体が削除されます。</remarks>
        public void RemoveRange(int absolute_index,int count = 1)
        {
            long removed_length = count;

            while(removed_length >= 0)
            {
                var i = _rleData.GetIndexFromAbsoluteIndexIntoRange(absolute_index);
                if (i == -1)
                    throw new InvalidOperationException("absoulte range is invaild");

                var container = _rleData.Get(i);
                if (container.length <= removed_length)
                {
                    _rleData.RemoveAt(i);
                    removed_length -= container.length;
                }
                else
                {
                    container.length -= removed_length;
                    _rleData.Set(i, container);
                    break;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}
