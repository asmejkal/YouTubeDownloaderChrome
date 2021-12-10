using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NativeHost
{
    public class ProcessPipeListener
    {
        public const int DefaultBufferSize = 1024 * 1024;

        private readonly string _path;
        private readonly ILogger<ProcessPipeListener> _logger;

        public AsyncEvent<ReadOnlyMemory<byte>> StdoutWritten = new();
        public event EventHandler<string> StderrWritten;

        public ProcessPipeListener(string path, ILogger<ProcessPipeListener> logger)
        {
            _path = path;
            _logger = logger;
        }

        public async Task RunAsync(string args, int bufferSize = DefaultBufferSize, CancellationToken cancellationToken = default)
        {
            var psi = new ProcessStartInfo(_path, args)
            {
                RedirectStandardOutput = true
            };

            if (StderrWritten != null)
                psi.RedirectStandardError = true;

            var cts = new CancellationTokenSource();
            using var process = Process.Start(psi);
            cancellationToken.Register(() => cts.Cancel());

            _logger.LogInformation("Running process {ProcessId} from {ProcessRunPath} with args {ProcessRunArgs}", process.Id, _path, args);

            if (StdoutWritten == null)
                throw new InvalidOperationException("There must be at least one consumer of the stdout event");

            var stdoutTask = Task.Run(async () =>
            {
                try
                {
                    var buffer = new Memory<byte>(new byte[bufferSize]);
                    int bytesRead;
                    while ((bytesRead = await process.StandardOutput.BaseStream.ReadAsync(buffer, cts.Token)) > 0)
                    {
                        await StdoutWritten.InvokeAsync(this, buffer.Slice(0, bytesRead));
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Stdout reader for process {ProcessId} canceled", process.Id);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stdout reader for process {ProcessId} errored", process.Id);
                    throw;
                }

                _logger.LogInformation("Stdout reader for process {ProcessId} finished", process.Id);
            });

            var stderrTask = Task.Run(async () =>
            {
                if (StderrWritten == null)
                    return;

                try
                {
                    while (!process.StandardError.EndOfStream)
                    {
                        var line = await process.StandardError.ReadLineAsync().WaitAsync(cts.Token);
                        if (line == null)
                            break;
                        
                        StderrWritten.Invoke(this, line);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Stderr reader for process {ProcessId} canceled", process.Id);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stderr reader for process {ProcessId} errored", process.Id);
                    throw;
                }

                _logger.LogInformation("Stderr reader for process {ProcessId} finished", process.Id);
            });

            try
            {
                await stdoutTask;
            }
            finally
            {
                cts.Cancel();
            }

            try
            {
                await stderrTask;
            }
            catch (OperationCanceledException)
            {
                // Canceled
            }
        }
    }
}
