namespace SpawnDev.MatrixLEDDisplay.ImageTools
{
    public class RGBPixel : Pixel
    {
        public static implicit operator RGBPixel((byte R, byte G, byte B) v) => new RGBPixel(v.R, v.G, v.B);
        public RGBPixel(byte r, byte g, byte b) : base(new byte[] { r, g, b }, 0) { }
        public RGBPixel(byte[] source, int i) : base(source, i) { }
        public byte R { get => _source[_i]; set => _source[_i] = value; }
        public byte G { get => _source[_i + 1]; set => _source[_i + 1] = value; }
        public byte B { get => _source[_i + 2]; set => _source[_i + 2] = value; }
        public string HexColor
        {
            get => $"#{R:X2}{G:X2}{B:X2}";
            set
            {
                if (string.IsNullOrEmpty(value) || !value.StartsWith("#")) return;
                var bytes = Convert.FromHexString(value.Substring(1));
                R = bytes[0];
                G = bytes[1];
                B = bytes[2];
            }
        }
    }
}
