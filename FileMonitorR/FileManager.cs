using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Linq;

namespace FileMonitorR
{
    public class FileManager : IFileManager
    {
        private readonly ConcurrentDictionary<string, FileWatcher> s_store = new();


        public FileManager(IHubContext<FileMonitorHub> hub)
        {
            Hub = hub;
        }

        public IHubContext<FileMonitorHub> Hub { get; }

        public IEnumerable<string> GetLastLines(string path)
        {
            var streamer = s_store.GetOrAdd(path, p => new FileWatcher(p));
            streamer.Read();
            return streamer.LastLines;
        }

        public IObservable<string> StreamContents(string path)
        {
            var streamer = s_store.GetOrAdd(path, p => new FileWatcher(p));

            return streamer.Contents.Zip(Observable.Interval(TimeSpan.FromMilliseconds(200)), (a, b) => a);

        }

        public bool IsStreaming(string path) => s_store.ContainsKey(path);

        class FileWatcher : IDisposable
        {
            string _path;
            FileSystemWatcher _fileSystemWatcher;
            int _isReading = 0;
            long _lastKnownPosition = 0;
            long _lastModifiedDateTime = 0;
            Subject<string> _subject = new();

            LinkedList<string> lastLines = new();


            public IEnumerable<string> LastLines
            {
                get { return lastLines; }
            }

            public FileWatcher(string path)
            {
                _path = path;
                SetupFileWatcher();
            }

            public IObservable<string> Contents
            {
                get { return _subject; }
            }

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

            public void Read()
            {
                var lastModified = File.GetLastWriteTimeUtc(_path).Ticks;
                if (lastModified == _lastModifiedDateTime) return;

                if (Interlocked.CompareExchange(ref _isReading, 0, 1) == 1) return;

                // do the reading
                using var fileStream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fileStream);

                _ = reader.BaseStream.Seek(_lastKnownPosition, SeekOrigin.Begin);

                while (!reader.EndOfStream)
                {
                    var str = reader.ReadLine();
                    if (lastLines.Count == 10) lastLines.RemoveFirst();
                    lastLines.AddLast(str);

                    _subject.OnNext(str);
                }

                _lastKnownPosition = reader.BaseStream.Position;
                _lastModifiedDateTime = lastModified;

                Interlocked.Decrement(ref _isReading);

            }

            public void Dispose()
            {
                _fileSystemWatcher?.Dispose();
            }
        }
    }


    public interface IFileManager
    {
        IEnumerable<string> GetLastLines(string path);

        IObservable<string> StreamContents(string path);

        bool IsStreaming(string path);
    }
}
