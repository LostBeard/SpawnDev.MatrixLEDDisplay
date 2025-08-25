using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.MatrixLEDDisplay;
using SpawnDev.MatrixLEDDisplay.Demo;
using SpawnDev.MatrixLEDDisplay.Demo.Layout;
using SpawnDev.MatrixLEDDisplay.Demo.Layout.AppTray;
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

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var _textArea = @"
02 04 05
00 ff ff 00 ff ff 00 ff ff ff 00 ff ff 00 ff ff   
00 ff ff 00 ff ff 00 ff ff 00 ff ff 00 ff ff 00   
ff ff ff ff ff ff ff e7 00 ff e7 00 ff e7 00 ff   
00 ff ff 00 ff ff ff 00 ff e7 00 ff ff 00 ff ff   
00 ff ff 00 ff ff 00 ff ff 00 ff ff 00 ff ff 00 
ff ff 00 ff ff 00 ff 00 ff ff 00 ff ff 52 ff fe
";
//var tt = @"
//0f 01 d8  
//ff e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff   
//e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9   
//d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9 d8   
//ff e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff   
//e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9   
//d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9    
//";

_textArea = @"
02 f1 04
";

_textArea = @"
01 01 00 32 ff ff ff
";

_textArea = @"
0f 01 d8
ff e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff   
e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9   
d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9 d8   
ff e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff   
e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9   
d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9 d8 ff e9
";

var apples = "apples";
JS.Set("_apples", apples);
var applesString = JS.Get<SpawnDev.BlazorJS.JSObjects.String>("_apples");
var applesValueOf = applesString.ValueOf();

var dataChunk = Convert.FromHexString(_textArea.Trim().Replace(" ", "").Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", ""));
var totalBytes = dataChunk.Length;


var crc = CalcCRC(dataChunk);
var crc2 = (byte)dataChunk.Select(o => (int)o).Sum();
// 31
var crcReal31 = Convert.FromHexString("31")[0];
// f7
var crcReal = Convert.FromHexString("f7")[0];
// ba
var crcReal1 = Convert.FromHexString("ba")[0];
var yay = crc == crcReal;
var crcReal2 = Convert.FromHexString("10")[0];
builder.Services.AddRadzenComponents();
builder.Services.AddScoped<AppTrayService>();
builder.Services.AddScoped<MainLayoutService>();
builder.Services.AddScoped<ThemeTrayIconService>();

await builder.Build().BlazorJSRunAsync();


byte CalcCRC(byte[] imageChunk)
{
    byte crc = 0;
    for (var i = 0; i < imageChunk.Length; i++)
    {
        var b = imageChunk[i];
        crc += b;
    }
    return crc;
}

