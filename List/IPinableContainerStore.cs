using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection.DataStore;

namespace FooProject.Collection
{
    public interface IPinableContainerStore<T>
    {
        IPinnedContent<T> Get(IPinableContainer<T> pinableContainer);

        bool TryGet(IPinableContainer<T> pinableContainer, out IPinnedContent<T> result);

        void Set(IPinableContainer<T> pinableContainer);

        void Commit();
    }

    public interface IPinableContainerStoreWithAutoDisposer<T> : IPinableContainerStore<T>
    {
        event Action<T> Disposeing;

        IEnumerable<T> ForEachAvailableContent();
    }
}
