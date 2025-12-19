using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace TextReaderDemo
{
    // https://github.com/commandlineparser/commandline/issues/810
    public static class CommandLineExtensions
    {
        public static Task<ParserResult<object>> WithJobAsync<TJob>(
            this ParserResult<object> parserResult,
            Func<TJob, Task> parsedFunc) where TJob : IJob =>
            parserResult.WithParsedAsync(parsedFunc);

        public static async Task<ParserResult<object>> WithJobAsync<TJob>(
            this Task<ParserResult<object>> parserResultTask,
            Func<TJob, Task> parsedFunc) where TJob : IJob
        {
            var parserResult = await parserResultTask;

            return await parserResult.WithParsedAsync(parsedFunc);
        }

        public static async Task WithNotParsedAsync(
            this Task<ParserResult<object>> parserResultTask,
            Action<IEnumerable<Error>> notParsedAction)
        {
            var parserResult = await parserResultTask;

            parserResult.WithNotParsed(notParsedAction);
        }
    }

    // https://github.com/commandlineparser/commandline/issues/810
    public interface IJob
    {
    }

    [Verb("load", HelpText = "Load block")]
    public class LoadCommnad : IJob
    {
        [Value(0, MetaName = "CountValue")]
        public int Count { get; set; }
    }

    [Verb("loadasync", HelpText = "Load block asynchronously")]
    public class LoadAsyncCommnad : IJob
    {
        [Value(0, MetaName = "CountValue")]
        public int Count { get; set; }
    }

    [Verb("loadall", HelpText = "Load entier file")]
    public class LoadAllCommnad : IJob
    {
        [Value(0)]
        IEnumerable<string> args { get; set; }
    }

    [Verb("loadasyncall", HelpText = "Load entire file asynchronously")]
    public class LoadAsyncAllCommnad : IJob
    {
        [Value(0)]
        IEnumerable<string> args { get; set; }
    }

    [Verb("exit", HelpText = "Exit this progoram")]
    public class ExitCommnad : IJob
    {
        [Value(0)]
        IEnumerable<string> args { get; set; }
    }

    [Verb("save",HelpText ="Save entier content to file")]
    public class SaveCommand : IJob
    {
        [Value(0, MetaName = "FilePathValue")]
        public string FilePath { get; set; }
    }

    [Verb("usage", HelpText = "Show current state")]
    public class UsageCommnad : IJob
    {
        [Value(0)]
        IEnumerable<string> args { get; set; }
    }

    [Verb("insert", HelpText = "Insert text to index")]
    internal class InsertCommand : IJob
    {
        [Value(0, MetaName = "IndexValue")]
        public int Index { get; set; }

        [Value(1, MetaName = "TextValue")]
        public string Text { get; set; }
    }

    [Verb("show", HelpText = "Show text from index")]
    internal class ShowCommand : IJob
    {
        [Value(0, MetaName = "IndexValue")]
        public int Index { get; set; }

        [Value(1, MetaName = "LengthValue",Required = false)]
        public int Length { get; set; }
    }

    [Verb("remove", HelpText = "Remove text from index with length")]
    internal class RemoveCommand : IJob
    {
        [Value(0, MetaName = "IndexValue")]
        public int Index { get; set; }

        [Value(1, MetaName = "LengthValue")]
        public int Length { get; set; }
    }

    [Verb("find", HelpText = "Find string within loaded text")]
    internal class FindCommand : IJob
    {
        [Value(0, MetaName = "TextValue")]
        public string Text { get; set; }

        [Value(1, MetaName = "IndexValue", Required = false)]
        public int Index { get; set; }

        [Value(2, MetaName = "LengthValue", Required = false)]
        public int Length { get; set; }

        [Option("ci", Required = false, Default = false )]
        public bool CaseInsensitive { get; set; }

    }
}
