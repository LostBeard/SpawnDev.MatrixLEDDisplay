using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpawnDev.MatrixLEDDisplay
{
    public class MatrixLEDDisplayService : IAsyncBackgroundService
    {
        BlazorJSRuntime JS;

        public List<MIMatrixDisplay> Displays { get; } = new List<MIMatrixDisplay>();

        Task? _Ready = null;
        public Task Ready => _Ready ??= InitASync();

        public MatrixLEDDisplayService(BlazorJSRuntime js)
        {
            JS = js;
        }
        async Task InitASync()
        {

        }
        public event Action<MIMatrixDisplay> OnDisplayAdded = default!;
        public event Action<MIMatrixDisplay> OnDisplayRemoved = default!;
        public async Task<MIMatrixDisplay?> ConnectDisplay()
        {
            var display = new MIMatrixDisplay(JS);
            var connected = await display.Connect();
            if (!connected)
            {
                display.Dispose();
                return null;
            }
            // remove the device if it was previously connected and sitll in the list
            RemoveDisplay(display.BLEDevice!.Id);
            // add
            Displays.Add(display);
            OnDisplayAdded?.Invoke(display);
            return display;
        }
        public MIMatrixDisplay? GetDisplay(string id) => Displays.FirstOrDefault(o => o.BLEDevice?.Id == id);
        public bool RemoveDisplay(MIMatrixDisplay display, bool forget = false)
        {
            if (display == null) return false;
            if (!Displays.Contains(display)) return false;
            display.Disconnect(forget);
            display.Dispose();
            Displays.Remove(display);
            OnDisplayRemoved?.Invoke(display);
            return true;
        }
        public bool RemoveDisplay(string id, bool forget = false)
        {
            return RemoveDisplay(GetDisplay(id)!, forget);
        }
        public void RemoveAllDisplays(bool forget = false)
        {
            var displays = Displays.ToList();
            foreach (var display in displays)
            {
                RemoveDisplay(display, forget);
            }
        }
    }
}
