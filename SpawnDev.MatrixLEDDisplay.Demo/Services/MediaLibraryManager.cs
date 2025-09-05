using Radzen;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.Toolbox;
using SpawnDev.MatrixLEDDisplay.Demo.Pages;
using File = SpawnDev.BlazorJS.JSObjects.File;

namespace SpawnDev.MatrixLEDDisplay.Demo.Services
{
    public class MediaLibraryManager : IAsyncBackgroundService
    {
        Task? _Ready;
        public Task Ready => _Ready ??= InitAsync();
        public event Action OnStateChanged = default!;
        public Cache LibraryCache { get; private set; }
        Window window;
        BlazorJSRuntime JS;
        CallbackGroup callbackGroup = new CallbackGroup();
        NotificationService NotificationService;
        DialogService DialogService;
        public List<MediaLibraryItem> MediaItems { get; private set; } = new List<MediaLibraryItem>();
        HttpClient HttpClient;
        AssetManifestService AssetManifestService;
        public MediaLibraryManager(BlazorJSRuntime js, HttpClient httpClient, NotificationService notificationService, DialogService dialogService, AssetManifestService assetManifestService)
        {
            JS = js;
            HttpClient = httpClient;
            NotificationService = notificationService;
            DialogService = dialogService;
            AssetManifestService = assetManifestService;
            window = JS.Get<Window>("window");
        }
        async Task InitAsync()
        {
            await AssetManifestService.Ready;
            window.AddEventListener("dragover", Callback.Create<DragEvent>(Window_OnDragOver, callbackGroup));
            window.AddEventListener("drop", Callback.Create<DragEvent>(Window_OnDrop, callbackGroup));
            LibraryCache = await Cache.OpenCache("MediaLibrary");
            await UpdateMediaItems();
        }
        void Window_OnDragOver(DragEvent e)
        {
            e.PreventDefault();
        }
        async void Window_OnDrop(DragEvent e)
        {
            e.PreventDefault();
            using var target = e.Target;
            using var dataTransfer = e.DataTransfer;
            using var items = dataTransfer.Items;
            var allItems = items.ToArray();
            var files = new List<File>();
            foreach (var item in allItems)
            {
                if (item.Kind == "file")
                {
                    var file = item.GetAsFile()!;
                    JS.Log($"File: {file!.Name}");
                    if (file.Type.StartsWith("image/"))
                    {
                        files.Add(file);
                    }
                }
            }
            if (!files.Any()) return;
            await AddFilesToLibrary(files);
        }
        public async Task ShowImageSelectDialog()
        {
            File[]? files = null;
            try
            {
                files = await FilePicker.ShowOpenFilePicker("image/", true);
            }
            catch { }
            if (files == null || !files.Any()) return;
            await AddFilesToLibrary(files.ToList());
        }
        public async Task<bool> Remove(string fileName)
        {
            var ret = await LibraryCache.Delete(fileName);
            await UpdateMediaItems();
            return ret;
        }
        async Task UpdateMediaItems()
        {
            var newList = new List<MediaLibraryItem>();
            var included = await GetIncludedPixelArt();
            foreach (var file in included)
            {
                using var resp = await JS.Fetch(file);
                var fileBlob = await resp.Blob();
                var mediaItem = new MediaLibraryItem(file, fileBlob, true);
                newList.Add(mediaItem);
            }
            var files = await LibraryCache.GetAllFiles();
            foreach (var file in files)
            {
                var fileBlob = await LibraryCache.ReadBlob(file);
                if (fileBlob == null) continue;
                var mediaItem = new MediaLibraryItem(file, fileBlob, false);
                newList.Add(mediaItem);
            }
            var previous = MediaItems.ToList();
            foreach (var item in previous)
            {
                item.Dispose();
            }
            MediaItems.Clear();
            MediaItems.AddRange(newList);
            StateHasChanged();
        }
        async Task AddFilesToLibrary(List<File> files)
        {
            var needFeedback = new List<File>();
            foreach (var file in files)
            {
                var fileName = file.Name;
                var hasExt = fileName.Contains(".");
                var ext = hasExt ? fileName.Substring(fileName.LastIndexOf(".")) : "";
                var baseName = hasExt ? fileName.Substring(0, fileName.LastIndexOf(".")) : fileName;
                using var existing = await LibraryCache.ReadBlob(fileName);
                var exists = existing != null;
                if (exists)
                {
                    // TODO - show dialog asking if we should skip, overwrite, or write to a file that does not exist
                    var resolve = await ResolveFileExistsDialog.Show(DialogService, file, existing);
                    if (resolve == ResolveFileExistsDialog.HandleExists.Skip) continue;
                    var keepBoth = resolve == ResolveFileExistsDialog.HandleExists.KeepBoth;
                    if (keepBoth)
                    {
                        var n = 0;
                        while (exists)
                        {
                            fileName = $"{baseName}_{n}{ext}";
                            exists = (await LibraryCache.GetPathType(fileName)) != CacheExtensions.CachePathType.NOT_FOUND;
                        }
                    }
                }
                await LibraryCache.WriteBlob(fileName, file, file.Type);
                JS.Log($"File saved: {fileName}");
            }
            await UpdateMediaItems();
        }
        void StateHasChanged()
        {
            OnStateChanged?.Invoke();
        }
        async Task<List<string>> GetIncludedPixelArt()
        {
            var ret = new List<string>();
            var assetManifest = AssetManifestService.AssetManifest;
            if (assetManifest != null)
            {
                foreach (var asset in assetManifest.Assets)
                {
                    if (asset.Url.StartsWith("pixelart/"))
                    {
                        ret.Add(asset.Url);
                    }
                }
            }
            return ret;
        }
    }
}
