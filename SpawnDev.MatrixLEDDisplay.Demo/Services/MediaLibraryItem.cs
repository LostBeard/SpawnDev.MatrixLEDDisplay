using SpawnDev.BlazorJS.JSObjects;

namespace SpawnDev.MatrixLEDDisplay.Demo.Services
{
    public class MediaLibraryItem : IDisposable
    {
        public string Name { get; set; }
        public string Url { get; private set; }
        public bool ReadOnly { get; private set; }
        public MediaLibraryItem(string name, Blob blob, bool readOnly)
        {
            Name = name;
            Url = URL.CreateObjectURL(blob);
            ReadOnly = readOnly;
        }
        public void Dispose()
        {
            if (Url == null) return;
            URL.RevokeObjectURL(Url);
            Url = null!;
        }
    }
}
