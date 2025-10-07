using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;
using CommandLine;
using FooProject.Collection;
using FooProject.Collection.DataStore;
using SharedDemoProgram;
using TextReaderDemo;
using static System.Runtime.InteropServices.JavaScript.JSType;

const int DEFAULT_SHOW_LENGTH = 1024;

Console.WriteLine("open filename to show?");
string filepath = Console.ReadLine().Trim('"');

FileStream stream = null;
try
{
    stream = new FileStream(filepath, FileMode.Open,FileAccess.Read,FileShare.Read);
}
catch (DirectoryNotFoundException ex)
{
    Console.WriteLine(ex.Message);
    Environment.Exit(1);
}
catch (IOException ex)
{
    Console.WriteLine(ex.Message);
    Environment.Exit(1);
}
catch (UnauthorizedAccessException ex)
{
    Console.WriteLine(ex.Message);
    Environment.Exit(1);
}

Console.WriteLine("Do you want to use diskbase store for edit file?(yes/no)");
string usedisk = Console.ReadLine();
IPinableContainerStore<IComposableList<char>> store;
if (usedisk == "yes")
{
    var serializer = new StringBufferSerializer();
    //ファイナライザーで消えるので何もしなくてもいい
    store = new DiskPinableContentDataStore<IComposableList<char>>(serializer);
}
else
{
    store = new MemoryPinableContentDataStore<IComposableList<char>>();
}

var charReader = new CharReader(stream, Encoding.UTF8);
var lazyLoadStore = new ReadOnlyCharDataStore(charReader);
lazyLoadStore.SecondaryDataStore = store;
var customConverter = new DefaultCustomConverter<char>();
customConverter.DataStore = lazyLoadStore;
BigList<char> biglist1 = new BigList<char>();
biglist1.CustomBuilder = customConverter;
biglist1.LeastFetchStore = customConverter;
biglist1.BlockSize = 32768;

var exitflag = false;
while (exitflag == false)
{
    Console.WriteLine("");
    Console.WriteLine("input command(if want to show help text,input help):");
    string cmd = Console.ReadLine();

    string[] cmds = cmd.Split(" ");
    await Parser.Default.ParseArguments<LoadCommnad, LoadAllCommnad, LoadAsyncAllCommnad, SaveCommand, UsageCommnad, InsertCommand, RemoveCommand, ShowCommand, ExitCommnad, FindCommand>(cmds)
        .WithParsed<LoadCommnad>(opt =>
        {
            var time = BenchmarkRunner.Run(() =>
            {
                for (int i = 0; i < opt.Count; i++)
                {
                    var pinableContainer = lazyLoadStore.Load(biglist1.BlockSize);
                    if (pinableContainer == null)
                        break;
                    biglist1.Add(pinableContainer);
                }
            });
            Console.WriteLine($"success to load. elapsed time:{time} ms");
        })
        .WithParsed<LoadAllCommnad>(opt => {
            var time = BenchmarkRunner.Run(() => {
                while (true)
                {
                    var pinableContainer = lazyLoadStore.Load(biglist1.BlockSize);
                    if (pinableContainer == null)
                        break;
                    biglist1.Add(pinableContainer);
                }
            });
            Console.WriteLine($"success to load.elapsed time:{time} ms");
        })
        .WithParsed<SaveCommand>(opt =>
        {
            try
            {
                var time = BenchmarkRunner.Run(() =>
                {
                    if (string.IsNullOrEmpty(opt.FilePath))
                    {
                        Console.WriteLine("must be set filepath");
                        return;
                    }
                    using (var saveFileStream = new FileStream(opt.FilePath.Trim('"'), FileMode.OpenOrCreate))
                    {
                        saveFileStream.Position = 0;
                        using (var streamWriter = new StreamWriter(saveFileStream))
                        {
                            foreach (var item in biglist1.Chunk(biglist1.BlockSize))
                            {
                                streamWriter.Write(item);
                            }
                        }
                    }
                });
                Console.WriteLine($"success to save {opt.FilePath}. elapsed time:{time} ms");
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine(ex.Message);
            }
        })
        .WithParsed<UsageCommnad>(opt =>
        {
            Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");
            Console.WriteLine("Loaded Char Count:" + biglist1.Count);
        })
        .WithParsed<InsertCommand>(opt =>
        {
            var number = opt.Index;
            if (number >= 0 && number < biglist1.Count)
            {
                var time = BenchmarkRunner.Run(() =>
                {
                    biglist1.InsertRange(number, opt.Text);
                });
                Console.WriteLine($"success.elapsed time:{time} ms");
            }
            else
            {
                Console.WriteLine("too large index");
            }
        })
        .WithParsed<RemoveCommand>(opt =>
        {
            var number = opt.Index;
            if (number >= 0 && number < biglist1.Count)
            {
                var length = opt.Length;
                if (number + length > biglist1.Count)
                {
                    length = biglist1.Count - number;
                }
                var time = BenchmarkRunner.Run(() =>
                {
                    biglist1.RemoveRange(number, length);
                });
                Console.WriteLine($"success.elapsed time:{time} ms");
            }
            else
            {
                Console.WriteLine("too large index");
            }
        })
        .WithParsed<ShowCommand>(opt =>
        {
            Console.WriteLine("");
            var number = opt.Index;
            if (number >= 0 && number < biglist1.Count)
            {
                var length = opt.Length;

                if (opt.Length == 0)
                    length = DEFAULT_SHOW_LENGTH;

                if (number + length > biglist1.Count)
                {
                    length = biglist1.Count - number;
                }
                string text = string.Empty;
                var time = BenchmarkRunner.Run(() =>
                {
                    text = new string(biglist1.GetRangeEnumerable(number, length).ToArray());
                });
                Console.WriteLine(text);
                Console.WriteLine($"elapsed time:{time} ms");
            }
            else
            {
                Console.WriteLine("too large index");
            }
        })
        .WithParsed<FindCommand>(opt =>
        {
            var number = opt.Index;
            if (number >= 0 && number < biglist1.Count)
            {
                var length = opt.Length;

                if (opt.Length == 0)
                    length = biglist1.Count;

                if (number + length > biglist1.Count)
                {
                    length = biglist1.Count - number;
                }
                long foundIndex = -1;
                var time = BenchmarkRunner.Run(() =>
                {
                    var textSearch = new TextSearch(opt.Text, opt.CaseInsensitive);
                    foundIndex = textSearch.IndexOf(biglist1, number, length);
                });
                if (foundIndex == -1)
                {
                    Console.WriteLine("not found");
                }
                else
                {
                    Console.WriteLine($"found text index:{foundIndex}");
                }
                Console.WriteLine($"elapsed time:{time} ms");
            }
            else
            {
                Console.WriteLine("too large index");
            }

        })
        .WithParsed<ExitCommnad>(opt =>
        {
            exitflag = true;
        })
        .WithParsedAsync<LoadAsyncAllCommnad>(async opt =>
        {
            var time = await BenchmarkRunner.RunAsync( async () =>
            {
                while (true)
                {
                    var pinableContainer = await lazyLoadStore.LoadAsync(biglist1.BlockSize);
                    if (pinableContainer == null)
                        break;
                    biglist1.Add(pinableContainer);
                }
            });
            Console.WriteLine($"success to load.elapsed time:{time} ms");
        });
}
