using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection;
using FooProject.Generator;

namespace TextReaderDemo
{
    [BigRleArrayFlagsAttribute]
    [Flags]
    public enum Marker
    {
        None = 0,
        Important = 1,
        Hilight = 2,
        Solid = 4,
    }

}
