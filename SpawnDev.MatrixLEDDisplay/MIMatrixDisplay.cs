using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.Toolbox;
using static System.Net.Mime.MediaTypeNames;

namespace SpawnDev.MatrixLEDDisplay
{
    public class ColorRGBA
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;
        public string ToHex() => $"#{Convert.ToHexString(new[] { r, g, b, a })}";
    }
    public class ColorRGB
    {
        public byte r;
        public byte g;
        public byte b;
        public string ToHex() => $"#{Convert.ToHexString(new[] { r, g, b })}";
    }
    /// <summary>
    /// Connects to and controls a Merkury Innovations MI Matrix Display 16x16 LED panel using BLE
    /// </summary>
    public class MIMatrixDisplay : IDisposable
    {
        BlazorJSRuntime JS;
        double _gamma = 0.6d;
        /// <summary>
        /// The gamma used when drawing to the matrix display
        /// </summary>
        public double Gamma
        {
            get => _gamma;
            set
            {
                if (_gamma == value) return;
                _gamma = value;
                _ = SendPicture();
            }
        }
        (byte r, byte g, byte b) _backgroundColor = (194, 136, 36);
        /// <summary>
        /// This color will be used as the background color for images with transparency.
        /// </summary>
        public string BackgroundColorHex
        {
            get => $"#{Convert.ToHexString(new[] { _backgroundColor.r, _backgroundColor.g, _backgroundColor.b })}";
            set
            {
                if (string.IsNullOrEmpty(value) || !value.StartsWith("#")) return;
                var bytes = Convert.FromHexString(value.Substring(1));
                BackgroundColor = (bytes[0], bytes[1], bytes[2]);
            }
        }
        /// <summary>
        /// This color will be used as the background color for images with transparency.
        /// </summary>
        public (byte r, byte g, byte b) BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor == value) return;
                _backgroundColor = value;
                _ = SendPicture();
            }
        }
        /// <summary>
        /// Paired Bluetooth device id in base 64 format (could change.) This appears to be generated when the device is paired and may change is re-paired.
        /// </summary>
        public string? DeviceId { get; private set; }
        /// <summary>
        /// Paired Bluetooth device id in hex format. This appears to be generated when the device is paired and may change is re-paired.
        /// </summary>
        public string? DeviceHexId
        {
            get
            {
                if (string.IsNullOrEmpty(DeviceId)) return null;
                var bytes = Convert.FromBase64String(DeviceId);
                var ret = Convert.ToHexString(bytes);
                return ret;
            }
        }
        /// <summary>
        /// The device name
        /// </summary>
        public string DeviceName { get; private set; } = "MI Matrix Display";
        /// <summary>
        /// The device BLE service UUID
        /// </summary>
        public string BLEServiceId { get; private set; } = "0000ffd0-0000-1000-8000-00805f9b34fb";
        /// <summary>
        /// The device LED BLE characteristic UUID
        /// </summary>
        public string LEDCharacteristicId { get; private set; } = "0000ffd1-0000-1000-8000-00805f9b34fb";
        /// <summary>
        /// The device Notify BLE characteristic UUID (currently unsure what this is used for)
        /// </summary>
        public string NotifyCharacteristicId { get; private set; } = "0000ffd2-0000-1000-8000-00805f9b34fb";
        // Bluetooth vars
        /// <summary>
        /// Bluetooth device
        /// </summary>
        public BluetoothDevice? BLEDevice { get; private set; }
        /// <summary>
        /// Bluetooth device GATT server
        /// </summary>
        public BluetoothRemoteGATTServer? BLEServer { get; private set; }
        /// <summary>
        /// Bluetooth device GATT service
        /// </summary>
        public BluetoothRemoteGATTService? BLEService { get; private set; }
        /// <summary>
        /// Bluetooth device GATT characteristic
        /// </summary>
        public BluetoothRemoteGATTCharacteristic? NotifyCharacteristic { get; private set; }
        /// <summary>
        /// Bluetooth device GATT characteristic
        /// </summary>
        public BluetoothRemoteGATTCharacteristic? LEDCharacteristic { get; private set; }
        /// <summary>
        /// True if the device is currently connected
        /// </summary>
        public bool Connected { get; private set; } = false;
        /// <summary>
        /// Fired when the device's state has changed
        /// </summary>
        public event Action<MIMatrixDisplay> OnStateChanged = default!;
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="js"></param>
        public MIMatrixDisplay(BlazorJSRuntime js)
        {
            JS = js;
        }
        /// <summary>
        /// Creates a simple test image
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public (byte r, byte g, byte b, byte a)[] CreateTestPicture(byte n = 1)
        {
            var ret = new (byte r, byte g, byte b, byte a)[256];
            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    var r = x * 16;
                    var g = y * 16;
                    var b = n * 255;
                    var i = y * 16 + x;
                    ret[i] = ((byte)r, (byte)g, (byte)b, (byte)255);
                }
            }
            return ret;
        }
        /// <summary>
        /// Opens a file picker to select an image that will be loaded onto the display.
        /// </summary>
        /// <returns></returns>
        public async Task SelectAndLoadImage()
        {
            if (BLEServer != null && BLEServer.Connected && BLEService != null && LEDCharacteristic != null)
            {
                var files = await FilePicker.ShowOpenFilePicker("", false);
                if (files == null || files.Length == 0) return;
                var file = files[0];
                var imageDataUrl = await FileReader.ReadAsDataURLAsync(file);
                if (string.IsNullOrEmpty(imageDataUrl)) return;
                using var image = await HTMLImageElement.CreateFromImageAsync(imageDataUrl);
                await SendPicture(image);
            }
        }
        /// <summary>
        /// Opens a file picker to select an image that will be loaded onto the display.
        /// </summary>
        /// <returns></returns>
        public async Task LoadImageFromURL(string url)
        {
            if (BLEServer != null && BLEServer.Connected && BLEService != null && LEDCharacteristic != null)
            {
                if (string.IsNullOrEmpty(url)) return;
                using var image = await HTMLImageElement.CreateFromImageAsync(url);
                await SendPicture(image);
            }
        }
        /// <summary>
        /// Sends a simple test picture to the display
        /// </summary>
        /// <returns></returns>
        public async Task SendTestPicture()
        {
            if (BLEServer != null && BLEServer.Connected && BLEService != null && LEDCharacteristic != null)
            {
                var initCommand = new byte[] { 0xbc, 0x0f, 0xf1, 0x08, 0x08, 0x55 };
                await LEDCharacteristic.WriteValueWithoutResponse(initCommand);
                await Task.Delay(2);
                //
                var imageData = CreateTestPicture(1);
                await SendPicture(imageData);
            }
        }
        /// <summary>
        /// Connect to a display
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Connect()
        {
            if (Connected)
            {
                return Connected;
            }
            try
            {
                // chrome://bluetooth-internals/#devices
                // MI Matric Display
                // EF:64:12:41:B0:A0
                // R8Hpj1WJAAeSNSsxWUrgeA==
                // Services:
                // Service #1 - Primary
                // 0000ffd0-0000-1000-8000-00805f9b34fb
                // Characteristics:
                // - 0000ffd1-0000-1000-8000-00805f9b34fb
                // - - Properties:
                // - - - WriteWithoutResponse
                // - 0000ffd2-0000-1000-8000-00805f9b34fb
                // - - Properties:
                // - - - Notify
                // - - Descriptors:
                // - - - 00002902-0000-1000-8000-00805f9b34fb
                // Service #2
                // 0000af30-0000-1000-8000-00805f9b34fb
                // -----------------------------------
                using var navigator = JS.Get<Navigator>("navigator");
                using var bluetooth = navigator.Bluetooth;
                if (bluetooth == null) return false;
                var options = new BluetoothDeviceOptions
                {
                    AcceptAllDevices = true,
                    OptionalServices = new[] { BLEServiceId }
                };
                if (!string.IsNullOrEmpty(DeviceName))
                {
                    options.Filters = new BluetoothDeviceFilter[] {
                        new BluetoothDeviceFilter
                        {
                            Name  = DeviceName,
                        },
                    };
                    options.AcceptAllDevices = false;
                }
                // 
                BLEDevice = await bluetooth!.RequestDevice(options);
                BLEDevice.OnGATTServerDisconnected += Device_OnGATTServerDisconnected;
                //bleState = $"Connected to device {device.Name}";
                BLEServer = await BLEDevice.GATT!.Connect();
                BLEService = await BLEServer.GetPrimaryService(BLEServiceId);
                // LED characteristic - WriteNoResponse
                LEDCharacteristic = await BLEService.GetCharacteristic(LEDCharacteristicId);
                // ? characteristic - Notify
                NotifyCharacteristic = await BLEService.GetCharacteristic(NotifyCharacteristicId);
                NotifyCharacteristic.OnCharacteristicValueChanged += SensorCharacteristicFound_OnCharacteristicValueChanged;
                await NotifyCharacteristic.StartNotifications();
                DeviceId = BLEDevice.Id;
                DeviceName = BLEDevice.Name;
                Connected = true;
                //timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            { }
            return Connected;
        }
        void StateHasChanged()
        {
            OnStateChanged?.Invoke(this);
        }
        void Device_OnGATTServerDisconnected(Event e)
        {
            if (Connected)
            {
                Disconnect();
            }
            StateHasChanged();
        }
        void SensorCharacteristicFound_OnCharacteristicValueChanged(Event e)
        {
            //using var characteristic = e.TargetAs<BluetoothRemoteGATTCharacteristic>();
            //using var value = characteristic.Value;
            //retrievedValue = textDecoder!.Decode(value.Buffer);
            //timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            StateHasChanged();
        }
        /// <summary>
        /// Disconnect from the display
        /// </summary>
        /// <param name="forget"></param>
        public void Disconnect(bool forget = false)
        {
            if (!Connected) return;
            Connected = false;
            LEDCharacteristic?.Dispose();
            LEDCharacteristic = null;
            if (NotifyCharacteristic != null)
            {
                NotifyCharacteristic.OnCharacteristicValueChanged -= SensorCharacteristicFound_OnCharacteristicValueChanged;
                if (BLEServer?.Connected == true)
                {
                    _ = NotifyCharacteristic.StopNotifications();
                }
                NotifyCharacteristic.Dispose();
                NotifyCharacteristic = null;
            }
            if (BLEService != null)
            {
                BLEService.Dispose();
                BLEService = null;
            }
            if (BLEServer != null)
            {
                if (BLEServer.Connected)
                {
                    // this will cause Device_OnGATTServerDisconnected to fire.
                    BLEServer.Disconnect();
                }
                BLEServer.Dispose();
                BLEServer = null;
            }
            if (BLEDevice != null)
            {
                // Forget is disabled... it is listed as a method for BluetoothDevice but does not exist in Chrome...
                //if (forget) device.Forget();
                BLEDevice.OnGATTServerDisconnected -= Device_OnGATTServerDisconnected;
                BLEDevice.Dispose();
                BLEDevice = null;
            }
        }
        /// <summary>
        /// The last drawn RGB data. If the image had any transparency, the background color was used to create this data when the data was last set.
        /// </summary>
        public (byte r, byte g, byte b)[] DrawnData { get; private set; } = new (byte r, byte g, byte)[256];
        /// <summary>
        /// The last set RGBA data. Transparency data has not been processed in this data.
        /// </summary>
        public (byte r, byte g, byte b, byte a)[] Data { get; private set; } = new (byte r, byte g, byte, byte a)[256];
        /// <summary>
        /// Sends a 16x16 RGBA image to the display.
        /// </summary>
        /// <param name="imageDataBytes">A byte array containing RGBA, 4 bytes per pixel, data to be sent to the display after gamma and transparency processing.</param>
        /// <returns></returns>
        public Task SendPictureRGBA(byte[] imageDataBytes)
        {
            var imageData = new (byte r, byte g, byte b, byte a)[256];
            for (int n = 0; n < imageDataBytes.Length; n += 4)
            {
                var i = n / 4;
                var r = imageDataBytes[n];
                var g = imageDataBytes[n + 1];
                var b = imageDataBytes[n + 2];
                var a = imageDataBytes[n + 3];
                imageData[i] = (r, g, b, a);
            }
            return SendPicture(imageData);
        }
        /// <summary>
        /// Resends the current picture to the display using the current settings.
        /// </summary>
        /// <returns></returns>
        public Task SendPicture()
        {
            return SendPicture(Data);
        }
        byte GammaCorrect(byte v, double gammaCorrection)
        {
            var s = (double)v / 255d;
            var gc = Math.Pow(s, gammaCorrection);
            return (byte)(gc * 255d);
        }
        public async Task SendPicture(HTMLImageElement image)
        {
            if (BLEServer != null && BLEServer.Connected && BLEService != null && LEDCharacteristic != null)
            {
                using var canvas = new OffscreenCanvas(16, 16);
                using var ctx = canvas.Get2DContext(new CanvasRenderingContext2DSettings { WillReadFrequently = true });
                var biggestSide = Math.Max(image.NaturalWidth, image.NaturalHeight);
                if (biggestSide <= 16)
                {
                    // center
                    var x = (int)Math.Round((16 - image.NaturalWidth) / 2d);
                    var y = (int)Math.Round((16 - image.NaturalHeight) / 2d);
                    ctx.DrawImage(image, x, y, image.NaturalWidth, image.NaturalHeight);
                }
                else
                {
                    // scale down and center
                    var scale = 16f / (float)biggestSide;
                    var scaleWidth = (int)(image.NaturalWidth * scale);
                    var scaleHeight = (int)(image.NaturalHeight * scale);
                    var x = (int)Math.Round((16 - scaleWidth) / 2d);
                    var y = (int)Math.Round((16 - scaleHeight) / 2d);
                    ctx.DrawImage(image, x, y, scaleWidth, scaleHeight);
                }
                var imageDataBytes = ctx.GetImageBytes()!;
                //
                var initCommand = new byte[] { 0xbc, 0x0f, 0xf1, 0x08, 0x08, 0x55 };
                await LEDCharacteristic.WriteValueWithoutResponse(initCommand);
                await Task.Delay(2);
                //
                await SendPictureRGBA(imageDataBytes);
            }
        }
        public async Task SendPicture(OffscreenCanvas image)
        {
            if (BLEServer != null && BLEServer.Connected && BLEService != null && LEDCharacteristic != null)
            {
                var width = image.Width;
                var height = image.Height;
                using var canvas = new OffscreenCanvas(16, 16);
                using var ctx = canvas.Get2DContext(new CanvasRenderingContext2DSettings { WillReadFrequently = true });
                var biggestSide = Math.Max(width, height);
                if (biggestSide <= 16)
                {
                    // center
                    var x = (int)Math.Round((16 - width) / 2d);
                    var y = (int)Math.Round((16 - height) / 2d);
                    ctx.DrawImage(image, x, y, width, height);
                }
                else
                {
                    // scale down and center
                    var scale = 16f / (float)biggestSide;
                    var scaleWidth = (int)(width * scale);
                    var scaleHeight = (int)(height * scale);
                    var x = (int)Math.Round((16 - scaleWidth) / 2d);
                    var y = (int)Math.Round((16 - scaleHeight) / 2d);
                    ctx.DrawImage(image, x, y, scaleWidth, scaleHeight);
                }
                var imageDataBytes = ctx.GetImageBytes()!;
                //
                var initCommand = new byte[] { 0xbc, 0x0f, 0xf1, 0x08, 0x08, 0x55 };
                await LEDCharacteristic.WriteValueWithoutResponse(initCommand);
                await Task.Delay(2);
                //
                await SendPictureRGBA(imageDataBytes);
            }
        }
        /// <summary>
        /// Sends a 16x16 RGB image to the display.
        /// </summary>
        /// <param name="imageData"></param>
        /// <returns></returns>
        public Task<bool> SendPicture((byte r, byte g, byte b)[] imageData)
        {
            return SendPicture(imageData.Select(o => (o.r, o.g, o.b, (byte)255)).ToArray());
        }
        /// <summary>
        /// Sends a 16x16 RGBA image to the display.
        /// </summary>
        /// <param name="imageData"></param>
        /// <returns></returns>
        public async Task<bool> SendPicture((byte r, byte g, byte b, byte a)[] imageData)
        {
            if (LEDCharacteristic == null) return false;
            Data = imageData;
            // process transparency
            for (var i = 0; i < imageData.Length; i++)
            {
                var srcPixel = imageData[i];
                var r = srcPixel.r;
                var g = srcPixel.g;
                var b = srcPixel.b;
                if (srcPixel.a < 255)
                {
                    var an = (double)srcPixel.a / 255d;
                    r = (byte)double.Lerp(BackgroundColor.r, r, an);
                    g = (byte)double.Lerp(BackgroundColor.g, g, an);
                    b = (byte)double.Lerp(BackgroundColor.b, b, an);
                }
                DrawnData[i] = (r, g, b);
            }
            // process gamma and send
            try
            {
                var gammaCorrection = 1d / Gamma;
                for (var blockIndex = 0; blockIndex < 8; blockIndex++)
                {
                    var blockData = new byte[100];
                    blockData[0] = 0xbc;
                    blockData[1] = 0x0f;
                    blockData[2] = (byte)((blockIndex + 1) & 0xff);
                    for (var i = 0; i < 32; i++)
                    {
                        var pixelIndex = blockIndex * 32 + i;
                        var srcPixel = DrawnData[pixelIndex];
                        var r = srcPixel.r;
                        var g = srcPixel.g;
                        var b = srcPixel.b;
                        blockData[3 + i * 3] = GammaCorrect(r, gammaCorrection);     // Red
                        blockData[3 + i * 3 + 1] = GammaCorrect(g, gammaCorrection); // Green
                        blockData[3 + i * 3 + 2] = GammaCorrect(b, gammaCorrection); // Blue
                    }
                    blockData[99] = 0x55;
                    await LEDCharacteristic.WriteValueWithoutResponse(blockData);
                    await Task.Delay(5);
                }
                StateHasChanged();
                return true;
            }
            catch
            { }
            return false;
        }
        /// <summary>
        /// Disconnect and release resources
        /// </summary>
        public void Dispose()
        {
            Disconnect();
        }
    }
}
