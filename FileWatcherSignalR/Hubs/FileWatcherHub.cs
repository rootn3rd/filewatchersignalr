using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FileWatcherSignalR.Hubs
{
    public class FileWatcherHub : Hub
    {

        static FileWatcherHub()
        {
            SetupChanger();
        }

        private static void SetupChanger()
        {
            _ = Task.Run(async () =>
            {
                var prefixes = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

                while (true)
                {
                    Task.Delay(10000).Wait();

                    var cur = DateTime.Now.Ticks;
                    try
                    {
                        using (var fileWriter = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                        {
                            using (var fwriter = new StreamWriter(fileWriter))
                            {
                                foreach (var p in prefixes)
                                {
                                    var str = $"{cur}-{p}\r\n";
                                    await fileWriter.WriteAsync(Encoding.UTF8.GetBytes(str));
                                }

                            }
                        }

                        //File.AppendAllLines(path, prefixes.Select(p => $"{cur}-{p}"));
                        //using (var writer = new StreamWriter(path))
                        //{
                        //    var allTasks = prefixes.Select(p => writer.WriteLineAsync($"{cur}-{p}")).ToArray();

                        //    await Task.WhenAll(allTasks);

                        //    await writer.WriteLineAsync("-----------------");
                        //}
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                    }
                }
            });
        }
        static string path = @"D:\StudioWorks\NETCoreProjects\FileWatcherSignalR\FileWatcherSignalR\data.txt";

        static List<string> LastEntries = Enumerable.Range(0, 10).Select(i => $"Initial Data - {i}").ToList();

        static bool isRunning = false;
        public async Task GetContents()
        {

            if (isRunning) return;

            isRunning = true;

            var f_info = s_Files.GetOrAdd(path, (p) =>
            {
                return new FileInfo() { path = p };
            });

            Action<string, CancellationToken> action = async (d, t) =>
            {
                await Clients.All.SendAsync(d, t);

                await Task.Delay(1000);
            };

            await ReadFromFileAsync(f_info, action, new CancellationToken());
        }

        public override Task OnConnectedAsync()
        {
            return Clients.Caller.SendAsync("initialData", LastEntries);
        }

        static ConcurrentDictionary<string, FileInfo> s_Files = new();

        public IHubContext<FileWatcherHub> HubContext { get; }

        public ChannelReader<string> Watch(CancellationToken token)
        {
            var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(20)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });

            var f_info = s_Files.GetOrAdd(path, (p) =>
            {
                return new FileInfo() { path = p };
            });

            _ = ReadFromFileAsync(f_info, async (data, t) => await channel.Writer.WriteAsync(data, t), token);

            return channel.Reader;
        }
        class FileInfo
        {
            public string path { get; set; }
            public long lastknownModified { get; set; } = DateTime.MinValue.Ticks;
            public long lastknownPosition { get; set; } = 0;
        }

        private static async Task ReadFromFileAsync(FileInfo fileInfo, Action<string, CancellationToken> writerCallback, CancellationToken token)
        {

            while (true)
            {
                await f_Schedule(fileInfo, writerCallback, token);

                await Task.Delay(5000, token);
            }

            async Task f_Schedule(FileInfo fileInfo, Action<string, CancellationToken> writerCallback, CancellationToken token)
            {

                var currDate = File.GetLastWriteTimeUtc(path).Ticks;
                if (fileInfo.lastknownModified == currDate) return;

                var exception = default(Exception);
                try
                {
                    using (var fileReader = new FileStream(fileInfo.path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var freader = new StreamReader(fileReader))
                        {
                            freader.BaseStream.Seek(fileInfo.lastknownPosition, SeekOrigin.Begin);

                            while (!freader.EndOfStream)
                            {
                                writerCallback($"{freader.ReadLine()}", token); // writer.WriteAsync($"{freader.ReadLine()}", token);

                                await Task.Delay(200, token);
                            }

                            fileInfo.lastknownPosition = freader.BaseStream.Position;
                        }
                    }

                    fileInfo.lastknownModified = currDate;


                }
                catch (Exception ex)
                {
                    exception = ex;
                    Console.WriteLine(ex);
                }
                finally
                {
                    //writer.Complete(exception);
                }
            }
        }


        public ChannelReader<string> MultipleWatcher(CancellationToken token)
        {
            var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(10)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });

            token.Register(() => Console.WriteLine("Something just cancelled!!"));


            _ = Task.Run(async () =>
            {


                var fileInfo = new FileInfo() { path = path };

                var dir = new DirectoryInfo(path);
                var watcher = new FileSystemWatcher(dir.Parent.FullName, "data.txt")
                {
                    EnableRaisingEvents = true,
                    NotifyFilter = NotifyFilters.LastWrite
                };

                watcher.Changed += (s, e) => Task.Run(() => f_Schedule());
                _ = f_Schedule();

                //while (!token.IsCancellationRequested)
                //{
                //    await f_Schedule();

                //    await Task.Delay(5000);
                //}

                async Task f_Schedule()
                {

                    var currDate = File.GetLastWriteTimeUtc(path).Ticks;
                    if (fileInfo.lastknownModified == currDate) return;

                    var exception = default(Exception);
                    try
                    {
                        using (var fileReader = new FileStream(fileInfo.path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            using (var freader = new StreamReader(fileReader))
                            {
                                freader.BaseStream.Seek(fileInfo.lastknownPosition, SeekOrigin.Begin);

                                while (!freader.EndOfStream)
                                {
                                    //writerCallback($"{freader.ReadLine()}", token); // writer.WriteAsync($"{freader.ReadLine()}", token);
                                    await channel.Writer.WriteAsync($"{freader.ReadLine()}");

                                    await Task.Delay(50);
                                }

                                fileInfo.lastknownPosition = freader.BaseStream.Position;
                            }
                        }
                        fileInfo.lastknownModified = currDate;


                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        Console.WriteLine("Ex from reader - " + ex);
                    }
                    finally
                    {
                        //writer.Complete(exception);
                    }
                }
            });

            return channel.Reader;

        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
