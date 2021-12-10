using System;

namespace NativeHost
{
    public class VideoMetadata
    {
        public string Name { get; }

        public VideoMetadata(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
