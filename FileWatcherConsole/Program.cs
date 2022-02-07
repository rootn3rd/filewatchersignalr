using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileWatcherConsole
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var path = @"D:\StudioWorks\NETCoreProjects\FileWatcherSignalR\FileWatcherConsole\data.txt";

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(300));

            var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            var streamWriter = new StreamWriter(fs);
            var random = new Random();
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                var num = random.Next(1000);
                Console.WriteLine(num);
                streamWriter.WriteLine(num);
                streamWriter.FlushAsync().Wait();
                Task.Delay(1000).Wait();
            }

            Console.WriteLine("Completed!");
            //var pathA = @"D:\StudioWorks\NETCoreProjects\FileWatcherSignalR\FileWatcherConsole\dummy\a.txt";
            //var pathB = @"D:\StudioWorks\NETCoreProjects\FileWatcherSignalR\FileWatcherConsole\dummy\b.txt";


            //FileMonitor.GetMonitor(pathA, (s) => Console.WriteLine($"A First =>{s}"));
            //FileMonitor.GetMonitor(pathA, (s) => Console.WriteLine($"A Second =>{s}"));


            //FileMonitor.GetMonitor(pathB, (s) => Console.WriteLine($"\tB First =>{s}"));


            Console.ReadLine();

        }

        private static void ReaderWriterExample()
        {
            var path = @"..\..\..\data.txt";

            _ = Task.Run(() =>
            {
                foreach (var item in collection.GetConsumingEnumerable())
                {
                    Console.WriteLine(item);
                }

            });

            var prefixes = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

            _ = Task.Run(() =>
            {

                while (true)
                {
                    Task.Delay(10000).Wait();

                    var cur = DateTime.Now.Ticks;

                    File.AppendAllLines(path, prefixes.Select(p => $"{cur}-{p}"));

                }

            });
            var dir = new DirectoryInfo(path);
            var watcher = new FileSystemWatcher(dir.Parent.FullName, "data.txt")
            {
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite
            };

            watcher.Changed += (s, e) => Console.WriteLine($"{s}- {e}");

            //while (true)
            //{
            //    Schedule(path);

            //    Task.Delay(5000).Wait();

            //    Console.WriteLine("Waiting...");
            //}

        }


        static long lastknownModified = 0l;
        static long lastknownPosition = 0l;
        static BlockingCollection<string> collection = new();


        static void Schedule(string path)
        {
            var currDate = File.GetLastWriteTimeUtc(path).Ticks;
            if (lastknownModified == currDate) return;

            using (var freader = new StreamReader(path))
            {
                freader.BaseStream.Seek(lastknownPosition, SeekOrigin.Begin);

                while (!freader.EndOfStream) collection.Add(freader.ReadLine());

                lastknownPosition = freader.BaseStream.Position;
            }
            lastknownModified = currDate;

        }


    }
}
