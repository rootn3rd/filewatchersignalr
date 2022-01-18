using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FileWatcherConsole
{

    class FileMonitor : IDisposable
    {
        static ConcurrentDictionary<string, FileMonitor> s_FileMonitors = new();

        public static void GetMonitor(string path, Action<string> listener)
        {
            var monitor = s_FileMonitors.GetOrAdd(path, (p) => new FileMonitor(p));

            monitor.AddListener(listener);
        }


        string _path;
        private long _lastKnownPosition;
        private long _lastKnownModified;
        FileSystemWatcher _fileSystemWatcher;
        Channel<string> _channel;
        CancellationToken _cancellationToken;

        int _isReading = 0;

        List<Action<string>> _action = new List<Action<string>>();

        public FileMonitor(string path)
        {
            _path = path;

            _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(10)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
            });

            SetupFileWatcher();

            Read();

            _ = Task.Run(() => SetupListeners());

            _cancellationToken.Register(() => Dispose());

        }

        private async Task SetupListeners()
        {
            while (true)
            {
                if (_action.Count != 0)
                {
                    while (await _channel.Reader.WaitToReadAsync(_cancellationToken))
                    {
                        while (_channel.Reader.TryRead(out string item))
                        {
                            _action.ForEach(a => a(item));
                        }
                    }
                }

                await Task.Delay(10000);
            }
        }

        public void AddListener(Action<string> listener) => _action.Add(listener);


        private void SetupFileWatcher()
        {
            var dirInfo = new DirectoryInfo(_path);
            var fileName = Path.GetFileName(_path);
            _fileSystemWatcher = new FileSystemWatcher(dirInfo.Parent.FullName, fileName)
            {
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite
            };

            _fileSystemWatcher.Changed += (s, e) =>
            {
                Read();
            };
        }

        private void Read()
        {
            var currDate = File.GetLastWriteTimeUtc(_path).Ticks;

            if (_lastKnownModified == currDate) return;

            if (Interlocked.Exchange(ref _isReading, 1) == 1) return;

            using var fileReader = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var freader = new StreamReader(fileReader);

            _ = freader.BaseStream.Seek(_lastKnownPosition, SeekOrigin.Begin);

            while (!freader.EndOfStream) _channel.Writer.TryWrite(freader.ReadLine());

            _lastKnownModified = currDate;
            _lastKnownPosition = freader.BaseStream.Position;

            Interlocked.Exchange(ref _isReading, 0);
        }

        public void Dispose()
        {
            _fileSystemWatcher?.Dispose();
            _channel.Writer.Complete();
        }
    }
}
