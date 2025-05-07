using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection
{
    public class PinableContainer<T>
    {
        long index;
        public T Content { get; private set; }

        public PinableContainer(T content)
        {
            this.Content = content;
            this.index = -1;
        }

        public void SetConent(long index, T content)
        {
            this.Content = content;
            this.index = index;
        }

        public void RemoveContent()
        {
            this.Content = default(T);
        }
    }
}
