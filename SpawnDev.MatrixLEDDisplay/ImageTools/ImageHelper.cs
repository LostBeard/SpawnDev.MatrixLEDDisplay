using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SpawnDev.MatrixLEDDisplay.ImageTools
{
    /// <summary>
    /// Tools for working with Gifs
    /// </summary>
    public static class ImageHelper
    {
        /// <summary>
        /// Returns image frames
        /// </summary>
        /// <param name="imageBytes"></param>
        /// <returns></returns>
        public static Task<List<RGBAImage>> GetImageFrames(byte[] imageBytes) => GetImageFrames(imageBytes, CancellationToken.None);
        /// <summary>
        /// Returns image frames
        /// </summary>
        /// <param name="imageBytes"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<List<RGBAImage>> GetImageFrames(byte[] imageBytes, CancellationToken cancellationToken)
        {
            var ret = new List<RGBAImage>();
            using var memStream = new MemoryStream(imageBytes);
            using var image = await Image.LoadAsync(memStream, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (image.PixelType.BitsPerPixel == 24)
            {
                var frames = image.Frames.Cast<ImageFrame<Rgb24>>();
                foreach (var frame in frames)
                {
                    var bytes = GetRgbaBytesFromFrame(frame);
                    ret.Add(new RGBAImage(image.Width, image.Height, bytes));
                }
            }
            else if (image.PixelType.BitsPerPixel == 32)
            {
                var frames = image.Frames.Cast<ImageFrame<Rgba32>>();
                foreach (var frame in frames)
                {
                    var bytes = GetRgbaBytesFromFrame(frame);
                    ret.Add(new RGBAImage(image.Width, image.Height, bytes));
                }
            }
            return ret;
        }
        /// <summary>
        /// Returns image frames
        /// </summary>
        /// <param name="imageBytes"></param>
        /// <param name="outWidth"></param>
        /// <param name="outHeight"></param>
        /// <returns></returns>
        public static Task<List<RGBAImage>> GetImageFrames(byte[] imageBytes, int outWidth, int outHeight) => GetImageFrames(imageBytes, outWidth, outHeight, CancellationToken.None);
        /// <summary>
        /// Returns image frames
        /// </summary>
        /// <param name="imageBytes"></param>
        /// <param name="outWidth"></param>
        /// <param name="outHeight"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<List<RGBAImage>> GetImageFrames(byte[] imageBytes, int outWidth, int outHeight, CancellationToken cancellationToken)
        {
            var ret = new List<RGBAImage>();
            using var memStream = new MemoryStream(imageBytes);
            using var image = await Image.LoadAsync(memStream, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (image.Width != outWidth || image.Height != outHeight)
            {
                image.Mutate(o => o.Resize(outWidth, outHeight));
            }
            if (image.PixelType.BitsPerPixel == 24)
            {
                var frames = image.Frames.Cast<ImageFrame<Rgb24>>();
                foreach (var frame in frames)
                {
                    var bytes = GetRgbaBytesFromFrame(frame);
                    ret.Add(new RGBAImage(outWidth, outHeight, bytes));
                }
            }
            else if (image.PixelType.BitsPerPixel == 32)
            {
                var frames = image.Frames.Cast<ImageFrame<Rgba32>>();
                foreach (var frame in frames)
                {
                    var bytes = GetRgbaBytesFromFrame(frame);
                    ret.Add(new RGBAImage(outWidth, outHeight, bytes));
                }
            }
            return ret;
        }
        /// <summary>
        /// Returns RGBA image frame as an RGBA byte array
        /// </summary>
        /// <param name="imageFrame"></param>
        /// <returns></returns>
        public static byte[] GetRgbaBytesFromFrame(ImageFrame<Rgba32> imageFrame)
        {
            var size = imageFrame.Width * imageFrame.Height * 4;
            var bytes = new byte[size];
            imageFrame.CopyPixelDataTo(bytes);
            return bytes;
        }
        /// <summary>
        /// Returns RGB image frame as an RGBA byte array
        /// </summary>
        /// <param name="imageFrame"></param>
        /// <returns></returns>
        public static byte[] GetRgbaBytesFromFrame(ImageFrame<Rgb24> imageFrame)
        {
            var size = imageFrame.Width * imageFrame.Height * 3;
            var bytes = new byte[size];
            imageFrame.CopyPixelDataTo(bytes);
            // convert to rgba
            var sizeRgba = imageFrame.Width * imageFrame.Height * 4;
            var retBytes = new byte[sizeRgba];
            for (var y = 0; y < imageFrame.Height; y++)
            {
                for (var x = 0; x < imageFrame.Width; x++)
                {
                    var p = (y * imageFrame.Width + x);
                    var iRgb = p * 3;
                    var iRgba = p * 4;
                    retBytes[iRgba] = bytes[iRgb];
                    retBytes[iRgba + 1] = bytes[iRgb + 1];
                    retBytes[iRgba + 2] = bytes[iRgb + 2];
                    retBytes[iRgba + 3] = 255;
                }
            }
            return retBytes;
        }
    }
}
