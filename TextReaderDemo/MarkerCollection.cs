using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection;

namespace EditorDemo
{
    [Flags]
    public enum Marker
    {
        None = 0,
        Hilight = 1,
    }

    public class MarkerCollection
    {
        BigRleArray<Marker> collection = new BigRleArray<Marker>();

        public void Add(Marker m,int length)
        {
            collection.AddRange(m, length);
        }

        public Marker Get(long index)
        {
            return (Marker)collection.GetValue(index);
        }

        public void Set(int index,int count, Marker value)
        {
            collection.UpdateRange(index, value, count, (container, require_count, inputed_value) =>
            {
                var new_value = container.Value | inputed_value;
                return new BigRleArrayRange<Marker>(new_value, require_count); 
            });
        }

        public void Unset(int index, int count, Marker value)
        {
            collection.UpdateRange(index, value, count, (container, require_count, inputed_value) =>
            {
                var new_value = container.Value ^ inputed_value;
                return new BigRleArrayRange<Marker>(new_value, require_count);
            });
        }

    }
}
