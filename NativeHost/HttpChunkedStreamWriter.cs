using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NativeHost
{
    public class HttpChunkedStreamWriter : IDisposable
    {
        private readonly NetworkStream _stream;
        private readonly IReadOnlyDictionary<string, string> _headers;
        private bool _initalized;
        private bool _finalized;

        public HttpChunkedStreamWriter(NetworkStream stream, IReadOnlyDictionary<string, string> headers)
        {
            _stream = stream;

            _headers = new Dictionary<string, string>(headers)
            {
                ["Transfer-Encoding"] = "chunked",
                ["Connection"] = "close",
            };
        }

        public async Task WriteChunkAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_initalized)
            {
                var serializedHeaders = string.Join("\r\n", _headers.Select(x => $"{x.Key}: {x.Value}"));
                await _stream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\n{serializedHeaders}\r\n\r\n"), cancellationToken);
                _initalized = true;
            }

            await _stream.WriteAsync(Encoding.ASCII.GetBytes($"{buffer.Length:X}\r\n"), cancellationToken);
            await _stream.WriteAsync(buffer, cancellationToken);
            await _stream.WriteAsync(Encoding.ASCII.GetBytes("\r\n"), cancellationToken);
        }

        public async Task FinalizeAsync(CancellationToken cancellationToken = default)
        {
            if (!_finalized)
            {
                await _stream.WriteAsync(Encoding.ASCII.GetBytes($"0\r\n\r\n"), cancellationToken);

                _stream.Socket.Shutdown(SocketShutdown.Send);
                var buffer = new byte[1024];
                while ((await _stream.Socket.ReceiveAsync(buffer, SocketFlags.None)) > 0)
                {
                }

                _finalized = true;
            }
        }

        public void Dispose()
        {
            ((IDisposable)_stream).Dispose();
        }

        public static HttpChunkedStreamWriter CreateFileStream(NetworkStream stream, string fileName)
        {
            return new HttpChunkedStreamWriter(stream, new Dictionary<string, string>()
            {
                ["Content-Type"] = "application/binary-octet",
                ["Content-Disposition"] = $"attachment; filename*=UTF-8''{Uri.EscapeDataString(fileName)}"
            });
        }
    }
}
