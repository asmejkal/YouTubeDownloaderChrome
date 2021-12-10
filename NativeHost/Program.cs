using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NativeHost.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace NativeHost
{
    class Program
    {
        public enum ResultCode
        {
            Success,
            InvalidCommand,
            InvalidParameters,
            InvalidConfiguration,
            UnspecifiedError
        }

        public class ResultMessage
        {
            [JsonProperty("code")]
            public ResultCode Code { get; }

            [JsonProperty("textCode")]
            public string TextCode => Code.ToString();

            [JsonProperty("data")]
            public JToken Data { get; }

            public ResultMessage(ResultCode code, JToken data = null)
            {
                Code = code;
                Data = data;
            }
        }

        public enum CommandType
        {
            Download,
            DownloadSegment
        }

        public class CommandMessage
        {
            public CommandType CommandType { get; set; }
            public string Url { get; set; }
            public double? FromSeconds { get; set; }
            public double? ToSeconds { get; set; }

            public string YtDlpPath { get; set; }
            public string FfmpegPath { get; set; }
            public string FilenameTemplate { get; set; }
            public string SegmentFilenameTemplate { get; set; }
            public string YtDlpArguments { get; set; }
            public string FfmpegSegmentArguments { get; set; }
            public bool EnableLogs { get; set; }
        }

        public static async Task Main()
        {
            try
            {
                var message = ReadMessage();
                var result = await ProcessMessageAsync(message);
                if (result != null)
                    WriteMessage(result);
            }
            catch (Exception ex)
            {
                WriteMessage(new ResultMessage(ResultCode.UnspecifiedError, ex.ToString()));
            }
        }

        private static IServiceProvider BuildServices(CommandMessage message)
        {
            if (string.IsNullOrEmpty(message.YtDlpPath) || !File.Exists(message.YtDlpPath))
                throw new ArgumentException("Invalid YT-DLP path");
            
            if (string.IsNullOrEmpty(message.FfmpegPath) || !File.Exists(message.FfmpegPath))
                throw new ArgumentException("Invalid FFMPEG path");

            var services = new ServiceCollection();

            
                services.AddLogging(x =>
                {
                    if (message.EnableLogs)
                    {
                        var logger = new LoggerConfiguration()
                            .WriteTo.File(CreateLogFilePath());

                        x.AddSerilog(logger.CreateLogger());
                    }
                    else
                    {
                        x.AddProvider(NullLoggerProvider.Instance);
                    }
                });

            services.Configure<YouTubeDlpOptions>(x =>
            {
                x.Path = message.YtDlpPath;
                x.FileNameTemplate = message.FilenameTemplate;
                x.SegmentFileNameTemplate = message.SegmentFilenameTemplate;
                x.FfmpegPath = message.FfmpegPath;
                x.Arguments = message.YtDlpArguments;
                x.FfmpegSegmentArguments = message.FfmpegSegmentArguments;
            });

            services.AddSingleton<YouTubeDlpService>();

            return services.BuildServiceProvider();
        }

        private static async Task<ResultMessage> ProcessMessageAsync(CommandMessage message)
        {
            IServiceProvider services;
            try
            {
                services = BuildServices(message);
            }
            catch (ArgumentException ex)
            {
                return new ResultMessage(ResultCode.InvalidConfiguration, ex.ToString());
            }

            return await RunCommandAsync(message, services);
        }

        private static async Task<ResultMessage> RunCommandAsync(CommandMessage message, IServiceProvider services)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Message received: {ReceivedMessage}", JsonConvert.SerializeObject(message, Formatting.Indented));

                var response = message.CommandType switch
                {
                    CommandType.Download => await DownloadAsync(message, services),
                    CommandType.DownloadSegment => await DownloadSegmentAsync(message, services),
                    _ => new ResultMessage(ResultCode.InvalidCommand)
                };

                logger.LogInformation("Result: {ResultMessage}", response != null ? JsonConvert.SerializeObject(response) : "none");
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process message");
                throw;
            }
        }

        private static async Task<ResultMessage> DownloadAsync(CommandMessage message, IServiceProvider services)
        {
            if (!Uri.TryCreate(message.Url, UriKind.Absolute, out var url))
                return new ResultMessage(ResultCode.InvalidParameters);

            var service = services.GetRequiredService<YouTubeDlpService>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            var metadata = await service.GetMetadataAsync(url);

            return await EstablishFileStreamConnectionAsync(metadata.Name, logger, async writer =>
            {
                await service.StreamVideoAsync(url, writer);
            });
        }

        private static async Task<ResultMessage> DownloadSegmentAsync(CommandMessage message, IServiceProvider services)
        {
            if (!message.FromSeconds.HasValue || !message.ToSeconds.HasValue)
                return new ResultMessage(ResultCode.InvalidParameters);

            var from = TimeSpan.FromSeconds(message.FromSeconds.Value);
            var to = TimeSpan.FromSeconds(message.ToSeconds.Value);

            if (!Uri.TryCreate(message.Url, UriKind.Absolute, out var url))
                return new ResultMessage(ResultCode.InvalidParameters);

            var service = services.GetRequiredService<YouTubeDlpService>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            var metadata = await service.GetSegmentMetadataAsync(url, from, to);

            return await EstablishFileStreamConnectionAsync(metadata.Name, logger, async writer =>
            {
                await service.StreamVideoSegmentAsync(metadata, from, to, writer);
            });
        }

        private static async Task<ResultMessage> EstablishFileStreamConnectionAsync(
            string fileName,
            ILogger<Program> logger,
            Func<HttpChunkedStreamWriter, Task> connectionHandler)
        {
            using var server = new SingleFileLocalHttpServer(fileName);
            var fileUrl = server.StartListening();

            var uploadTask = Task.Run(async () =>
            {
                using var writer = await server.AcceptConnectionAsync(TimeSpan.FromSeconds(10));
                await connectionHandler(writer);
            });

            var response = new ResultMessage(ResultCode.Success, JToken.FromObject(new
            {
                url = fileUrl.AbsoluteUri
            }));

            logger.LogInformation("Sending connection information: {ResultMessage}", JsonConvert.SerializeObject(response));
            WriteMessage(response);

            await uploadTask;
            logger.LogInformation("Upload task finished");
            return null;
        }

        private static string CreateLogFilePath()
        {
            var logFolder = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs");
            Directory.CreateDirectory(logFolder);

            return Path.Join(logFolder, DateTimeOffset.UtcNow.ToString("yyyy-MM-dd--HH-mm-ss-fffff") + ".txt");
        }

        private static CommandMessage ReadMessage()
        {
            using var reader = new BinaryReader(Console.OpenStandardInput());
            var length = reader.ReadInt32();
            var bytes = reader.ReadBytes(length);
            var json = Encoding.UTF8.GetString(bytes);

            return JsonConvert.DeserializeObject<CommandMessage>(json);
        }

        private static void WriteMessage(ResultMessage message)
        {
            var json = JsonConvert.SerializeObject(message, Formatting.None);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var writer = new BinaryWriter(Console.OpenStandardOutput());
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }
    }
}
