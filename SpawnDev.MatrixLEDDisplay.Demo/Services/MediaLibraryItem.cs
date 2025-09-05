using SpawnDev.BlazorJS.JSObjects;

namespace SpawnDev.MatrixLEDDisplay.Demo.Services
{
    public class MediaLibraryItem : IDisposable
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public MediaLibraryItem(string name, Blob blob)
        {
            Name = name;
            Url = URL.CreateObjectURL(blob);
        }
        public void Dispose()
        {
            if (Url == null) return;
            URL.RevokeObjectURL(Url);
            Url = null!;
        }
    }
}
