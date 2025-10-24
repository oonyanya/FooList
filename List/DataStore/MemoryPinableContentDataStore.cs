using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace FooProject.Collection.DataStore
{
    public class MemoryPinableContentDataStore<T> : PinableContentDataStoreBase<T>
    {
        public override bool TryGet(IPinableContainer<T> pinableContainer, out IPinnedContent<T> result)
        {
            result = new PinnedContent<T>(pinableContainer,this);
            return true;
        }

        public override void Set(IPinableContainer<T> pinableContainer)
        {
            return;
        }

        public override IPinableContainer<T> CreatePinableContainer(T content)
        {
            return new PinableContainer<T>(content) { ID = nameof(MemoryPinableContentDataStore<T>) };
        }
    }
}
