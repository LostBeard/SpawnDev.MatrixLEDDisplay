namespace SpawnDev.MatrixLEDDisplay.ImageTools
{
    public sealed class RGBAImage : MemImage
    {
        public RGBAPixel this[int pixelIndex]
        {
            get => new RGBAPixel(Bytes, GetByteIndex(pixelIndex));
            set => Set(pixelIndex, value.R, value.G, value.B, value.A);
        }
        public RGBAPixel this[int x, int y]
        {
            get => new RGBAPixel(Bytes, GetByteIndex(x, y));
            set => Set(x, y, value.R, value.G, value.B, value.A);
        }
        public RGBAPixel Get(int x, int y) => new RGBAPixel(Bytes, GetByteIndex(x, y));
        public void Set(int x, int y, byte r, byte g, byte b, byte a)
        {
            var i = GetByteIndex(x, y);
            Bytes[i] = r;
            Bytes[i + 1] = g;
            Bytes[i + 2] = b;
            Bytes[i + 3] = a;
        }
        public void Set(int pixelIndex, byte r, byte g, byte b, byte a)
        {
            var i = pixelIndex * ElementSize;
            Bytes[i] = r;
            Bytes[i + 1] = g;
            Bytes[i + 2] = b;
            Bytes[i + 3] = a;
        }
        public RGBAImage(int width, int height, byte[] bytes) : base(width, height, 4, bytes) { }
        public RGBAImage(int width, int height) : base(width, height, 4) { }
        public RGBImage ToRGBImage(RGBPixel backgroundColor)
        {
            var ret = new RGBImage(Width, Height);
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var srcPixel = this[x, y];
                    var r = srcPixel.R;
                    var g = srcPixel.G;
                    var b = srcPixel.B;
                    var a = srcPixel.A;
                    if (a < 255)
                    {
                        var an = a / 255d;
                        r = (byte)double.Lerp(backgroundColor.R, r, an);
                        g = (byte)double.Lerp(backgroundColor.G, g, an);
                        b = (byte)double.Lerp(backgroundColor.B, b, an);
                    }
                    ret.Set(x, y, r, g, b);
                }
            }
            return ret;
        }
    }
}
