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
        CacheList<long, PinableContainer<T>> writebackCacheList = null;

        public MemoryPinableContentDataStoreWithAutoDisposer(int cache_limit = 128)
        {
            this.writebackCacheList = new CacheList<long, PinableContainer<T>>();
            this.writebackCacheList.Limit = cache_limit;
            this.writebackCacheList.CacheOuted += (outed_index, outed_item) => {
                this.OnDispoing(outed_item.Content);
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

        public PinnedContent<T> Get(PinableContainer<T> pinableContainer)
        {
            PinnedContent<T> result;
            if (TryGet(pinableContainer, out result))
                return result;
            else
                throw new ArgumentException();            
        }

        public bool TryGet(PinableContainer<T> pinableContainer, out PinnedContent<T> result)
        {
            result = new PinnedContent<T>(pinableContainer,this);
            return true;
        }

        public void Set(PinableContainer<T> pinableContainer)
        {
            if (pinableContainer.IsRemoved)
            {
                this.emptyList.SetID(pinableContainer.CacheIndex);
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
