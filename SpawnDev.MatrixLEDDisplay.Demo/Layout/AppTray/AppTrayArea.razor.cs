using Microsoft.AspNetCore.Components;

namespace SpawnDev.MatrixLEDDisplay.Demo.Layout.AppTray
{
    public partial class AppTrayArea : IDisposable
    {
        [Inject]
        AppTrayService TrayIconService { get; set; } = default!;

        protected override void OnInitialized()
        {
            TrayIconService.OnStateHasChanged += TrayIconService_OnStateHasChanged;
        }
        protected override void OnAfterRender(bool firstRender)
        {
            TrayIconService.AfterRender(firstRender);
        }
        public void Dispose()
        {
            TrayIconService.OnStateHasChanged -= TrayIconService_OnStateHasChanged;
        }
        private void TrayIconService_OnStateHasChanged()
        {
            StateHasChanged();
        }
    }
}
