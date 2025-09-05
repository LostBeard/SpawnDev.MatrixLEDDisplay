using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.Toolbox;

namespace SpawnDev.MatrixLEDDisplay
{
    public class MatrixLEDDisplayService : IAsyncBackgroundService
    {
        /// <inheritdoc/>
        public Task Ready => _Ready ??= InitASync();
        Task? _Ready = null;
        BlazorJSRuntime JS;
        Cache? DeviceStore;
        /// <summary>
        /// Displays
        /// </summary>
        public Dictionary<string, MIMatrixDisplay> Displays { get; } = new Dictionary<string, MIMatrixDisplay>();
        /// <summary>
        /// Contains display metadata  such as a user specified named that defaults to "Display #"
        /// </summary>
        public Dictionary<string, DisplayMetaData> DisplayMetaData { get; } = new Dictionary<string, DisplayMetaData>();
        /// <summary>
        /// Fired after a display has been added
        /// </summary>
        public event Action<MIMatrixDisplay> OnDisplayAdded = default!;
        /// <summary>
        /// Fired after a display has been removed
        /// </summary>
        public event Action<MIMatrixDisplay> OnDisplayRemoved = default!;
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="js"></param>
        public MatrixLEDDisplayService(BlazorJSRuntime js)
        {
            JS = js;
        }
        /// <summary>
        /// Get the display metadata
        /// </summary>
        /// <param name="display"></param>
        /// <returns></returns>
        public DisplayMetaData? GetMetaData(MIMatrixDisplay display)
        {
            return display == null ? null : DisplayMetaData.TryGetValue(display.DeviceId!, out var metadata) ? metadata : null;
        }
        async Task InitASync()
        {
            DeviceStore = await Cache.OpenCache("MatrixLEDDisplays");
            await UpdateDisplayMetaData();
        }
        /// <summary>
        /// Updates the cached metadata
        /// </summary>
        /// <returns></returns>
        public async Task<List<DisplayMetaData>> UpdateDisplayMetaData()
        {
            var existingKeys = DisplayMetaData.Keys.ToList();
            var metadata = await GetAllDisplayMetadata();
            var newKeys = metadata.Select(o => o.DisplayId).ToList();
            foreach (var md in metadata)
            {
                DisplayMetaData[md.DisplayId] = md;
            }
            var removed = existingKeys.Except(newKeys).ToList();
            foreach (var r in removed)
            {
                DisplayMetaData.Remove(r);
            }
            return DisplayMetaData.Select(o => o.Value).ToList();
        }
        async Task<List<DisplayMetaData>> GetAllDisplayMetadata()
        {
            var fileNames = await DeviceStore!.GetFiles();
            var metaDataFileNames = fileNames.Where(o => o.EndsWith(".display.json"));
            var ret = new List<DisplayMetaData>();
            foreach (var fileName in metaDataFileNames)
            {
                var metadata = await GetDisplayMetadata(fileName);
                if (metadata != null)
                {
                    ret.Add(metadata);
                }
            }
            return ret;
        }
        public async Task SetDisplayMetadata(DisplayMetaData displayMetaData)
        {
            if (displayMetaData == null || string.IsNullOrEmpty(displayMetaData.Name) || string.IsNullOrEmpty(displayMetaData.DisplayId))
            {
                throw new InvalidDataException(nameof(displayMetaData));
            }
            await DeviceStore!.WriteJSON($"{displayMetaData.DisplayId}.display.json", displayMetaData);
            DisplayMetaData[displayMetaData.DisplayId] = displayMetaData;
        }
        public async Task<bool> RemoveDisplayMetadata(DisplayMetaData displayMetaData)
        {
            if (displayMetaData == null || string.IsNullOrEmpty(displayMetaData.Name) || string.IsNullOrEmpty(displayMetaData.DisplayId))
            {
                throw new InvalidDataException(nameof(displayMetaData));
            }
            var ret = await DeviceStore!.Delete($"{displayMetaData.DisplayId}.display.json");
            DisplayMetaData.Remove(displayMetaData.DisplayId);
            return ret;
        }
        public async Task<DisplayMetaData?> GetDisplayMetadata(string displayId, bool allowCreate = false)
        {
            if (string.IsNullOrEmpty(displayId))
            {
                throw new InvalidDataException(nameof(displayId));
            }
            var ret = await DeviceStore!.ReadJSON<DisplayMetaData>($"{displayId}.display.json");
            if (ret == null && allowCreate)
            {
                var allDisplays = await GetAllDisplayMetadata();
                var allDisplayNames = allDisplays.Select(o => o.Name).ToList();
                var newDisplayIndex = 1;
                var newDisplayName = $"Display {newDisplayIndex}";
                while (allDisplayNames.Contains(newDisplayName))
                    newDisplayName = $"Display {++newDisplayIndex}";
                ret = new DisplayMetaData
                {
                    DisplayId = displayId,
                    Name = newDisplayName,
                };
                await SetDisplayMetadata(ret);
            }
            if (ret == null)
            {
                if (DisplayMetaData.ContainsKey(displayId))
                {
                    DisplayMetaData.Remove(displayId);
                }
            }
            else
            {
                DisplayMetaData[displayId] = ret;
            }
            return ret;
        }
        /// <summary>
        /// Start connecting a new display
        /// </summary>
        /// <returns></returns>
        public async Task<MIMatrixDisplay?> ConnectDisplay()
        {
            var display = new MIMatrixDisplay(JS);
            var connected = await display.Connect();
            if (!connected)
            {
                display.Dispose();
                return null;
            }
            // remove the device if it was previously connected and still in the list (prevent duplicates)
            var existing = GetDisplay(display!.DeviceId!);
            if (existing != null)
            {
                existing.Disconnect();
                existing.Dispose();
            }
            await UpdateDisplayMetaData();
            var metadata = await GetDisplayMetadata(display.DeviceId!, true);
            Displays[display.DeviceId!] = display;
            OnDisplayAdded?.Invoke(display);
            return display;
        }
        /// <summary>
        /// Find a connected display by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public MIMatrixDisplay? GetDisplay(string id) => Displays.TryGetValue(id, out var display) ? display : null;
        /// <summary>
        /// Remove a connected display
        /// </summary>
        /// <param name="display"></param>
        /// <param name="forget"></param>
        /// <returns></returns>
        public bool RemoveDisplay(MIMatrixDisplay display, bool forget = false)
        {
            if (display == null) return false;
            if (!Displays.ContainsKey(display.DeviceId!)) return false;
            display.Disconnect();
            display.Dispose();
            Displays.Remove(display.DeviceId!);
            OnDisplayRemoved?.Invoke(display);
            return true;
        }
        /// <summary>
        /// Remove a connected display
        /// </summary>
        /// <param name="id"></param>
        /// <param name="forget"></param>
        /// <returns></returns>
        public bool RemoveDisplay(string id, bool forget = false)
        {
            return RemoveDisplay(GetDisplay(id)!, forget);
        }
        /// <summary>
        /// Remove all connected displays
        /// </summary>
        /// <param name="forget"></param>
        public void RemoveAllDisplays(bool forget = false)
        {
            var displays = Displays.ToDictionary();
            foreach (var display in displays.Values)
            {
                RemoveDisplay(display, forget);
            }
        }
    }
}
