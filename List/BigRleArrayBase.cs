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
        /// 全ての範囲の長さを合計した値を返す
        /// </summary>
        public long TotalRangeCount
        {
            get
            {
                return _rleData.TotalRangeCount;
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
        public void Add(IRleArrayRange<T> item)
        {
            if (_rleData.Count > 0)
            {
                var last = _rleData[_rleData.Count - 1];
                if (last.Value.Equals(item.Value))
                {
                    last.length += item.length;
                    _rleData[_rleData.Count - 1] = last;
                }
                else
                {
                    _rleData.Add(item);
                }
            }
            else
            {
                _rleData.Add(item);
            }
        }

        /// <summary>
        /// アイテムを追加する
        /// </summary>
        /// <param name="item">追加するアイテム</param>
        /// <remarks>既に存在するアイテムの場合、アイテムの長さが変わります</remarks>
        public void AddRange(T item,int count = 1)
        {
            var new_value = this.CreateItem(value: item, length: count);
            this.Add(new_value);
        }

        /// <summary>
        /// アイテムを取得します
        /// </summary>
        /// <param name="absolute_index">取得対象の絶対インデックス</param>
        /// <returns>絶対インデックスに該当するアイテムを返す</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public T GetValue(long absolute_index)
        {
            var i = this.GetIndexFromAbsoluteIndexIntoRange(absolute_index);

            var container = _rleData.Get(i);
            return container.Value;
        }

        /// <summary>
        /// アイテムを取得する
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns></returns>
        public IRleArrayRange<T> GetAt(long index)
        {
            return _rleData.Get(index);
        }

        /// <summary>
        ///　対応するIRleArrayRangeを取得する
        /// </summary>
        /// <param name="absolute_index">取得対象の絶対インデックス</param>
        /// <returns></returns>
        public IRleArrayRange<T> Get(long absolute_index)
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
        public IRleArrayRange<T> Get(long absolute_index, out long index)
        {
            var i = this.GetIndexFromAbsoluteIndexIntoRange(absolute_index);

            var container = _rleData.Get(i);
            index = i;
            return container;
        }

        /// <summary>
        /// アイテムを更新します
        /// </summary>
        /// <param name="index">更新するインデックス</param>
        /// <param name="item">新しいアイテム</param>
        public void SetAt(long index, IRleArrayRange<T> item)
        {
            this._rleData.Set(index, item);
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
        /// アイテムを挿入する
        /// </summary>
        /// <param name="item">挿入したいアイテム</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>既に存在するアイテムの場合、当該アイテムの長さが変わります。また、既に存在するアイテムに別のアイテムを挿入した場合、分割されます</remarks>
        public void Insert(IRleArrayRange<T> item)
        {
            var absolute_index = item.start;
            if (absolute_index == _rleData.TotalRangeCount)
            {
                Add(item);
                return;
            }

            var i = _rleData.GetIndexFromAbsoluteIndexIntoRange(absolute_index);
            if (i == -1)
                throw new InvalidOperationException("absoulte range is invaild or not found");

            var container = _rleData.Get(i);
            if (container.Value.Equals(item.Value))
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
                    _rleData.Set(i, this.CreateItem(value: container.Value, length: offset));
                    _rleData.Insert(i + 1, item);
                    _rleData.Insert(i + 2, this.CreateItem(value: container.Value, length: length));
                }
                else
                {
                    _rleData.Set(i, item);
                    _rleData.Insert(i + 1, this.CreateItem(value: container.Value, length: length));
                }
            }

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
            var new_item = this.CreateItem(value: item, start:absolute_index, length: count);
            this.Insert(new_item);
        }

        /// <summary>
        /// アイテムを削除します
        /// </summary>
        /// <param name="absolute_index">削除する絶対インデックス</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>アイテムの長さを変えます。0になった場合、アイテム自体が削除されます。</remarks>
        public void RemoveRange(int absolute_index,int count = 1)
        {
            var index = this.GetIndexFromAbsoluteIndexIntoRange(absolute_index);

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
        /// <returns>カスタム処理を実装したい場合、continerを複製してください。なお、countとinput_itemの長さが同じ場合は複製する必要はありません</returns>
        protected virtual IRleArrayRange<T> defaultProcessItem(IRleArrayRange<T> container, long count, IRleArrayRange<T> input_item)
        {
            if (input_item.length == count)
                return input_item;
            else
                return CreateItem( length:count, value: input_item.Value );
        }

        internal long GetIndexFromAbsoluteIndexIntoRange(long absolute_index)
        {
            var index = 0L;

            if(absolute_index == 0)
            {
                if (this._rleData.Count > 0)
                    index = 0;
                else
                    index = -1;
            }
            else if(absolute_index == this.TotalRangeCount)
            {
                index = this.Count - 1;
            }
            else
            {
                index = _rleData.GetIndexFromAbsoluteIndexIntoRange(absolute_index);
            }

            if (index == -1)
                throw new InvalidOperationException("absoulte range is invaild");

            return index;
        }

        /// <summary>
        /// アイテムを更新します
        /// </summary>
        /// <param name="absolute_index">更新する絶対インデックス</param>
        /// <param name="count">更新する長さ</param>
        /// <param name="input_item">アイテム</param>
        /// <param name="processItem">処理用のメソッド。nullの場合、単純に上書きされます。arg1は処理対象のコンテナー、arg2は出力すべき数、arg3は入力アイテムを表します。</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>何もしない場合、input_itemの値で置き換えます。カスタム処理を実装したい場合、continerを複製してください。</remarks>
        public void Update(int absolute_index, int count , IRleArrayRange<T> input_item, Func<IRleArrayRange<T>, long, IRleArrayRange<T>, IRleArrayRange<T>> processItem = null)
        {
            var input_value = input_item.Value;

            var index = this.GetIndexFromAbsoluteIndexIntoRange(absolute_index);

            if (processItem == null)
            {
                processItem = defaultProcessItem;
            }

            var container = _rleData.Get(index);
            if (count <= container.length)
            {
                if (container.length == count)
                {
                    _rleData.RemoveAt(index);
                    var new_item = processItem(container, container.length, input_item);
                    _rleData.Insert(index, new_item);
                }
                else
                {
                    var offset = absolute_index - container.start;
                    var offseted_length = container.length - offset;

                    if (offset > 0)
                    {
                        _rleData.Set(index, this.CreateItem(value: container.Value, length: offset));
                        _rleData.Insert(index + 1, processItem(container, count, input_item));
                        var new_item_length = offseted_length - count;
                        if (new_item_length > 0)
                            _rleData.Insert(index + 2, this.CreateItem(value: container.Value, length: new_item_length));
                    }
                    else
                    {
                        _rleData.Set(index, processItem(container, count, input_item));
                        _rleData.Insert(index + 1, this.CreateItem(value: container.Value, length: offseted_length - count));
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
                        var new_item = processItem(container, container.length, input_item);
                        _rleData.Insert(index, new_item);
                    }
                    else
                    {
                        if (total_remove_length == count)    //先頭かどうか判別する
                        {
                            container.length -= remove_length;
                            _rleData.Set(index, container);
                            var new_item = processItem(container, remove_length, input_item);
                            _rleData.Insert(index + 1, new_item);
                        }
                        else
                        {
                            var new_item = processItem(container, remove_length, input_item);
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
        /// アイテムを更新します
        /// </summary>
        /// <param name="absolute_index">更新する絶対インデックス</param>
        /// <param name="count">長さ</param>
        /// <param name="input_value">アイテム</param>
        /// <param name="processItem">処理用のメソッド。nullの場合、単純に上書きされます。arg1は処理対象のコンテナー、arg2は出力すべき数、arg3は入力アイテムを表します。</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>何もしない場合、input_valueの値で置き換えます。カスタム処理を実装したい場合、continerを複製してください。</remarks>
        public void UpdateRange(int absolute_index, T input_value, int count = 1, Func<IRleArrayRange<T>, long, IRleArrayRange<T>, IRleArrayRange<T>> processItem = null)
        {
            var new_item = this.CreateItem(input_value, absolute_index, count);
            this.Update(absolute_index, count, new_item, processItem);
        }

        /// <summary>
        /// アイテムを削除します
        /// </summary>
        /// <param name="index">削除するインデックス</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>アイテムの長さを変えます。0になった場合、アイテム自体が削除されます。</remarks>
        public void RemoveAt(int index)
        {
            this._rleData.RemoveAt(index);
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
