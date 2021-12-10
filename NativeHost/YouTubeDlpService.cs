using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NativeHost.Configuration;

namespace NativeHost
{
    public class YouTubeDlpService
    {
        public const string DefaultFileNameTemplate = "%(title)s-%(id)s.%(ext)s";
        public const string DefaultSegmentFileNameTemplate = "%(title)s-%(id)s-%(from)s-%(to)s.mp4";
        public const string DefaultArguments = "-f bestvideo+bestaudio";
        public const string DefaultFfmpegSegmentArguments = "-c:v libx264 -crf 17 -f mp4 -movflags empty_moov";
        public const string FFmpegTimeFormat = "hh\\:mm\\:ss\\.fff";
        public const string FilenameSuffixFormat = "hh\\hmm\\mss\\.fff\\s";
        private readonly YouTubeDlpOptions _options;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<YouTubeDlpService> _logger;

        public YouTubeDlpService(IOptions<YouTubeDlpOptions> options, ILoggerFactory loggerFactory, ILogger<YouTubeDlpService> logger)
        {
            _options = options.Value;
            _loggerFactory = loggerFactory;
            _logger = logger;

            if (string.IsNullOrEmpty(_options.FileNameTemplate))
                _options.FileNameTemplate = DefaultFileNameTemplate;

            if (string.IsNullOrEmpty(_options.SegmentFileNameTemplate))
                _options.SegmentFileNameTemplate = DefaultSegmentFileNameTemplate;

            if (string.IsNullOrEmpty(_options.Arguments))
                _options.Arguments = DefaultArguments;

            if (string.IsNullOrEmpty(_options.FfmpegSegmentArguments))
                _options.FfmpegSegmentArguments = DefaultFfmpegSegmentArguments;
        }

        public async Task<VideoMetadata> GetMetadataAsync(Uri url, CancellationToken cancellationToken = default)
        {
            var results = await GetMetadataInnerAsync(url, $"--get-filename -o \"{_options.FileNameTemplate}\"", cancellationToken);
            return new VideoMetadata(results.First());
        }

        public async Task<VideoSegmentMetadata> GetSegmentMetadataAsync(Uri url, TimeSpan from, TimeSpan to, CancellationToken cancellationToken = default)
        {
            var fileNameTemplate = _options.SegmentFileNameTemplate
                .Replace("%(from)s", from.ToString(FilenameSuffixFormat))
                .Replace("%(to)s", to.ToString(FilenameSuffixFormat));

            var results = await GetMetadataInnerAsync(url, $"{_options.Arguments} --get-url --get-filename -o \"{fileNameTemplate}\"", cancellationToken);
            var streams = results.SkipLast(1);
            if (streams.Count() < 2 || streams.Any(x => !Uri.TryCreate(x, UriKind.Absolute, out var uri)))
                throw new InvalidOperationException($"Received invalid video and/or audio stream metadata {string.Join("\n", streams)}");

            return new VideoSegmentMetadata(results.Last(), streams.First(), streams.Skip(1).FirstOrDefault());
        }

        public Task StreamVideoAsync(Uri url, HttpChunkedStreamWriter streamWriter, CancellationToken cancellationToken = default)
        {
            return StreamInnerAsync(_options.Path, $"{_options.Arguments} -o - \"{url.AbsoluteUri}\"", streamWriter, cancellationToken);
        }

        public Task StreamVideoSegmentAsync(VideoSegmentMetadata metadata, TimeSpan from, TimeSpan to, HttpChunkedStreamWriter streamWriter, CancellationToken cancellationToken = default)
        {
            var timeArgs = $"-ss {from.ToString(FFmpegTimeFormat)} -to {to.ToString(FFmpegTimeFormat)}";
            var videoArgs = $"{timeArgs} -i \"{metadata.VideoStreamUrl}\"";
            var audioArgs = !string.IsNullOrEmpty(metadata.AudioStreamUrl) ? $"{timeArgs} -i \"{metadata.AudioStreamUrl}\"" : "";

            var finalArgs = $"{timeArgs} {videoArgs} {audioArgs} {_options.FfmpegSegmentArguments} -";
            return StreamInnerAsync(_options.FfmpegPath, finalArgs, streamWriter, cancellationToken);
        }

        private async Task<List<string>> GetMetadataInnerAsync(Uri url, string args, CancellationToken cancellationToken = default)
        {
            var result = new StringBuilder();
            var process = new ProcessPipeListener(_options.Path, _loggerFactory.CreateLogger<ProcessPipeListener>());
            process.StdoutWritten += (_, x) =>
            {
                var chunk = Encoding.Unicode.GetString(x.Span);
                result.Append(chunk);
                return Task.CompletedTask;
            };

            process.StderrWritten += (_, x) => _logger.LogInformation(x);

            await process.RunAsync($"{args} --encoding utf-16 \"{url.AbsoluteUri}\"", 1024, cancellationToken);

            var output = result.ToString();
            return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Trim().TrimStart('\uFEFF'))
                .ToList();
        }

        private async Task StreamInnerAsync(string processPath, string args, HttpChunkedStreamWriter streamWriter, CancellationToken cancellationToken = default)
        {
            var process = new ProcessPipeListener(processPath, _loggerFactory.CreateLogger<ProcessPipeListener>());

            try
            {
                process.StdoutWritten += (_, x) => streamWriter.WriteChunkAsync(x);
                process.StderrWritten += (_, x) => _logger.LogInformation(x);

                await process.RunAsync(args, cancellationToken: cancellationToken);
            }
            finally
            {
                try
                {
                    await streamWriter.FinalizeAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to finalize chunked stream");
                }
            }
        }
    }
}
