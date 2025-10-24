using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    public class MemoryPinableContentDataStoreWithAutoDisposer<T> : IPinableContainerStoreWithAutoDisposer<T>, IDisposable
    {
        EmptyList emptyList = new EmptyList();
        bool disposedValue = false;
        TwoQueueCacheList<long, PinableContainer<T>> writebackCacheList = null;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="cache_limit">キャッシュの量。すくなくとも、CacheParameters.MINCACHESIZE以上は指定する必要がある。</param>
        public MemoryPinableContentDataStoreWithAutoDisposer(int cache_limit = 128)
        {
            this.writebackCacheList = new TwoQueueCacheList<long, PinableContainer<T>>();
            this.writebackCacheList.Limit = cache_limit;
            this.writebackCacheList.CacheOuted += (ev) => {
                var key = ev.Key;
                var outed_item = ev.Value;

                this.OnDispoing(outed_item.Content);

                if (outed_item.IsRemoved == true)
                    return;

                this.emptyList.ReleaseID(outed_item.CacheIndex);
                outed_item.CacheIndex = PinableContainer<T>.NOTCACHED;
            };
        }

        public event Action<T> Disposeing;

        public void OnDispoing(T item)
        {
            if (this.Disposeing != null)
                this.Disposeing(item);
        }

        public IEnumerable<T> ForEachAvailableContent()
        {
            foreach (var pinableContainer in this.writebackCacheList.ForEachValue())
            {
                if (pinableContainer.CacheIndex != PinableContainer<T>.NOTCACHED || pinableContainer.Content?.Equals(default(T)) == false)
                {
                    yield return pinableContainer.Content;
                }
            }
        }

        public IPinnedContent<T> Get(IPinableContainer<T> pinableContainer)
        {
            IPinnedContent<T> result;
            if (TryGet(pinableContainer, out result))
                return result;
            else
                throw new ArgumentException();            
        }

        public bool TryGet(IPinableContainer<T> pinableContainer, out IPinnedContent<T> result)
        {
            result = new PinnedContent<T>(pinableContainer,this);
            return true;
        }

        public void Set(IPinableContainer<T> ipinableContainer)
        {
            PinableContainer<T> pinableContainer = (PinableContainer<T>)ipinableContainer;
            if (pinableContainer.IsRemoved)
            {
                this.emptyList.ReleaseID(pinableContainer.CacheIndex);
                pinableContainer.CacheIndex = PinableContainer<T>.NOTCACHED;
                return;
            }

            if (pinableContainer.CacheIndex == PinableContainer<T>.NOTCACHED)
            {
                pinableContainer.CacheIndex = this.emptyList.GetID();
            }

            this.writebackCacheList.Set(pinableContainer.CacheIndex, pinableContainer);
            return;
        }

        public void Commit()
        {
            this.writebackCacheList.Flush();
        }

        public IPinableContainer<T> Update(IPinableContainer<T> pinableContainer, T newcontent, long oldstart, long oldcount, long newstart, long newcount)
        {
            return this.CreatePinableContainer(newcontent);
        }

        public IPinableContainer<T> CreatePinableContainer(T content)
        {
            return new PinableContainer<T>(content) { ID = nameof(MemoryPinableContentDataStoreWithAutoDisposer<T>) };
        }

        public IPinableContainer<T> Clone(IPinableContainer<T> pin, T cloned_content = default(T))
        {
            if (cloned_content.Equals(default(T)))
                return this.CreatePinableContainer(pin.Content);
            else
                return this.CreatePinableContainer(cloned_content);
        }

        public void Dispose()
        {
            //GC前にプログラム的にリソースを破棄するので
            //管理,非管理リソース両方が破棄されるようにする
            Dispose(true);
            GC.SuppressFinalize(this);//破棄処理は完了しているのでGC不要の合図
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue)
            {
                return;
            }

            if (disposing)
            {
                //管理リソースの破棄処理
                this.writebackCacheList.Flush();
            }

            //非管理リソースの破棄処理

            disposedValue = true;
        }

        ~MemoryPinableContentDataStoreWithAutoDisposer()
        {
            //GC時に実行されるデストラクタでは非管理リソースの削除のみ
            Dispose(false);
        }
    }
}
