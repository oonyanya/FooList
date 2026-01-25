using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    public class MemoryPinableContentDataStoreWithAutoDisposer<T> : PinableContentDataStoreWithAutoDisposerBase<T>
    {
        IAllocator emptyList = new EmptyAllocator();
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

        /// <inheritdoc/>
        public override IEnumerable<T> ForEachAvailableContent()
        {
            foreach (var pinableContainer in this.writebackCacheList.ForEachValue())
            {
                if (pinableContainer.CacheIndex != PinableContainer<T>.NOTCACHED || pinableContainer.Content?.Equals(default(T)) == false)
                {
                    yield return pinableContainer.Content;
                }
            }
        }

        /// <inheritdoc/>
        public override bool TryGet(IPinableContainer<T> pinableContainer, out IPinnedContent<T> result)
        {
            result = new PinnedContent<T>(pinableContainer,this);
            return true;
        }

        /// <inheritdoc/>
        public override void Set(IPinableContainer<T> ipinableContainer)
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

        /// <inheritdoc/>
        public override void Commit()
        {
            this.writebackCacheList.Flush();
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                this.writebackCacheList.Flush();
            }
        }

        /// <inheritdoc/>
        public override IPinableContainer<T> CreatePinableContainer(T content)
        {
            return new PinableContainer<T>(content) { ID = nameof(MemoryPinableContentDataStoreWithAutoDisposer<T>) };
        }

    }
}
