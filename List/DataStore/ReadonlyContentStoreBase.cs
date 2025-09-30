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
        const int SECONDARY_DATA_STORE_ID = 1;

        public IPinableContainerStore<T> SecondaryDataStore { get; set; }

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

            IPinableContainerStoreWithAutoDisposer<T> secondaryDataStoreWithAutoDisposer = this.SecondaryDataStore as IPinableContainerStoreWithAutoDisposer<T>;
            if (secondaryDataStoreWithAutoDisposer != null)
            {
                foreach(var item in secondaryDataStoreWithAutoDisposer.ForEachAvailableContent())
                {
                    yield return item;
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
        /// <remarks>read_bytesが0を返した場合、これ以上読み取るものがないことを表す</remarks>
        public virtual T OnLoad(int count,out long index,out int read_bytes)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 初回読み込みを行う
        /// </summary>
        /// <param name="count">読み込みたい要素数</param>
        /// <returns>IPinableContainerを返す。これ以上読み取ることができない場合、nullを返す。</returns>
        public IPinableContainer<T> Load(int count)
        {
            int read_bytes;
            long index;
            var content = OnLoad(count, out index, out read_bytes);
            if (read_bytes == 0)
                return null;
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
            if (SecondaryDataStore != null)
            {
                switch (ipinableContainer.ID)
                {
                    case SECONDARY_DATA_STORE_ID:
                        return this.SecondaryDataStore.Get(ipinableContainer);
                }
            }

            IPinnedContent<T> result;
            if (this.TryGet(ipinableContainer, out result))
                return result;
            else
                throw new ArgumentException();
        }

        public bool TryGet(IPinableContainer<T> ipinableContainer, out IPinnedContent<T> result)
        {
            if (SecondaryDataStore != null)
            {
                switch (ipinableContainer.ID)
                {
                    case SECONDARY_DATA_STORE_ID:
                        return this.SecondaryDataStore.TryGet(ipinableContainer, out result);
                }
            }

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
            if (SecondaryDataStore != null)
            {
                switch (ipinableContainer.ID)
                {
                    case SECONDARY_DATA_STORE_ID:
                        this.SecondaryDataStore.Set(ipinableContainer);
                        return;
                }
            }

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
            if (SecondaryDataStore != null)
            {
                this.SecondaryDataStore.Commit();
            }
        }

        public IPinableContainer<T> Update(IPinableContainer<T> pinableContainer, T newcontent, long oldstart, long oldcount, long newstart, long newcount)
        {
            if (SecondaryDataStore != null)
            {
                switch (pinableContainer.ID)
                {
                    case SECONDARY_DATA_STORE_ID:
                        var updatedPinableContainer = this.SecondaryDataStore.Update(pinableContainer, newcontent, oldstart, oldcount, newstart, newcount);
                        updatedPinableContainer.ID = SECONDARY_DATA_STORE_ID;
                        return updatedPinableContainer;
                }
            }
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
            if (SecondaryDataStore != null)
                newPinableContainer.ID = SECONDARY_DATA_STORE_ID;
            else
                newPinableContainer.CacheIndex = PinableContainer<T>.ALWAYS_KEEP;
            return newPinableContainer;
        }

    }
}
