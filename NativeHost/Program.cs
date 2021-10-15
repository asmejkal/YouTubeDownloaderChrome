using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NativeHost
{
    class Program
    {
        public enum ResultCode
        {
            Success,
            InvalidCommand,
            InvalidUri,
            UnsetDownloadFolder,
            UnspecifiedError
        }

        public class ResultMessage
        {
            [JsonProperty("code")]
            public ResultCode Code { get; }

            [JsonProperty("data")]
            public JToken Data { get; }

            public ResultMessage(ResultCode code, JToken data = null)
            {
                Code = code;
                Data = data;
            }
        }

        private const string FFmpegTimeFormat = "hh\\:mm\\:ss\\.fff";
        private const string FilenameSuffixFormat = "hh\\hmm\\mss\\.fff\\s";

        public static void Main()
        {
            try
            {
                var message = ReadMessage();
                var result = ProcessMessage(message);
                WriteMessage(result);
            }
            catch (Exception ex)
            {
                WriteMessage(new ResultMessage(ResultCode.UnspecifiedError, ex.ToString()));
            }
        }

        private static ResultMessage ProcessMessage(JObject data)
        {
            if (!Uri.TryCreate((string)data["url"], UriKind.Absolute, out var url))
                return new ResultMessage(ResultCode.InvalidUri);

            var downloadFolder = Directory.CreateDirectory((string)data["downloadFolder"]);

            return (string)data["cmd"] switch
            {
                "download" => Download(url, downloadFolder),
                "downloadSegment" => DownloadSegment(data, url, downloadFolder),
                _ => new ResultMessage(ResultCode.InvalidCommand)
            };
        }

        private static ResultMessage Download(Uri url, DirectoryInfo downloadFolder)
        {
            var output = Path.Join(downloadFolder.FullName, "%(title)s-%(id)s.%(ext)s")
                .Replace("\\", "\\\\");

            var command = $"youtube-dl -f bestvideo+bestaudio --no-mtime -o '{output}' '{url}'";

            var process = Process.Start("powershell.exe", $"-NoExit -Command {command}");
            process.WaitForExit();
            return new ResultMessage(ResultCode.Success);
        }

        private static ResultMessage DownloadSegment(JObject data, Uri url, DirectoryInfo downloadFolder)
        {
            var from = TimeSpan.FromSeconds((double)data["from"]);
            var to = TimeSpan.FromSeconds((double)data["to"]);

            var fileNameSuffix = from.ToString(FilenameSuffixFormat) + "-" + to.ToString(FilenameSuffixFormat);
            var output = Path.Join(downloadFolder.FullName, $"%(title)s-%(id)s-{fileNameSuffix}.%(ext)s")
                .Replace("\\", "\\\\");

            var timeParams = $"-ss {from.ToString(FFmpegTimeFormat)} -to {to.ToString(FFmpegTimeFormat)}";
            var videoInput = $"\"$(youtube-dl -f bestvideo --get-url '{url}')\"";
            var audioInput = $"\"$(youtube-dl -f bestaudio --get-url '{url}')\"";
            var fileName = $"\"$(youtube-dl --get-filename -o '{output}' '{url}')\"";
            
            var command = $"ffmpeg {timeParams} -i {videoInput} {timeParams} -i {audioInput} {fileName}";

            var process = Process.Start("powershell.exe", $"-NoExit -Command {command}");
            process.WaitForExit();
            return new ResultMessage(ResultCode.Success);
        }

        private static JObject ReadMessage()
        {
            using var reader = new BinaryReader(Console.OpenStandardInput());
            var length = reader.ReadInt32();
            var bytes = reader.ReadBytes(length);
            var json = Encoding.UTF8.GetString(bytes);

            return JObject.Parse(json);
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
