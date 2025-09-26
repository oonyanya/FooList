using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    /// <summary>
    /// 読み取り専用ストアの基底クラス
    /// </summary>
    /// <typeparam name="T">格納対象の型</typeparam>
    public class ReadonlyContentStoreBase<T> : IPinableContainerStore<T>
    {
        EmptyList emptyList = new EmptyList();
        TwoQueueCacheList<long, PinableContainer<T>> cacheList = null;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="cache_limit">キャッシュの量。すくなくとも、CacheParameters.MINCACHESIZE以上は指定する必要がある。</param>
        public ReadonlyContentStoreBase(int cache_limit = 128)
        {
            this.cacheList = new TwoQueueCacheList<long, PinableContainer<T>>();
            this.cacheList.Limit = cache_limit;
            this.cacheList.CacheOuted += (ev) => {
                var key = ev.Key;
                var outed_item = ev.Value;

                if (outed_item.IsRemoved == true || outed_item.CacheIndex == PinableContainer<T>.ALWAYS_KEEP)
                    return;

                outed_item.Content = default(T);
                this.emptyList.ReleaseID(outed_item.CacheIndex);
                outed_item.CacheIndex = PinableContainer<T>.NOTCACHED;
            };
        }

        public IEnumerable<T> ForEachAvailableContent()
        {
            foreach (var pinableContainer in this.cacheList.ForEachValue())
            {
                if (pinableContainer.CacheIndex != PinableContainer<T>.NOTCACHED || pinableContainer.Content?.Equals(default(T)) == false)
                {
                    yield return pinableContainer.Content;
                }
            }
        }

        /// <summary>
        /// 初回、読み込み時に呼び出される
        /// </summary>
        /// <param name="count">読み込む要素数</param>
        /// <param name="index">割り当てられたインデックス。専らファイル上のアドレスを指す。</param>
        /// <param name="read_bytes">割り当てられたバイト数。</param>
        /// <returns>読み込んだ要素を返す</returns>
        public virtual T OnLoad(int count,out long index,out int read_bytes)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 初回読み込みを行う
        /// </summary>
        /// <param name="count">読み込みたい要素数</param>
        /// <returns>IPinableContainerを返す</returns>
        public IPinableContainer<T> Load(int count)
        {
            int read_bytes;
            long index;
            var content = OnLoad(count, out index, out read_bytes);
            PinableContainer<T> newpin = (PinableContainer<T>)this.CreatePinableContainer(content);
            //ディスク上に存在するので永遠に保存しておく必要はない
            newpin.CacheIndex = PinableContainer<T>.NOTCACHED;
            newpin.Info = new DiskAllocationInfo(index, read_bytes);
            return newpin;
        }

        /// <summary>
        /// 何らかの理由で再読み込みを行った際に呼び出される。例えば、メモリー上にある要素が不要になった後、再度、読み込む必要が出てきたときに呼び出される。
        /// </summary>
        /// <param name="index">読み込むべきインデックス。専らファイル上のアドレスを指す。</param>
        /// <param name="bytes">読み込むべきバイト数</param>
        /// <returns>読み込んだ要素を返す</returns>
        public virtual T OnRead(long index, int bytes)
        {
            throw new NotImplementedException();
        }

        public IPinnedContent<T> Get(IPinableContainer<T> ipinableContainer)
        {
            IPinnedContent<T> result;
            if (this.TryGet(ipinableContainer, out result))
                return result;
            else
                throw new ArgumentException();
        }

        public bool TryGet(IPinableContainer<T> ipinableContainer, out IPinnedContent<T> result)
        {
            var pinableContainer = (PinableContainer<T>)ipinableContainer;
            if (pinableContainer.CacheIndex != PinableContainer<T>.NOTCACHED || pinableContainer.Content?.Equals(default(T)) == false)
            {
                result = new PinnedContent<T>(pinableContainer, this);
                return true;
            }

            PinableContainer<T> _;
            //キャッシュに存在してなかったら、読む
            if (this.cacheList.TryGet(pinableContainer.Info.Index, out _) && pinableContainer.Content != null)
            {
                result = new PinnedContent<T>(pinableContainer, this);
            }
            else
            {
                pinableContainer.Content = OnRead(pinableContainer.Info.Index, pinableContainer.Info.AlignedLength);
                result = new PinnedContent<T>(pinableContainer, this);
            }

            return true;
        }

        public void Set(IPinableContainer<T> ipinableContainer)
        {
            PinableContainer<T> pinableContainer = (PinableContainer<T>)ipinableContainer;
            if (pinableContainer.IsRemoved)
            {
                this.emptyList.ReleaseID(pinableContainer.CacheIndex);
                pinableContainer.Info = null;
                pinableContainer.Content = default(T);
                pinableContainer.CacheIndex = PinableContainer<T>.NOTCACHED;
                return;
            }

            if (pinableContainer.CacheIndex == PinableContainer<T>.ALWAYS_KEEP)
                return;

            if (pinableContainer.CacheIndex == PinableContainer<T>.NOTCACHED)
            {
                pinableContainer.CacheIndex = this.emptyList.GetID();
            }

            this.cacheList.Set(pinableContainer.CacheIndex, pinableContainer);
            return;
        }

        public void Commit()
        {
        }

        public IPinableContainer<T> Update(IPinableContainer<T> pinableContainer, T newcontent, long oldstart, long oldcount, long newstart, long newcount)
        {
            //面倒なので変更した奴はキャッシュアウトの対象外にする
            var ipinableContainer = this.CreatePinableContainer(newcontent);
            PinableContainer<T> newPinableContainer = (PinableContainer<T>)ipinableContainer;
            newPinableContainer.CacheIndex = PinableContainer<T>.ALWAYS_KEEP;
            return newPinableContainer;
        }

        public IPinableContainer<T> CreatePinableContainer(T content)
        {
            var newPinableContainer = new PinableContainer<T>(content);
            //メモリー上にだけ存在する奴があるのでデフォルトは常に保持しておく
            newPinableContainer.CacheIndex = PinableContainer<T>.ALWAYS_KEEP;
            return newPinableContainer;
        }

    }
}
