namespace SpawnDev.MatrixLEDDisplay.ImageTools
{
    public abstract class MemImage
    {

        public int Width { get; }

        public int Height { get; }

        public int Stride { get; }

        public byte[] Bytes { get; }

        public int ElementSize { get; }

        public int ByteLength { get; }

        public int Length { get; }

        protected MemImage(int width, int height, int elementSize, byte[]? bytes = null)
        {
            Width = width;
            Height = height;
            ElementSize = elementSize;
            Stride = Width * ElementSize;
            Length = width * height;
            var expectedByteLength = Stride * Height;
            if (bytes == null)
            {
                Bytes = new byte[expectedByteLength];
            }
            else
            {
                if (bytes.Length != expectedByteLength)
                {
                    throw new ArgumentException($"Byte array size does not match image dimensions and pixel size {Width}x{Height}");
                }
                Bytes = bytes;
            }
            ByteLength = Bytes.Length;
        }
        public int GetByteIndex(int x, int y) => y * Stride + x * ElementSize;
        public int GetByteIndex(int elementIndex) => GetByteIndex(elementIndex % Width, (int)Math.Floor(elementIndex / (double)Width));
        public void ForEachXY(Action<int, int> callbackXY)
        {
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    callbackXY(x, y);
                }
            }
        }
    }
}
