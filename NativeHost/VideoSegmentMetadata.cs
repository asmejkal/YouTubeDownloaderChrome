using System;

namespace NativeHost
{
    public class VideoSegmentMetadata : VideoMetadata
    {
        public string VideoStreamUrl { get; }
        public string AudioStreamUrl { get; }

        public VideoSegmentMetadata(string name, string videoStreamUrl, string audioStreamUrl = null)
            : base(name)
        {
            VideoStreamUrl = videoStreamUrl ?? throw new ArgumentNullException(nameof(videoStreamUrl));
            AudioStreamUrl = audioStreamUrl;
        }
    }
}
