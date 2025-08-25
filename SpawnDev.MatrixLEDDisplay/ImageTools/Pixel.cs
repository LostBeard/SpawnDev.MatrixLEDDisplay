namespace SpawnDev.MatrixLEDDisplay.ImageTools
{
    public abstract class Pixel
    {
        protected byte[] _source;
        protected int _i;
        protected Pixel(byte[] source, int i)
        {
            _source = source;
            _i = i;
        }
    }
}
