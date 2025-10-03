using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace TextReaderDemo
{
    [Verb("load", HelpText = "Load block")]
    public class LoadCommnad
    {
        [Value(0, MetaName = "CountValue")]
        public int Count { get; set; }
    }

    [Verb("loadall", HelpText = "Load entier file")]
    public class LoadAllCommnad
    {
        [Value(0)]
        IEnumerable<string> args { get; set; }
    }

    [Verb("exit", HelpText = "Exit this progoram")]
    public class ExitCommnad
    {
        [Value(0)]
        IEnumerable<string> args { get; set; }
    }

    [Verb("save",HelpText ="Save entier content to file")]
    public class SaveCommand
    {
        [Value(0, MetaName = "FilePathValue")]
        public string FilePath { get; set; }
    }

    [Verb("usage", HelpText = "Show current state")]
    public class UsageCommnad
    {
        [Value(0)]
        IEnumerable<string> args { get; set; }
    }

    [Verb("insert", HelpText = "Insert text to index")]
    internal class InsertCommand
    {
        [Value(0, MetaName = "IndexValue")]
        public int Index { get; set; }

        [Value(1, MetaName = "TextValue")]
        public string Text { get; set; }
    }

    [Verb("show", HelpText = "Show text from index")]
    internal class ShowCommand
    {
        [Value(0, MetaName = "IndexValue")]
        public int Index { get; set; }

        [Value(1, MetaName = "LengthValue",Required = false)]
        public int Length { get; set; }
    }

    [Verb("remove", HelpText = "Remove text from index with length")]
    internal class RemoveCommand
    {
        [Value(0, MetaName = "IndexValue")]
        public int Index { get; set; }

        [Value(1, MetaName = "LengthValue")]
        public int Length { get; set; }
    }
}
