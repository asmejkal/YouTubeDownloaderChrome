using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NativeHost
{
    public class SingleFileLocalHttpServer : IDisposable
    {
        private readonly string _fileName;
        private readonly int _port;
        private readonly object _listenerLock = new object();

        private TcpListener _listener;
        private TcpClient _client;

        public SingleFileLocalHttpServer(string fileName, int port = 0)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException(nameof(fileName));

            _fileName = fileName;
            _port = port;
        }

        public Uri StartListening()
        {
            lock (_listenerLock)
            {
                _listener = new TcpListener(IPAddress.Parse("127.0.0.1"), _port);
                _listener.Start();

                var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
                return new Uri($"http://127.0.0.1:{port}");
            }
        }

        public async Task<HttpChunkedStreamWriter> AcceptConnectionAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            lock (_listenerLock)
            {
                if (_listener == null)
                    throw new InvalidOperationException("Server is not listening");
            }

            var connectTask = _listener.AcceptTcpClientAsync();
            var task = await Task.WhenAny(connectTask, Task.Delay(timeout));
            if (task != connectTask)
                throw new TimeoutException("Host failed to connect in time");

            _client = await connectTask;
            var socketStream = _client.GetStream();
            try
            {
                return HttpChunkedStreamWriter.CreateFileStream(socketStream, _fileName);
            }
            catch
            {
                socketStream.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            _client?.Close();
            _listener?.Stop();

            ((IDisposable)_client)?.Dispose();
        }
    }
}
