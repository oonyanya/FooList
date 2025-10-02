using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace TextReaderDemo
{
    [Verb("load")]
    public class LoadCommnad
    {
        [Value(0, MetaName = "CountValue")]
        public int Count { get; set; }
    }

    [Verb("loadall")]
    public class LoadAllCommnad
    {
        [Value(0)]
        IEnumerable<string> args { get; set; }
    }

    [Verb("exit")]
    public class ExitCommnad
    {
        [Value(0)]
        IEnumerable<string> args { get; set; }
    }

    [Verb("usage")]
    public class UsageCommnad
    {
        [Value(0)]
        IEnumerable<string> args { get; set; }
    }

    [Verb("insert")]
    internal class InsertCommand
    {
        [Value(0, MetaName = "IndexValue")]
        public int Index { get; set; }

        [Value(1, MetaName = "TextValue")]
        public string Text { get; set; }
    }

    [Verb("show")]
    internal class ShowCommand
    {
        [Value(0, MetaName = "IndexValue")]
        public int Index { get; set; }
    }

    [Verb("remove")]
    internal class RemoveCommand
    {
        [Value(0, MetaName = "IndexValue")]
        public int Index { get; set; }

        [Value(1, MetaName = "LengthValue")]
        public int Length { get; set; }
    }
}
