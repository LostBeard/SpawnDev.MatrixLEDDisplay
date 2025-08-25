using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.Toolbox;

namespace SpawnDev.MatrixLEDDisplay
{
    // ID: EF:64:12:41:80:A0
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
        /// Command message tools for MI Matrix Display
        /// </summary>
        public static class Command
        {
            /// <summary>
            /// Power off
            /// </summary>
            /// <returns></returns>
            public static byte[] PowerOff { get; } = MakeBC(new byte[] { 0xff, 0 });
            /// <summary>
            /// Power on
            /// </summary>
            /// <returns></returns>
            public static byte[] PowerOn { get; } = MakeBC(new byte[] { 0xff, 1 });
            /// <summary>
            /// Reset
            /// </summary>
            /// <returns></returns>SATYA NUTELLA IS A SHIT EATING RETARD
            public static byte[] Reset { get; } = MakeBC(new byte[] { 0x00, 0x15 });
            /// <summary>
            /// Start slideshow mode
            /// </summary>
            /// <returns></returns>
            public static byte[] StartSlideShowMode { get; } = MakeBC(new byte[] { 0x00, 0x12 });
            /// <summary>
            /// Clear graffiti mode
            /// </summary>
            /// <returns></returns>
            public static byte[] ClearGraffitiMode { get; } = MakeBC(new byte[] { 0x00, 0x0d });
            /// <summary>
            /// Start graffiti mode
            /// </summary>
            /// <returns></returns>
            public static byte[] StartGraffitiMode { get; } = MakeBC(new byte[] { 0x00, 0x01 });
            /// <summary>
            /// Static image write enable
            /// </summary>
            /// <returns></returns>
            public static byte[] StaticImageWriteEnable { get; } = MakeBC(new byte[] { 0x00, 0x11, 0xf1 });
            /// <summary>
            /// Static image write disable
            /// </summary>
            /// <returns></returns>
            public static byte[] StaticImageWriteDisable { get; } = MakeBC(new byte[] { 0x00, 0x11, 0xf2 });
            /// <summary>
            /// ???? Commit
            /// </summary>
            public static byte[] Commit { get; } = new byte[] { 0x01, 0x00, 0xff, 0xff }; // 0100ffff
            /// <summary>
            /// Temp image write enable
            /// </summary>
            /// <returns></returns>
            public static byte[] TempImageWriteEnable { get; } = MakeBC(new byte[] { 0x0f, 0xf1, 0x08 }); // 0100ffff  - 0x01, 0x00, 0xff, 0xff
            /// <summary>
            /// Temp image write disable
            /// </summary>
            /// <returns></returns>
            public static byte[] TempImageWriteDisable { get; } = MakeBC(new byte[] { 0x0f, 0xf2, 0x08 });
            /// <summary>
            /// Slideshow image write enable
            /// </summary>
            /// <returns></returns>
            public static byte[] SlideShowImageWriteEnable4 { get; } = MakeBC(new byte[] { 0x02, 0xf1, 0x04 });
            /// <summary>
            /// Slideshow image write disable
            /// </summary>
            /// <returns></returns>
            public static byte[] SlideShowImageWriteDisable4 { get; } = MakeBC(new byte[] { 0x02, 0xf2, 0x04 });
            /// <summary>
            /// Slideshow image write enable
            /// </summary>
            /// <returns></returns>
            public static byte[] SlideShowImageWriteEnable6 { get; } = MakeBC(new byte[] { 0x02, 0xf1, 0x06 });
            /// <summary>
            /// Slideshow image write disable
            /// </summary>
            /// <returns></returns>
            public static byte[] SlideShowImageWriteDisable6 { get; } = MakeBC(new byte[] { 0x02, 0xf2, 0x06 });
            /// <summary>
            /// Writes the slideshow image chunk RGB data
            /// </summary>
            /// <param name="imageIndex">Slideshow image index 1 - ??</param>
            /// <param name="chunkIndex">Image Chunk index: 1 - 8 where each chunk is 2 rows of RGB pixel data, 32 pixels * 3 bytes per pixel = 96 bytes</param>
            /// <param name="rgb">The 96 bytes of RGB image chunk data</param>
            /// <returns></returns>
            public static byte[] SlideShowImageWriteChunk(byte imageIndex, byte chunkIndex, byte[] rgb) => MakeBC(new byte[] { 0x02, imageIndex, chunkIndex }.Concat(rgb).ToArray());
            /// <summary>
            /// Writes the temp image chunk RGB data
            /// </summary>
            /// <param name="chunkIndex">Image Chunk index: 1 - 8 where each chunk is 2 rows of RGB pixel data, 32 pixels * 3 bytes per pixel = 96 bytes</param>
            /// <param name="rgb">The 96 bytes of RGB image chunk data</param>
            /// <returns></returns>
            public static byte[] TempImageWriteChunk(byte chunkIndex, byte[] rgb) => MakeBC(new byte[] { 0x0f, chunkIndex }.Concat(rgb).ToArray());
            /// <summary>
            /// Set a graffiti mode pixel
            /// </summary>
            /// <param name="i"></param>
            /// <param name="rgb"></param>
            /// <returns></returns>
            public static byte[] SetGraffitiPixel(byte i, params byte[] rgb) => MakeBC(new byte[] { 0x00, 0x00, 0x01, i, rgb[0], rgb[1], rgb[2] });
            /// <summary>
            /// Creates a full message with prepended start byte 0xbc.<br/>
            /// If the message length + 13 (+ 0xbc byte and 12 byte BLE preamble) is not a multiple of 32, a CRC byte, and an end of message byte 0x55 is appended.<br/>
            /// </summary>
            public static byte[] MakeBC(byte[] msg)
            {
                var fullMessageLength = msg.Length + 13; // 13 = 12 byte BLE pre-amble before message + 1 start byte before message (0xbc)
                var ret = new byte[] { 0xbc }.Concat(msg);
                var tmp = fullMessageLength % 32;
                ret = fullMessageLength % 32 == 0 ? ret.Concat(new byte[] { CalcCRC(msg) }) : ret.Concat(new byte[] { CalcCRC(msg), 0x55 });
                return ret.ToArray();
            }
            /// <summary>
            /// Calculates the truncated sum of the data as a byte value
            /// </summary>
            public static byte CalcCRC(byte[] data)
            {
                byte crc = 0;
                foreach (var b in data) crc += b;
                return crc;
            }
        }
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
        /// Bluetooth device GATT descriptor found on the characteristic that has a notify property
        /// </summary>
        public BluetoothRemoteGATTDescriptor? NotifyDescriptor { get; private set; }
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
        public (byte r, byte g, byte b, byte a)[] CreateTestPicture2(byte n)
        {
            var ret = new (byte r, byte g, byte b, byte a)[256];
            for (var i = 0; i < ret.Length; i++)
            {
                ret[i].a = 255;
                ret[i].r = ret[i].g = ret[i].b = i == (int)n ? (byte)255 : (byte)0;
            }
            return ret;
        }
        public async Task Reset()
        {
            if (LEDCharacteristic == null) return;
            Data = new (byte r, byte g, byte b, byte a)[256];
            await LEDCharacteristic.WriteValueWithoutResponse(Command.Reset);
        }
        public async Task Off()
        {
            if (LEDCharacteristic == null) return;
            await LEDCharacteristic.WriteValueWithoutResponse(Command.PowerOff);
        }
        public async Task On()
        {
            if (LEDCharacteristic == null) return;
            await LEDCharacteristic.WriteValueWithoutResponse(Command.PowerOn);
        }
        public async Task SlideShowMode()
        {
            if (LEDCharacteristic == null) return;
            await LEDCharacteristic.WriteValueWithoutResponse(Command.StartSlideShowMode);
        }
        /// <summary>
        /// Opens a file picker to select an image that will be loaded onto the display.
        /// </summary>
        /// <returns></returns>
        public async Task SelectAndLoadImage(bool save)
        {
            if (BLEServer != null && BLEServer.Connected && BLEService != null && LEDCharacteristic != null)
            {
                var files = await FilePicker.ShowOpenFilePicker("", false);
                if (files == null || files.Length == 0) return;
                var file = files[0];
                var imageObjectUrl = URL.CreateObjectURL(file);
                if (string.IsNullOrEmpty(imageObjectUrl)) return;
                using var image = await HTMLImageElement.CreateFromImageAsync(imageObjectUrl);
                URL.RevokeObjectURL(imageObjectUrl);
                await SendPicture(image, save);
            }
        }
        /// <summary>
        /// Opens a file picker to select an image that will be loaded onto the display.
        /// </summary>
        /// <returns></returns>
        public async Task LoadImageFromURL(string url, bool save)
        {
            if (BLEServer != null && BLEServer.Connected && BLEService != null && LEDCharacteristic != null)
            {
                if (string.IsNullOrEmpty(url)) return;
                using var image = await HTMLImageElement.CreateFromImageAsync(url);
                await SendPicture(image, save);
            }
        }
        /// <summary>
        /// Sends a simple test picture to the display
        /// </summary>
        /// <returns></returns>
        public async Task SendTestPicture(bool save)
        {
            if (BLEServer != null && BLEServer.Connected && BLEService != null && LEDCharacteristic != null)
            {
                //
                var imageData = CreateTestPicture(1);
                await SendTempImage(imageData, save);
            }
        }
        /// <summary>
        /// Sends a simple test slide show
        /// </summary>
        /// <returns></returns>
        public async Task SendTestSlideShow()
        {
            if (BLEServer != null && BLEServer.Connected && BLEService != null && LEDCharacteristic != null)
            {

                //
                //await LEDCharacteristic.WriteValueWithoutResponse(Command.Commit);
                await LEDCharacteristic.WriteValueWithoutResponse(Command.Reset);
                await Task.Delay(500);
                var staticWriteEnabled = false;
                await LEDCharacteristic.WriteValueWithoutResponse(Command.StaticImageWriteEnable);
                staticWriteEnabled = true;
                await Task.Delay(500);
                for (var i = 2; i <= 6; i++)
                {
                    var imageData = CreateTestPicture2((byte)(i - 1));
                    await SendSlideShowImage((byte)i, imageData);
                    await Task.Delay(50);
                    if (staticWriteEnabled)
                    {
                        staticWriteEnabled = false;
                        await LEDCharacteristic.WriteValueWithoutResponse(Command.StaticImageWriteDisable);
                        await Task.Delay(500);
                    }
                }
                //{
                //    var imageData = CreateTestPicture2((byte)1);
                //    await SendSlideShowImage((byte)1, imageData);
                //    await Task.Delay(50);
                //}
                await LEDCharacteristic.WriteValueWithoutResponse(Command.StartSlideShowMode);
                await Task.Delay(50);
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
                // NotifyDescriptor - not sure what the 2 byte value that can be read from this means. Only seen as 0x0000
                NotifyDescriptor = await NotifyCharacteristic.GetDescriptor("00002902-0000-1000-8000-00805f9b34fb");
                NotifyDescriptorValue = await NotifyDescriptor.ReadValueBytes();
                JS.Log("NotifyDescriptorValue", NotifyDescriptorValue.Select(o => (int)o).ToArray());
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
        /// <summary>
        /// The value NotifyDescriptor had when last read
        /// </summary>
        public byte[] NotifyDescriptorValue { get; private set; } = new byte[2];
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
        async void SensorCharacteristicFound_OnCharacteristicValueChanged(Event e)
        {
            if (NotifyDescriptor == null) return;
            //using var characteristic = e.TargetAs<BluetoothRemoteGATTCharacteristic>();
            //using var value = characteristic.Value;
            //retrievedValue = textDecoder!.Decode(value.Buffer);
            //timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            NotifyDescriptorValue = await NotifyDescriptor.ReadValueBytes();
            JS.Log("NotifyDescriptorValue", NotifyDescriptorValue.Select(o => (int)o).ToArray());
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
        public Task SendTempImageRGBA(byte[] imageDataBytes, bool save)
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
            return SendTempImage(imageData, save);
        }
        ///// <summary>
        ///// Sends a 16x16 RGB image to the display.
        ///// </summary>
        ///// <param name="imageDataBytes">A byte array containing RGBA, 4 bytes per pixel, data to be sent to the display after gamma and transparency processing.</param>
        ///// <returns></returns>
        //public Task SendPictureRGB(byte[] imageDataBytes)
        //{
        //    var imageData = new (byte r, byte g, byte b, byte a)[256];
        //    for (int n = 0; n < imageDataBytes.Length; n += 3)
        //    {
        //        var i = n / 4;
        //        var r = imageDataBytes[n];
        //        var g = imageDataBytes[n + 1];
        //        var b = imageDataBytes[n + 2];
        //        var a = (byte)255;
        //        imageData[i] = (r, g, b, a);
        //    }
        //    return SendTempImage(imageData);
        //}
        bool _save = false;
        /// <summary>
        /// Resends the current picture to the display using the current settings.
        /// </summary>
        /// <returns></returns>
        public Task SendPicture()
        {
            return SendTempImage(Data, _save);
        }
        /// <summary>
        /// Save the current data as teh static picture.
        /// </summary>
        /// <returns></returns>
        public Task SavePicture()
        {
            return SendTempImage(Data, true);
        }
        byte GammaCorrect(byte v, double gammaCorrection)
        {
            var s = (double)v / 255d;
            var gc = Math.Pow(s, gammaCorrection);
            return (byte)(gc * 255d);
        }
        /// <summary>
        /// Sends an HTMLImageElement to the display.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public async Task SendPicture(HTMLImageElement image, bool save)
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
                await SendTempImageRGBA(imageDataBytes, save);
            }
        }
        ///// <summary>
        ///// Sends an OffscreenCanvas to the display.
        ///// </summary>
        //public async Task SendPicture(OffscreenCanvas image)
        //{
        //    if (BLEServer != null && BLEServer.Connected && BLEService != null && LEDCharacteristic != null)
        //    {
        //        var width = image.Width;
        //        var height = image.Height;
        //        using var canvas = new OffscreenCanvas(16, 16);
        //        using var ctx = canvas.Get2DContext(new CanvasRenderingContext2DSettings { WillReadFrequently = true });
        //        var biggestSide = Math.Max(width, height);
        //        if (biggestSide <= 16)
        //        {
        //            // center
        //            var x = (int)Math.Round((16 - width) / 2d);
        //            var y = (int)Math.Round((16 - height) / 2d);
        //            ctx.DrawImage(image, x, y, width, height);
        //        }
        //        else
        //        {
        //            // scale down and center
        //            var scale = 16f / (float)biggestSide;
        //            var scaleWidth = (int)(width * scale);
        //            var scaleHeight = (int)(height * scale);
        //            var x = (int)Math.Round((16 - scaleWidth) / 2d);
        //            var y = (int)Math.Round((16 - scaleHeight) / 2d);
        //            ctx.DrawImage(image, x, y, scaleWidth, scaleHeight);
        //        }
        //        var imageDataBytes = ctx.GetImageBytes()!;
        //        //
        //        await LEDCharacteristic.WriteValueWithoutResponse(initCommand);
        //        await Task.Delay(2);
        //        //
        //        await SendPictureRGBA(imageDataBytes);
        //    }
        //}
        ///// <summary>
        ///// Sends an HTMLCanvasElement to the display.
        ///// </summary>
        //public async Task SendPicture(HTMLCanvasElement image)
        //{
        //    if (BLEServer != null && BLEServer.Connected && BLEService != null && LEDCharacteristic != null)
        //    {
        //        var width = image.Width;
        //        var height = image.Height;
        //        using var canvas = new OffscreenCanvas(16, 16);
        //        using var ctx = canvas.Get2DContext(new CanvasRenderingContext2DSettings { WillReadFrequently = true });
        //        var biggestSide = Math.Max(width, height);
        //        if (biggestSide <= 16)
        //        {
        //            // center
        //            var x = (int)Math.Round((16 - width) / 2d);
        //            var y = (int)Math.Round((16 - height) / 2d);
        //            ctx.DrawImage(image, x, y, width, height);
        //        }
        //        else
        //        {
        //            // scale down and center
        //            var scale = 16f / (float)biggestSide;
        //            var scaleWidth = (int)(width * scale);
        //            var scaleHeight = (int)(height * scale);
        //            var x = (int)Math.Round((16 - scaleWidth) / 2d);
        //            var y = (int)Math.Round((16 - scaleHeight) / 2d);
        //            ctx.DrawImage(image, x, y, scaleWidth, scaleHeight);
        //        }
        //        var imageDataBytes = ctx.GetImageBytes()!;
        //        //
        //        await LEDCharacteristic.WriteValueWithoutResponse(initCommand);
        //        await Task.Delay(2);
        //        //
        //        await SendPictureRGBA(imageDataBytes);
        //    }
        //}
        /// <summary>
        /// Sends a 16x16 RGB image to the display.
        /// </summary>
        /// <param name="imageData"></param>
        /// <returns></returns>
        public Task<bool> SendTempImage((byte r, byte g, byte b)[] imageData)
        {
            return SendTempImage(imageData.Select(o => (o.r, o.g, o.b, (byte)255)).ToArray());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg">The message to be sent. A message start byte (0xbc) will be prepended and a message end byte (0x55) will be appended to the message.</param>
        /// <param name="addCrc">
        /// Some messages, like "Write Slide Show Image Chunk" use a CRC byte as the second to last byte.<br/>
        /// When true, a simple single byte crc is calculated and added to the message.
        /// </param>
        /// <returns></returns>
        //public async Task SendCommand(byte[] msg)
        //{
        //    if (LEDCharacteristic == null) throw new NullReferenceException();
        //    byte[] fullMessage = Command.Make(msg);
        //    await LEDCharacteristic.WriteValueWithoutResponse(fullMessage);
        //}
        //public async Task SendCommand(List<byte> msg)
        //{
        //    if (LEDCharacteristic == null) throw new NullReferenceException();
        //    byte[] fullMessage = Command.Make(msg.ToArray());
        //    await LEDCharacteristic.WriteValueWithoutResponse(fullMessage);
        //}
        /// <summary>
        /// Sends a 16x16 RGBA image to the display.
        /// </summary>
        /// <param name="imageData"></param>
        /// <returns></returns>
        public async Task<bool> SendTempImage((byte r, byte g, byte b, byte a)[] imageData, bool commit = true, bool reset = true)
        {
            if (LEDCharacteristic == null) return false;
            Data = imageData;
            _save = commit;
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
                if (commit)
                {
                    if (reset)
                    {
                        await LEDCharacteristic.WriteValueWithoutResponse(Command.Reset);
                        await Task.Delay(500);
                    }
                    //await LEDCharacteristic.WriteValueWithoutResponse(Command.StartSlideShowMode);
                    //await Task.Delay(5);
                    await LEDCharacteristic.WriteValueWithoutResponse(Command.StaticImageWriteEnable);
                    await Task.Delay(50);
                }
                await LEDCharacteristic.WriteValueWithoutResponse(Command.TempImageWriteEnable);
                await Task.Delay(50);
                for (int blockIndex = 0; blockIndex < 8; blockIndex++)
                {
                    var blockData = new byte[96];
                    for (var i = 0; i < 32; i++)
                    {
                        var pixelIndex = blockIndex * 32 + i;
                        var srcPixel = DrawnData[pixelIndex];
                        var r = srcPixel.r;
                        var g = srcPixel.g;
                        var b = srcPixel.b;
                        blockData[i * 3] = GammaCorrect(r, gammaCorrection);     // Red
                        blockData[i * 3 + 1] = GammaCorrect(g, gammaCorrection); // Green
                        blockData[i * 3 + 2] = GammaCorrect(b, gammaCorrection); // Blue
                    }
                    await LEDCharacteristic.WriteValueWithoutResponse(Command.TempImageWriteChunk((byte)(blockIndex + 1), blockData));
                    await Task.Delay(50);
                }
                await LEDCharacteristic.WriteValueWithoutResponse(Command.TempImageWriteDisable);
                await Task.Delay(50);
                if (commit)
                {
                    await LEDCharacteristic.WriteValueWithoutResponse(Command.StaticImageWriteDisable);
                    await Task.Delay(50);
                }
                StateHasChanged();
                return true;
            }
            catch
            { }
            return false;
        }
        //public async Task<bool> SendSlideShowImage(byte index, byte[] rgbImageBytes)
        //{
        //    if (LEDCharacteristic == null) return false;
        //    //Data = imageData;
        //    var chunks = rgbImageBytes.distr
        //    // process transparency
        //    for (var i = 0; i < rgbImageBytes.Length; i+=4)
        //    {
        //        var srcPixel = imageData[i];
        //        var r = srcPixel.r;
        //        var g = srcPixel.g;
        //        var b = srcPixel.b;
        //        if (srcPixel.a < 255)
        //        {
        //            var an = (double)srcPixel.a / 255d;
        //            r = (byte)double.Lerp(BackgroundColor.r, r, an);
        //            g = (byte)double.Lerp(BackgroundColor.g, g, an);
        //            b = (byte)double.Lerp(BackgroundColor.b, b, an);
        //        }
        //        DrawnData[i] = (r, g, b);
        //    }
        //    for (var i = 0; i < imageData.Length; i++)
        //    {
        //        var srcPixel = imageData[i];
        //        var r = srcPixel.r;
        //        var g = srcPixel.g;
        //        var b = srcPixel.b;
        //        if (srcPixel.a < 255)
        //        {
        //            var an = (double)srcPixel.a / 255d;
        //            r = (byte)double.Lerp(BackgroundColor.r, r, an);
        //            g = (byte)double.Lerp(BackgroundColor.g, g, an);
        //            b = (byte)double.Lerp(BackgroundColor.b, b, an);
        //        }
        //        DrawnData[i] = (r, g, b);
        //    }
        //    // process gamma and send
        //    try
        //    {
        //        var gammaCorrection = 1d / Gamma;
        //        await LEDCharacteristic.WriteValueWithoutResponse(Command.SlideShowImageWriteEnable);
        //        await Task.Delay(2);
        //        for (int blockIndex = 0; blockIndex < 8; blockIndex++)
        //        {
        //            var blockData = new byte[96];
        //            for (var i = 0; i < 32; i++)
        //            {
        //                var pixelIndex = blockIndex * 32 + i;
        //                var srcPixel = DrawnData[pixelIndex];
        //                var r = srcPixel.r;
        //                var g = srcPixel.g;
        //                var b = srcPixel.b;
        //                blockData[i * 3] = GammaCorrect(r, gammaCorrection);     // Red
        //                blockData[i * 3 + 1] = GammaCorrect(g, gammaCorrection); // Green
        //                blockData[i * 3 + 2] = GammaCorrect(b, gammaCorrection); // Blue
        //            }
        //            await LEDCharacteristic.WriteValueWithoutResponse(Command.SlideShowImageWriteChunk(index, (byte)(blockIndex + 1), blockData));
        //            await Task.Delay(5);
        //        }
        //        await LEDCharacteristic.WriteValueWithoutResponse(Command.SlideShowImageWriteDisable);
        //        StateHasChanged();
        //        return true;
        //    }
        //    catch
        //    { }
        //    return false;
        //}
        public async Task<bool> SendSlideShowImage(byte index, (byte r, byte g, byte b, byte a)[] imageData)
        {
            if (LEDCharacteristic == null) return false;
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
                await LEDCharacteristic.WriteValueWithoutResponse(Command.SlideShowImageWriteEnable6);
                await Task.Delay(50);
                for (int blockIndex = 0; blockIndex < 8; blockIndex++)
                {
                    var blockData = new byte[96];
                    for (var i = 0; i < 32; i++)
                    {
                        var pixelIndex = blockIndex * 32 + i;
                        var srcPixel = DrawnData[pixelIndex];
                        var r = srcPixel.r;
                        var g = srcPixel.g;
                        var b = srcPixel.b;
                        blockData[i * 3] = GammaCorrect(r, gammaCorrection);     // Red
                        blockData[i * 3 + 1] = GammaCorrect(g, gammaCorrection); // Green
                        blockData[i * 3 + 2] = GammaCorrect(b, gammaCorrection); // Blue
                    }
                    await LEDCharacteristic.WriteValueWithoutResponse(Command.SlideShowImageWriteChunk(index, (byte)(blockIndex + 1), blockData));
                    await Task.Delay(50);
                }
                await LEDCharacteristic.WriteValueWithoutResponse(Command.SlideShowImageWriteDisable6);
                await Task.Delay(50);
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
