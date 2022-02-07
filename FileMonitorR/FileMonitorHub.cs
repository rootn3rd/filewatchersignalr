using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FileMonitorR
{
    public class FileMonitorHub : Hub
    {
        private readonly IFileManager _fileManager;

        public FileMonitorHub(IFileManager fileManager)
        {
            _fileManager = fileManager;
        }

        public ChannelReader<string> StreamContents(string path)
        {
            return _fileManager.StreamContents(path).AsChannelReader();
        }

        public IEnumerable<string> GetLastTenElements(string path)
        {
            return _fileManager.GetLastLines(path);
        }

        public bool GetFileStat(string path) => _fileManager.IsStreaming(path);

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

    }

    public static class FileMonitorHubExtensions
    {
        public static ChannelReader<T> AsChannelReader<T>(this IObservable<T> source, int? bufferCount = null)
        {
            var channel = bufferCount != null ? Channel.CreateBounded<T>(new BoundedChannelOptions(bufferCount.Value)) : Channel.CreateUnbounded<T>();

            var sub = source.Subscribe(
                    onNext: v => channel.Writer.TryWrite(v),
                    onError: e => channel.Writer.TryComplete(e),
                    onCompleted: () => channel.Writer.TryComplete());

            channel.Reader.Completion.ContinueWith(t=> sub.Dispose());

            return channel.Reader;
        }
    }
}
