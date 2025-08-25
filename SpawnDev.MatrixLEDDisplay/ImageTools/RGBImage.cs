namespace SpawnDev.MatrixLEDDisplay.ImageTools
{
    public sealed class RGBImage : MemImage
    {
        public RGBPixel this[int pixelIndex]
        {
            get => new RGBPixel(Bytes, GetByteIndex(pixelIndex));
            set => Set(pixelIndex, value.R, value.G, value.B);
        }
        public RGBPixel this[int x, int y]
        {
            get => new RGBPixel(Bytes, GetByteIndex(x, y));
            set => Set(x, y, value.R, value.G, value.B);
        }
        public RGBPixel Get(int x, int y) => new RGBPixel(Bytes, GetByteIndex(x, y));
        public void Set(int x, int y, byte r, byte g, byte b)
        {
            var i = GetByteIndex(x, y);
            Bytes[i] = r;
            Bytes[i + 1] = g;
            Bytes[i + 2] = b;
        }
        public void Set(int pixelIndex, byte r, byte g, byte b)
        {
            var i = pixelIndex * ElementSize;
            Bytes[i] = r;
            Bytes[i + 1] = g;
            Bytes[i + 2] = b;
        }
        public RGBImage(int width, int height, byte[] bytes) : base(width, height, 3, bytes) { }
        public RGBImage(int width, int height) : base(width, height, 3) { }
    }
}
