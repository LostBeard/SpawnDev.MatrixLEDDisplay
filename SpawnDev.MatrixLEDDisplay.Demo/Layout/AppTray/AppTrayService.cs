using SpawnDev.BlazorJS;

namespace SpawnDev.MatrixLEDDisplay.Demo.Layout.AppTray
{
    /// <summary>
    /// App tray icon service. Usedwith AppTrayArea.razor component
    /// </summary>
    public class AppTrayService
    {
        List<AppTrayIcon> _TrayIcons { get; } = new List<AppTrayIcon>();
        public IEnumerable<AppTrayIcon> TrayIcons => ReverseOrder ? _TrayIcons.AsReadOnly().Reverse() : _TrayIcons.AsReadOnly();
        public event Action OnStateHasChanged = default!;
        public bool ReverseOrder { get; set; } = true;
        public bool IsWindow { get; }
        /// <summary>
        /// New instance
        /// </summary>
        public AppTrayService(BlazorJSRuntime js)
        {
            IsWindow = js.IsWindow;
        }
        public void Add(AppTrayIcon trayIcon)
        {
            _TrayIcons.Add(trayIcon);
            StateHasChanged();
        }
        public void Remove(AppTrayIcon trayIcon)
        {
            _TrayIcons.Remove(trayIcon);
            StateHasChanged();
        }
        public void StateHasChanged()
        {
            OnStateHasChanged?.Invoke();
        }
        public bool FirstRenderFired { get; private set; } = false;
        public delegate void AfterRenderDelegate(bool firstRender);
        public event AfterRenderDelegate OnAfterRender = default!;
        public void AfterRender(bool firstRender)
        {
            OnAfterRender?.Invoke(firstRender);
        }
    }
}
