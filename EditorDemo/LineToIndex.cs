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
        public int start { get; set; }
        public int length { get; set; }

        public LineToIndex(int index, int length)
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
