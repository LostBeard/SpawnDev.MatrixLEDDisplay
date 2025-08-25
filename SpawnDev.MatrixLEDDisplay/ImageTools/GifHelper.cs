using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SpawnDev.MatrixLEDDisplay.ImageTools
{
    /// <summary>
    /// Tools for working with Gifs
    /// </summary>
    public static class GifHelper
    {
        // https://khalidabuhakmeh.com/gifs-in-console-output-imagesharp-and-spectreconsole
        public static Task<List<RGBAImage>> GetGIFFrames(byte[] gifBytes, int resizeWidth, int resizeHeight) => GetGIFFrames(gifBytes, resizeWidth, resizeHeight, CancellationToken.None);

        public static Task<List<RGBAImage>> GetGIFFrames(byte[] gifBytes) => GetGIFFrames(gifBytes, CancellationToken.None);
        public static async Task<List<RGBAImage>> GetGIFFrames(byte[] gifBytes, CancellationToken cancellationToken)
        {
            var ret = new List<RGBAImage>();
            using var gif = await GifDecoder.Instance.DecodeAsync(new SixLabors.ImageSharp.Formats.DecoderOptions { }, new MemoryStream(gifBytes), cancellationToken);
            var metadata = gif.Frames.RootFrame.Metadata.GetGifMetadata();
            var frames = gif.Frames.Cast<ImageFrame<Rgba32>>();
            foreach (var frame in frames)
            {
                var bytes = await GetBytesFromFrameAsync(frame);
                ret.Add(new RGBAImage(frame.Width, frame.Height, bytes));
            }
            return ret;
        }
        static async Task<byte[]> GetBytesFromFrameAsync(ImageFrame<Rgba32> imageFrame)
        {
            var size = imageFrame.Width * imageFrame.Height * 4;
            var bytes = new byte[size];
            imageFrame.CopyPixelDataTo(bytes);
            return bytes;
        }
        public static async Task<List<RGBAImage>> GetGIFFrames(byte[] gifBytes, int width, int height, CancellationToken cancellationToken)
        {
            var ret = new List<RGBAImage>();
            using var gif = await GifDecoder.Instance.DecodeAsync(new SixLabors.ImageSharp.Formats.DecoderOptions { }, new MemoryStream(gifBytes), cancellationToken);
            var metadata = gif.Frames.RootFrame.Metadata.GetGifMetadata();
            if (gif.Width != width || gif.Height != height)
            {
                gif.Mutate(o => o.Resize(width, height));
            }
            var frames = gif.Frames.Cast<ImageFrame<Rgba32>>();
            foreach (var frame in frames)
            {
                var bytes = await GetBytesFromFrameAsync(frame);
                ret.Add(new RGBAImage(width, height, bytes));
            }
            return ret;
        }
    }
}
