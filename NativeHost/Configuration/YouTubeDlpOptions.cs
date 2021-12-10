namespace NativeHost.Configuration
{
    public class YouTubeDlpOptions
    {
        public string Path { get; set; }
        public string FfmpegPath { get; set; }
        public string Arguments { get; set; }
        public string FfmpegSegmentArguments { get; set; }
        public string FileNameTemplate { get; set; }
        public string SegmentFileNameTemplate { get; set; }
    }
}
