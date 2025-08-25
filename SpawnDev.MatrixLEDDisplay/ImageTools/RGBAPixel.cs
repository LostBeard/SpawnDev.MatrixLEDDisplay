using SpawnDev.BlazorJS.JSObjects;

namespace SpawnDev.MatrixLEDDisplay.ImageTools
{
    public class RGBAPixel : Pixel
    {
        public static implicit operator RGBAPixel((byte R, byte G, byte B) v) => new RGBAPixel(v.R, v.G, v.B);
        public RGBAPixel(byte r, byte g, byte b, byte a = 255) : base(new byte[] { r, g, b, a }, 0) { }
        public RGBAPixel(byte[] source, int i) : base(source, i) { }
        public byte R { get => _source[_i]; set => _source[_i] = value; }
        public byte G { get => _source[_i + 1]; set => _source[_i + 1] = value; }
        public byte B { get => _source[_i + 2]; set => _source[_i + 2] = value; }
        public byte A { get => _source[_i + 3]; set => _source[_i + 3] = value; }
        public string HexColor
        {
            get => $"#{R:X2}{G:X2}{B:X2}{A:X2}";
            set
            {
                if (string.IsNullOrEmpty(value) || !value.StartsWith("#")) return;
                var bytes = Convert.FromHexString(value.Substring(1));
                R = bytes[0];
                G = bytes[1];
                B = bytes[2];
                A = bytes.Length > 3 ? bytes[3] : (byte)255;
            }
        }
    }
}
