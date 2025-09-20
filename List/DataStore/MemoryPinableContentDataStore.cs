using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace FooProject.Collection.DataStore
{
    public class MemoryPinableContentDataStore<T> : IPinableContainerStore<T>
    {
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

        public void Set(IPinableContainer<T> pinableContainer)
        {
            return;
        }

        public void Commit()
        {
        }

        public IPinableContainer<T> Update(IPinableContainer<T> pinableContainer, T newcontent, long oldstart, long oldcount, long newstart, long newcount)
        {
            return this.CreatePinableContainer(newcontent);
        }

        public IPinableContainer<T> CreatePinableContainer(T content)
        {
            return new PinableContainer<T>(content);
        }
    }
}
