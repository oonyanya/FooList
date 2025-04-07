using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection;

namespace EditorDemo
{
    class LineToIndex : IRange
    {
        public long start { get; set; }
        public long length { get; set; }

        public LineToIndex(long index, long length)
        {
            start = index;
            this.length = length;
        }
        public LineToIndex()
        {
        }

        public IRange DeepCopy()
        {
            return new LineToIndex(start, length);
        }
    }
}
