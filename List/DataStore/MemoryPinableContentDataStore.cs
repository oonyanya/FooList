using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    internal class MemoryPinableContentDataStore<T> : IPinableContainerStore<T>
    {
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
            return;
        }
    }
}
