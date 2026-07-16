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
    /// <summary>
    /// IRleArrayRangeインターフェイス
    /// </summary>
    /// <typeparam name="J"></typeparam>
    public interface IRleArrayRange<J> : IRange
    {
        J Value { get; set; }
    }

    /// <summary>
    /// RleArrayを格納するためのコレクションの基底クラス
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BigRleArrayBase<T>: IEnumerable<IRleArrayRange<T>>
    {
        BigRangeList<IRleArrayRange<T>> _rleData;

        public BigRleArrayBase(int block_size = 0)
        {
            _rleData = new BigRangeList<IRleArrayRange<T>>();
            if(block_size > 0 )
                _rleData.BlockSize = block_size;
        }

        /// <summary>
        /// IRleArrayRangeを格納しているコレクション
        /// </summary>
        protected BigRangeList<IRleArrayRange<T>> RleData
        {
            get { return _rleData; }
        }

        /// <summary>
        /// 要素数
        /// </summary>
        public int Count
        {
            get {
                return _rleData.Count;
            }
        }

        /// <summary>
        /// アイテムを作成する
        /// </summary>
        /// <param name="value">値</param>
        /// <param name="start">開始インデックス。特に指定してない場合は-1</param>
        /// <param name="length">長さ。特に指定してない場合は-1</param>
        /// <returns>IRleArrayRangeを継承したクラスを返す。nullを返してはならない</returns>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual IRleArrayRange<T> CreateItem(T value,long start = -1,long length = -1)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// アイテムを追加する
        /// </summary>
        /// <param name="item">追加するアイテム</param>
        /// <remarks>既に存在するアイテムの場合、アイテムの長さが変わります</remarks>
        public void AddRange(T item,int count = 1)
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
                    var new_value = this.CreateItem(value: item, length: count);
                    new_value.Value = item;
                    new_value.length = count;
                    _rleData.Add(new_value);
                }
            }
            else
            {
                var new_value = this.CreateItem(value:item,length:count);
                _rleData.Add(new_value);
            }
        }

        /// <summary>
        /// アイテムを取得します
        /// </summary>
        /// <param name="absolute_index">取得対象の絶対インデックス</param>
        /// <returns>絶対インデックスに該当するアイテムを返す</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public T GetValue(long absolute_index)
        {
            var i = _rleData.GetIndexFromAbsoluteIndexIntoRange(absolute_index);
            if (i == -1)
                throw new InvalidOperationException("absoulte range is invaild or not found");

            var container = _rleData.Get(i);
            return container.Value;
        }

        /// <summary>
        ///　対応するIRleArrayRangeを取得する
        /// </summary>
        /// <param name="absolute_index">取得対象の絶対インデックス</param>
        /// <returns></returns>
        protected IRleArrayRange<T> Get(long absolute_index)
        {
            return this.Get(absolute_index, out _);
        }

        /// <summary>
        ///　対応するIRleArrayRangeを取得する
        /// </summary>
        /// <param name="absolute_index">取得対象の絶対インデックス</param>
        /// <param name="index">IRleArrayRangeが存在するインデックス</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected IRleArrayRange<T> Get(long absolute_index, out long index)
        {
            var i = _rleData.GetIndexFromAbsoluteIndexIntoRange(absolute_index);
            if (i == -1)
                throw new InvalidOperationException("absoulte range is invaild or not found");

            var container = _rleData.Get(i);
            index = i;
            return container;
        }

        /// <summary>
        /// 列挙子を返す
        /// </summary>
        /// <param name="absolute_index">開始インデックス</param>
        /// <param name="count">長さ</param>
        /// <returns>列挙子</returns>
        /// <remarks>IRleArrayRangeのstartとlengthは開始インデックスと長さの範囲内になるように調整されます</remarks>
        public IEnumerable<IRleArrayRange<T>> GetRanges(long absolute_index, long count)
        {
            return _rleData.GetFromAbsoluteIndexIntoRange(absolute_index, count, (item, relative_start, total_fetched_count, left_count) =>
            {
                return this.CreateItem(item.Value, item.start + total_fetched_count, item.length);
            });
        }

        /// <summary>
        /// 列挙子を返す
        /// </summary>
        /// <param name="absolute_index">開始インデックス</param>
        /// <param name="count">長さ</param>
        /// <returns>列挙子</returns>
        /// <remarks>IRleArrayRangeのstartとlengthは開始インデックスと長さの範囲内になるように調整されます</remarks>
        public IEnumerable<IRleArrayRange<T>> GetRangesAndClamp(long absolute_index,long count)
        {
            return _rleData.GetFromAbsoluteIndexIntoRange(absolute_index, count,(item, relative_start, total_fetched_count, left_count) =>
            {
                var clamped_count = item.length;
                if (relative_start > 0)
                {
                    clamped_count = item.length - relative_start;
                    return this.CreateItem(item.Value, item.start + relative_start + total_fetched_count, clamped_count);
                }
                else if (left_count < clamped_count)
                {
                    return this.CreateItem(item.Value, item.start + total_fetched_count, left_count);
                }
                else
                {
                    return this.CreateItem(item.Value, item.start + total_fetched_count, clamped_count);
                }
            });
        }

        /// <summary>
        /// 列挙子を返す
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IRleArrayRange<T>> GetEnumerator()
        {
            foreach (var item in _rleData)
                yield return item;
        }

        /// <summary>
        /// アイテムを挿入します
        /// </summary>
        /// <param name="absolute_index">挿入対象の絶対インデックス</param>
        /// <param name="item">挿入対象のアイテム</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>既に存在するアイテムの場合、アイテムの長さが変わります。また、既に存在するアイテムに別のアイテムを挿入した場合、分割されます</remarks>
        public void InsertRange(int absolute_index, T item,int count = 1)
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
                var CustomConverter = (RangeConverter<IRleArrayRange<T>>)_rleData.LeastFetchStore;
                var converter = CustomConverter.customLeastFetch.absoluteIndexIntoRange;
                var offset = absolute_index - container.start;
                var length = container.length - offset;

                if (offset > 0)
                {
                    _rleData.Set(i, this.CreateItem( value:container.Value, length :offset));
                    _rleData.Insert(i + 1, this.CreateItem(value : item, length : count ));
                    _rleData.Insert(i + 2, this.CreateItem(value : container.Value, length : length ));
                }
                else
                {
                    _rleData.Set(i, this.CreateItem(value : item, length : count ));
                    _rleData.Insert(i + 1, this.CreateItem(value : container.Value, length : length ));
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

                    container = this.Get(absolute_index, out index);
                }
            }
        }

        /// <summary>
        /// デフォルトのアイテム処理用メソッド
        /// </summary>
        /// <param name="container">処理対象のコンテナー</param>
        /// <param name="count">出力すべき数</param>
        /// <param name="input">入力アイテム</param>
        /// <returns>カスタム処理を実装したい場合、continerを複製してください。</returns>
        protected virtual IRleArrayRange<T> defaultProcessItem(IRleArrayRange<T> container, long count, T input)
        {
            return CreateItem( length:count, value:input );
        }

        /// <summary>
        /// アイテムを更新します
        /// </summary>
        /// <param name="absolute_index">更新するインデックス</param>
        /// <param name="count">長さ</param>
        /// <param name="input_value">アイテム</param>
        /// <param name="processItem">処理用のメソッド。nullの場合、単純に上書きされます。arg1は処理対象のコンテナー、arg2は出力すべき数、arg3は入力アイテムを表します。</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>何もしない場合、input_valueの値で置き換えます。カスタム処理を実装したい場合、continerを複製してください。</remarks>
        public void UpdateRange(int absolute_index, T input_value, int count = 1, Func<IRleArrayRange<T>, long, T, IRleArrayRange<T>> processItem = null)
        {
            var index = _rleData.GetIndexFromAbsoluteIndexIntoRange(absolute_index);
            if (index == -1)
                throw new InvalidOperationException("absoulte range is invaild");

            if(processItem == null)
            {
                processItem = defaultProcessItem;
            }

            var container = _rleData.Get(index);
            if (count <= container.length)
            {
                if (container.length == count)
                {
                    _rleData.RemoveAt(index);
                    var new_item = processItem(container, container.length, input_value);
                    _rleData.Insert(index, new_item);
                }
                else
                {
                    var offset = absolute_index - container.start;
                    var offseted_length = container.length - offset;

                    if (offset > 0)
                    {
                        _rleData.Set(index, this.CreateItem( value :container.Value, length : offset ));
                        _rleData.Insert(index + 1, processItem(container, count, input_value));
                        var new_item_length = offseted_length - count;
                        if (new_item_length > 0)
                            _rleData.Insert(index + 2, this.CreateItem(value:container.Value, length:new_item_length ));
                    }
                    else
                    {
                        _rleData.Set(index, processItem(container, count, input_value));
                        _rleData.Insert(index + 1, this.CreateItem(value : container.Value, length : offseted_length - count ));
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
                        var new_item = processItem(container, container.length, input_value);
                        _rleData.Insert(index, new_item);
                    }
                    else
                    {
                        if (total_remove_length == count)    //先頭かどうか判別する
                        {
                            container.length -= remove_length;
                            _rleData.Set(index, container);
                            var new_item = processItem(container, remove_length, input_value);
                            _rleData.Insert(index + 1, new_item);
                        }
                        else
                        {
                            var new_item = processItem(container, remove_length, input_value);
                            _rleData.Insert(index, new_item);
                            container.length -= remove_length;
                            _rleData.Set(index + 1, container);
                        }
                    }

                    total_remove_length -= remove_length;

                    if (total_remove_length <= 0)
                        break;

                    current_index += remove_length;

                    container = this.Get(current_index, out index);
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

        /// <summary>
        /// アイテムをすべて削除します
        /// </summary>
        public void Clear()
        {
            _rleData.Clear();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}
