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

        public int Count
        {
            get {
                return _rleData.Count;
            }
        }

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
            var index = _rleData.GetIndexFromAbsoluteIndexIntoRange(absolute_index);
            if (index == -1)
                throw new InvalidOperationException("absoulte range is invaild");

            var container = _rleData.Get(index);
            if (count <= container.length)
            {
                if (container.length == count)
                {
                    _rleData.RemoveAt(index);
                }
                else
                {
                    container.length -= count;
                    _rleData.Set(index, container);
                }
            }
            else
            {
                long total_remove_length = count;
                while (true)
                {
                    var offset = Math.Max(0, absolute_index - container.start);
                    var offseted_length = container.length - offset;
                    var remove_length = Math.Min(total_remove_length, offseted_length);
                    if (container.length == remove_length)
                    {
                        _rleData.RemoveAt(index);
                    }
                    else
                    {
                        container.length -= remove_length;
                        _rleData.Set(index, container);
                    }

                    total_remove_length -= remove_length;

                    if (total_remove_length <= 0)
                        break;

                    index = _rleData.GetIndexFromAbsoluteIndexIntoRange(absolute_index);
                    if (index == -1)
                        throw new InvalidOperationException("absoulte range is invaild");
                    container = this.Get(absolute_index);
                }
            }
        }

        /// <summary>
        /// アイテムを処理する
        /// </summary>
        /// <param name="container">処理対象のコンテナー</param>
        /// <param name="count">処理対象の数</param>
        /// <param name="input">入力アイテム</param>
        /// <returns>処理済みのアイテムを返す。</returns>
        /// <remarks>デフォルトだとinputの内容をそのまま返します。何かしらの処理をした場合、continerを複製してください。複製しない場合の動作は保証されません</remarks>
        protected virtual T ProcessItem(T container, long count, J input)
        {
            return new T() { length = count , Value = input};
        }

        /// <summary>
        /// アイテムを更新します
        /// </summary>
        /// <param name="absolute_index">更新するインデックス</param>
        /// <param name="count">長さ</param>
        /// <param name="input_value">アイテム</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>何もしない場合、input_valueの値で置き換えます。処理の仕方を変更したい場合、継承先のクラスでProcessItemを上書きする必要があります</remarks>
        public void UpdateRange(int absolute_index, J input_value, int count = 1)
        {
            var index = _rleData.GetIndexFromAbsoluteIndexIntoRange(absolute_index);
            if (index == -1)
                throw new InvalidOperationException("absoulte range is invaild");

            var container = _rleData.Get(index);
            if (count <= container.length)
            {
                if (container.length == count)
                {
                    _rleData.RemoveAt(index);
                    var new_item = this.ProcessItem(container, container.length, input_value);
                    _rleData.Insert(index, new_item);
                }
                else
                {
                    var offset = absolute_index - container.start;
                    var offseted_length = container.length - offset;

                    if (offset > 0)
                    {
                        _rleData.Set(index, new T() { Value = container.Value, length = offset });
                        _rleData.Insert(index + 1, this.ProcessItem(container, count, input_value));
                        var new_item_length = offseted_length - count;
                        if (new_item_length > 0)
                            _rleData.Insert(index + 2, new T() { Value = container.Value, length = new_item_length });
                    }
                    else
                    {
                        _rleData.Set(index, this.ProcessItem(container, count, input_value));
                        _rleData.Insert(index + 1, new T() { Value = container.Value, length = offseted_length - count });
                    }
                }
            }
            else
            {
                long current_index = absolute_index;
                long total_remove_length = count;
                while (true)
                {
                    var offset = Math.Max(0, absolute_index - container.start);
                    var offseted_length = container.length - offset;
                    var remove_length = Math.Min(total_remove_length, offseted_length);
                    if (container.length == remove_length)
                    {
                        _rleData.RemoveAt(index);
                        var new_item = this.ProcessItem(container, container.length, input_value);
                        _rleData.Insert(index, new_item);
                    }
                    else
                    {
                        if (total_remove_length == count)    //先頭かどうか判別する
                        {
                            container.length -= remove_length;
                            _rleData.Set(index, container);
                            var new_item = this.ProcessItem(container, remove_length, input_value);
                            _rleData.Insert(index + 1, new_item);
                        }
                        else
                        {
                            var new_item = this.ProcessItem(container, remove_length, input_value);
                            _rleData.Insert(index, new_item);
                            container.length -= remove_length;
                            _rleData.Set(index + 1, container);
                        }
                    }

                    total_remove_length -= remove_length;

                    if (total_remove_length <= 0)
                        break;

                    current_index += remove_length;

                    index = _rleData.GetIndexFromAbsoluteIndexIntoRange(current_index);
                    if (index == -1)
                        throw new InvalidOperationException("absoulte range is invaild");
                    container = this.Get(current_index);
                }
            }
        }

        /// <summary>
        /// アイテムを削除します
        /// </summary>
        /// <param name="absolute_index">削除するインデックス</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>アイテムの長さを変えます。0になった場合、アイテム自体が削除されます。</remarks>
        public void RemoveAt(int absolute_index)
        {
            this.RemoveRange(absolute_index, 1);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}
