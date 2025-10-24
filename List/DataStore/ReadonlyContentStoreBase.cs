using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    /// <summary>
    /// 読み込んだ結果を格納する
    /// </summary>
    /// <typeparam name="T">読み取り内容の型を表す</typeparam>
    public class OnLoadAsyncResult<T>
    {
        /// <summary>
        /// 読み込まれた内容
        /// </summary>
        /// <remarks>何も読み込まれなかった場合はnullが設定される</remarks>
        public T Value { get; set; }

        /// <summary>
        /// 割り当てられたインデックス。専らファイル上のアドレスを指す。
        /// </summary>
        /// <remarks>何も読み込まれなかった場合は0が設定される</remarks>
        public long Index { get; }

        /// <summary>
        /// 割り当てられたバイト数。
        /// </summary>
        /// <remarks>何も読み込まれなかった場合は0が設定される</remarks>
        public int ReadBytes { get; }

        public OnLoadAsyncResult(T value, long index, int read_bytes)
        {
            this.Value = value;
            this.Index = index;
            this.ReadBytes = read_bytes;
        }

        public static OnLoadAsyncResult<T> Create<J>(T  value, OnLoadAsyncResult<J> old)
        {
            return new OnLoadAsyncResult<T>(value,old.Index,old.ReadBytes);
        }
    }

    /// <summary>
    /// 読み取り専用ストアの基底クラス
    /// </summary>
    /// <typeparam name="T">格納対象の型</typeparam>
    public class ReadonlyContentStoreBase<T> : PinableContentDataStoreBase<T>
    {
        //データーストアのID一覧。初期状態はDEFAULT_IDとなる。
        protected const string DEFAULT_ID = nameof(ReadonlyContentStoreBase<T>);
        protected const string SECONDARY_DATA_STORE_ID = nameof(ReadonlyContentStoreBase<T>) + ".SecondaryDataStore";

        EmptyList emptyList = new EmptyList();
        TwoQueueCacheList<long, PinableContainer<T>> cacheList = null;
        IPinableContainerStore<T> _SecondaryDataStore;

        public IPinableContainerStore<T> SecondaryDataStore
        {
            get { return _SecondaryDataStore; }
            set { if (value == null) throw new ArgumentNullException(); _SecondaryDataStore = value; }
        }

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="cache_limit">キャッシュの量。すくなくとも、CacheParameters.MINCACHESIZE以上は指定する必要がある。</param>
        public ReadonlyContentStoreBase(int cache_limit = 128)
        {
            this.SecondaryDataStore = new MemoryPinableContentDataStore<T>();
            this.cacheList = new TwoQueueCacheList<long, PinableContainer<T>>();
            this.cacheList.Limit = cache_limit;
            this.cacheList.CacheOuted += (ev) => {
                var key = ev.Key;
                var outed_item = ev.Value;

                if (outed_item.IsRemoved == true)
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
        /// 初回読み込みを行う
        /// </summary>
        /// <param name="count">読み込みたい要素数</param>
        /// <returns>IPinableContainerを返す。これ以上読み取ることができない場合、nullを返す。</returns>
        public IPinableContainer<T> Load(OnLoadAsyncResult<T> read_result)
        {
            var content = read_result.Value;
            long index = read_result.Index;
            int read_bytes = read_result.ReadBytes;
            if (read_bytes == 0)
                return null;
            PinableContainer<T> newpin = (PinableContainer<T>)this.CreatePinableContainer(content);
            //ディスク上に存在するので永遠に保存しておく必要はない
            newpin.CacheIndex = PinableContainer<T>.NOTCACHED;
            newpin.Info = new DiskAllocationInfo(index, read_bytes);
            //まずはプライマリーデーターストアを使用する
            newpin.ID = DEFAULT_ID;
            return newpin;
        }

        /// <summary>
        /// 何らかの理由で再読み込みを行った際に呼び出される。例えば、メモリー上にある要素が不要になった後、再度、読み込む必要が出てきたときに呼び出される。
        /// </summary>
        /// <param name="index">読み込むべきインデックス。専らファイル上のアドレスを指す。</param>
        /// <param name="bytes">読み込むべきバイト数</param>
        /// <returns>読み込んだ要素を返す</returns>
        protected virtual T OnRead(long index, int bytes)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override IPinnedContent<T> Get(IPinableContainer<T> ipinableContainer)
        {
            switch (ipinableContainer.ID)
            {
                case SECONDARY_DATA_STORE_ID:
                    return this.SecondaryDataStore.Get(ipinableContainer);
            }

            IPinnedContent<T> result;
            if (this.TryGet(ipinableContainer, out result))
                return result;
            else
                throw new ArgumentException();  //TryGetが失敗することはあり得ないので、失敗したら、例外を投げる
        }

        /// <inheritdoc/>
        public override bool TryGet(IPinableContainer<T> ipinableContainer, out IPinnedContent<T> result)
        {
            switch (ipinableContainer.ID)
            {
                case SECONDARY_DATA_STORE_ID:
                    return this.SecondaryDataStore.TryGet(ipinableContainer, out result);
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
                //読み取り専用なので、Setの段階でキャッシュにセットすれば問題はない
            }

            return true;
        }

        /// <inheritdoc/>
        public override void Set(IPinableContainer<T> ipinableContainer)
        {
            switch (ipinableContainer.ID)
            {
                case SECONDARY_DATA_STORE_ID:
                    this.SecondaryDataStore.Set(ipinableContainer);
                    return;
            }

            if (EqualityComparer<T>.Default.Equals(ipinableContainer.Content, default(T)))
                return;

            //TryGetのほうでキャッシュにセットしてないのでここでセットする
            PinableContainer<T> pinableContainer = (PinableContainer<T>)ipinableContainer;
            if (pinableContainer.IsRemoved)
            {
                this.emptyList.ReleaseID(pinableContainer.CacheIndex);
                pinableContainer.Info = null;
                pinableContainer.Content = default(T);
                pinableContainer.CacheIndex = PinableContainer<T>.NOTCACHED;
                return;
            }

            if (pinableContainer.CacheIndex == PinableContainer<T>.NOTCACHED)
            {
                pinableContainer.CacheIndex = this.emptyList.GetID();
            }

            this.cacheList.Set(pinableContainer.CacheIndex, pinableContainer);
            return;
        }

        /// <inheritdoc/>
        public override void Commit()
        {
            this.cacheList.Flush();

            this.SecondaryDataStore.Commit();
        }

        /// <inheritdoc/>
        public override IPinableContainer<T> Update(IPinableContainer<T> pinableContainer, T newcontent, long oldstart, long oldcount, long newstart, long newcount)
        {
            //TODO:本当はコピーしないほうがいいが、面倒なので全部コピーする
            var updatedPinableContainer = this.SecondaryDataStore.Update(pinableContainer, newcontent, oldstart, oldcount, newstart, newcount);
            updatedPinableContainer.ID = SECONDARY_DATA_STORE_ID;
            return updatedPinableContainer;
        }

        /// <inheritdoc/>
        public override bool IsCanCloneContent(IPinableContainer<IComposableList<char>> pin)
        {
            switch (pin.ID)
            {
                case SECONDARY_DATA_STORE_ID:
                    return this.SecondaryDataStore.IsCanCloneContent(pin);
            }

            return true;
        }

        /// <inheritdoc/>
        /// <remarks>呼び出し前にCommit()を実行すること</remarks>
        public override IPinableContainer<T> Clone(IPinableContainer<T> pin, T cloned_content)
        {
            PinableContainer<T> newpin;
            switch (pin.ID)
            {
                case SECONDARY_DATA_STORE_ID:
                    {
                        newpin = (PinableContainer<T>)this.SecondaryDataStore.Clone(pin, cloned_content);
                        newpin.ID = SECONDARY_DATA_STORE_ID;
                        return newpin;
                    }
            }

            PinableContainer<T> oldpin = (PinableContainer<T>) pin;
            newpin = (PinableContainer<T>)this.CreatePinableContainer(cloned_content);
            newpin.CacheIndex = oldpin.CacheIndex;
            newpin.Info = new DiskAllocationInfo(oldpin.Info.Index,oldpin.Info.AlignedLength);
            newpin.ID = oldpin.ID;
            newpin.IsRemoved = oldpin.IsRemoved;

            return newpin;
        }

        /// <inheritdoc/>
        public override IPinableContainer<T> CreatePinableContainer(T content)
        {
            var newPinableContainer = new PinableContainer<T>(content);
            newPinableContainer.ID = SECONDARY_DATA_STORE_ID;
            return newPinableContainer;
        }

    }
}
