using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection
{
    public interface IComposableList<T> : IEnumerable<T>, IList<T>
    {
        void AddRange(IEnumerable<T> collection, int collection_length = -1);
        void InsertRange(int index, IEnumerable<T> collection, int collection_length = -1);
        void RemoveRange(int index, int count);
        IEnumerable<T> GetRange(int index, int count);
    }
}
