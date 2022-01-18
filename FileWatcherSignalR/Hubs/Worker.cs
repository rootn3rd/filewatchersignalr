using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileWatcherSignalR.Hubs
{


    public class Worker : BackgroundService
    {
        class FileInfo
        {
            public string path { get; set; }
            public long lastknownModified { get; set; } = DateTime.MinValue.Ticks;
            public long lastknownPosition { get; set; } = 0;
        }


        static string path = @".\data.txt";
        public Worker(IHubContext<FileWatcherHub> hubContext)
        {
            HubContext = hubContext;
            var directory = new DirectoryInfo(path);
            Console.WriteLine($"Path - {directory.FullName}");
        }

        public IHubContext<FileWatcherHub> HubContext { get; }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var fileInfo = new FileInfo() { path = path };

            Action<string, CancellationToken> action = async (d, t) =>
            {
                await HubContext.Clients.All.SendAsync("dataRead", d, t);
            };

            return ReadFromFileAsync(fileInfo, action, stoppingToken);
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

                    using (var freader = new StreamReader(path))
                    {
                        freader.BaseStream.Seek(fileInfo.lastknownPosition, SeekOrigin.Begin);

                        while (!freader.EndOfStream)
                        {
                            writerCallback($"{freader.ReadLine()}", token); // writer.WriteAsync($"{freader.ReadLine()}", token);

                            await Task.Delay(200, token);
                        }

                        fileInfo.lastknownPosition = freader.BaseStream.Position;
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
    }
}
