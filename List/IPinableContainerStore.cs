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
        PinnedContent<T> Get(PinableContainer<T> pinableContainer);

        bool TryGet(PinableContainer<T> pinableContainer, out PinnedContent<T> result);

        void Set(PinableContainer<T> pinableContainer);
    }
}
