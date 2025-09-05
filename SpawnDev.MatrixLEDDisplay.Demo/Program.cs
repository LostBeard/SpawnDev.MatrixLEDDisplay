using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.MatrixLEDDisplay;
using SpawnDev.MatrixLEDDisplay.Demo;
using SpawnDev.MatrixLEDDisplay.Demo.Layout;
using SpawnDev.MatrixLEDDisplay.Demo.Layout.AppTray;
using SpawnDev.MatrixLEDDisplay.Demo.Services;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Threading.Tasks;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddBlazorJSRuntime(out var JS);
builder.Services.AddSingleton<MatrixLEDDisplayService>();

if (JS.IsWindow)
{
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
}

builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<DialogService>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddSingleton<TooltipService>();
builder.Services.AddSingleton<ContextMenuService>();
builder.Services.AddSingleton<ThemeService>();
builder.Services.AddSingleton<AppTrayService>();
builder.Services.AddSingleton<MainLayoutService>();
builder.Services.AddSingleton<ThemeTrayIconService>();
builder.Services.AddSingleton<AssetManifestService>();
builder.Services.AddSingleton<MediaLibraryManager>(); 

await builder.Build().BlazorJSRunAsync();
