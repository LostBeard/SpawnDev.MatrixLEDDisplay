using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.Toolbox;
using SpawnDev.MatrixLEDDisplay.ImageTools;
using System.Runtime.InteropServices;

namespace SpawnDev.MatrixLEDDisplay
{
    // ID: EF:64:12:41:80:A0
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

            public static byte[] SlideShowMarker { get; } = MakeBC(new byte[] { 0x02, 0x07, 0x3c });

            public static byte[] SlideShowImageWriteEnable(byte count) => MakeBC(new byte[] { 0x02, 0xf1, count });

            public static byte[] SlideShowImageWriteDisable(byte count) => MakeBC(new byte[] { 0x02, 0xf2, count });
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
                _ = Resend();
            }
        }
        RGBPixel _backgroundColor = (194, 136, 36);
        /// <summary>
        /// This color will be used as the background color for images with transparency.
        /// </summary>
        public RGBPixel BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor == value) return;
                _backgroundColor = value;
                _ = Resend();
            }
        }
        public RGBImage GetFrame(int i)
        {
            return i >= 0 && i < SentImages.Count ? SentImages.ElementAtOrDefault(i): new RGBImage(16, 16);
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
        //public (byte r, byte g, byte b, byte a)[] CreateTestPicture(byte n = 1)
        //{
        //    var ret = new (byte r, byte g, byte b, byte a)[256];
        //    for (var y = 0; y < 16; y++)
        //    {
        //        for (var x = 0; x < 16; x++)
        //        {
        //            var r = x * 16;
        //            var g = y * 16;
        //            var b = n * 255;
        //            var i = y * 16 + x;
        //            ret[i] = ((byte)r, (byte)g, (byte)b, (byte)255);
        //        }
        //    }
        //    return ret;
        //}
        public RGBAImage CreateTestPicture(byte n = 1)
        {
            var ret = new RGBAImage(16, 16);
            ret.ForEachXY((x, y) => ret.Set(x, y, (byte)(x * 16), (byte)(y * 16), (byte)(n * 16), 255));
            return ret;
        }
        public RGBAImage CreateTestPicture2(byte n)
        {
            var ret = new RGBAImage(16, 16);
            for (var i = 0; i < ret.Length; i++)
            {
                var p = ret[i];
                p.A = 255;
                p.R = p.G = p.B = i == (int)n ? (byte)255 : (byte)0;
            }
            return ret;
        }
        public async Task Reset()
        {
            if (LEDCharacteristic == null) return;
            SourceData.Clear();
            SentImages.Clear();
            SlideShowFrames.Clear();
            SourceIsSlideShow = false;
            _save = false;
            await LEDCharacteristic.WriteValueWithoutResponse(Command.Reset);
        }
        public async Task PowerOff()
        {
            if (LEDCharacteristic == null) return;
            await LEDCharacteristic.WriteValueWithoutResponse(Command.PowerOff);
        }
        public async Task PowerOn()
        {
            if (LEDCharacteristic == null) return;
            await LEDCharacteristic.WriteValueWithoutResponse(Command.PowerOn);
        }
        public async Task StartSlideShowMode()
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
                SetBusy(true);
                try
                {
                    var files = await FilePicker.ShowOpenFilePicker("image/*", false);
                    if (files == null || files.Length == 0) return;
                    var file = files[0];
                    if (file.Name.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                    {
                        var gifBytes = await file.ArrayBuffer().UsingAsync(async o => (await o).ReadBytes());
                        var frames = await GifHelper.GetGIFFrames(gifBytes, 16, 16);
                        Console.WriteLine($"frames: {frames.Count}");
                        await SendSlideShow(frames, save);
                        await Task.Delay(500);
                    }
                    else
                    {
                        var imageObjectUrl = URL.CreateObjectURL(file);
                        if (string.IsNullOrEmpty(imageObjectUrl)) return;
                        using var image = await HTMLImageElement.CreateFromImageAsync(imageObjectUrl);
                        URL.RevokeObjectURL(imageObjectUrl);
                        await SendImage(image, save);
                    }
                }
                finally
                {
                    SetBusy(false);
                }
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
                await SendImage(image, save);
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
                await SendImage(imageData, save);
            }
        }
        public int SlideShowFrameCount => SentImages.Count;
        List<(int imageIndex, int sourceI)> SlideShowFrames = new List<(int imageIndex, int sourceI)>();
        public async Task SendSlideShow(List<RGBAImage> images, bool save = false)
        {
            if (BLEServer != null && BLEServer.Connected && BLEService != null && LEDCharacteristic != null)
            {
                SetBusy(true);
                try
                {
                    SlideShowFrames.Clear();
                    SourceData.Clear();
                    SentImages.Clear();
                    SourceIsSlideShow = false;
                    SourceData.AddRange(images);
                    _save = save;
                    var staticWriteEnabled = false;
                    if (save)
                    {
                        staticWriteEnabled = true;

                        await LEDCharacteristic.WriteValueWithoutResponse(Command.Reset);
                        await Task.Delay(500);

                        await LEDCharacteristic.WriteValueWithoutResponse(Command.StaticImageWriteEnable);
                        await Task.Delay(50);
                    }
                    await LEDCharacteristic.WriteValueWithoutResponse(Command.StartSlideShowMode);
                    await Task.Delay(500);
                    //var count = Math.Min(images.Count, 8);
                    var count = 8;
                    var slideShowFrames = new List<(int displayI, int sourceI)>();
                    for (var i = 0; i < count; i++)
                    {
                        var n = i % images.Count;
                        var imageData = images[n];
                        var imageDataRGB = imageData.ToRGBImage(BackgroundColor);
                        SentImages.Add(imageDataRGB);
                        var imageIndex = (byte)(i + 1);
                        slideShowFrames.Add((imageIndex, n));
                        await SendSlideShowImage(imageIndex, (byte)count, save, imageDataRGB);
                        await Task.Delay(50);
                        Console.WriteLine($"{i} / {n} / {count}");
                    }
                    SlideShowFrames.AddRange(slideShowFrames);
                    if (staticWriteEnabled)
                    {
                        staticWriteEnabled = false;
                        await LEDCharacteristic.WriteValueWithoutResponse(Command.StaticImageWriteDisable);
                        await Task.Delay(50);

                    }
                    await LEDCharacteristic.WriteValueWithoutResponse(Command.StartSlideShowMode);
                    await Task.Delay(50);
                }
                finally
                {
                    SetBusy(false);
                }
            }
        }
        ///// <summary>
        ///// Sends a simple test slide show
        ///// </summary>
        ///// <returns></returns>
        //public async Task SendTestSlideShow()
        //{
        //    if (BLEServer != null && BLEServer.Connected && BLEService != null && LEDCharacteristic != null)
        //    {
        //        //
        //        //await LEDCharacteristic.WriteValueWithoutResponse(Command.Commit);
        //        //await LEDCharacteristic.WriteValueWithoutResponse(Command.Reset);
        //        //await Task.Delay(5);
        //        await LEDCharacteristic.WriteValueWithoutResponse(Command.StartSlideShowMode);
        //        await Task.Delay(50);
        //        await LEDCharacteristic.WriteValueWithoutResponse(Command.StaticImageWriteEnable);
        //        await Task.Delay(50);
        //        var count = 8;
        //        var startPo = 1;
        //        for (var i = startPo; i < startPo + count; i++)
        //        {
        //            var imageData = CreateTestPicture2((byte)(i - startPo));
        //            await SendSlideShowImage((byte)i, (byte)count, true, imageData);
        //            await Task.Delay(50);
        //        }
        //        await LEDCharacteristic.WriteValueWithoutResponse(Command.StaticImageWriteDisable);
        //        await Task.Delay(50);
        //        await LEDCharacteristic.WriteValueWithoutResponse(Command.StartSlideShowMode);
        //        await Task.Delay(50);
        //    }
        //}
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
                Console.WriteLine("Disconnected");
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
        /// Last source sent
        /// </summary>
        public List<RGBAImage> SourceData { get; } = new List<RGBAImage>();
        public List<RGBImage> SentImages { get; } = new List<RGBImage>();
        /// <summary>
        /// Set to true when a slideshow is sent
        /// </summary>
        public bool SourceIsSlideShow { get; private set; }
        bool _save = false;
        /// <summary>
        /// Resends the current picture to the display using the current settings.
        /// </summary>
        /// <returns></returns>
        public async Task Resend()
        {
            if (!SourceData.Any()) return;
            if (SourceIsSlideShow)
            {
                await SendSlideShow(SourceData, _save);
            }
            else
            {
                await SendImage(SourceData.First(), _save);
            }
        }
        /// <summary>
        /// Save the current data as teh static picture.
        /// </summary>
        /// <returns></returns>
        public async Task SavePicture()
        {
            if (SourceIsSlideShow)
            {
                await SendSlideShow(SourceData, true);
            }
            else
            {
                await SendImage(SourceData.First(), true);
            }
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
        public async Task SendImage(HTMLImageElement image, bool save = false)
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
                await SendImage(new RGBAImage(16, 16, imageDataBytes), save);
            }
        }
        /// <summary>
        /// Sends a 16x16 RGBA image to the display.
        /// </summary>
        /// <param name="imageData"></param>
        /// <returns></returns>
        public async Task SendImage(RGBAImage imageData, bool save = false)
        {
            if (LEDCharacteristic == null) return;
            SlideShowFrames.Clear();
            SourceData.Clear();
            SentImages.Clear();
            SourceIsSlideShow = false;
            SourceData.Add(imageData);
            _save = save;
            var DrawnData = imageData.ToRGBImage(BackgroundColor);
            SentImages.Add(DrawnData);
            // process gamma and send
            SetBusy(true);
            try
            {
                var gammaCorrection = 1d / Gamma;
                if (save)
                {
                    await LEDCharacteristic.WriteValueWithoutResponse(Command.Reset);
                    await Task.Delay(500);
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
                        var r = srcPixel.R;
                        var g = srcPixel.G;
                        var b = srcPixel.B;
                        blockData[i * 3] = GammaCorrect(r, gammaCorrection);     // Red
                        blockData[i * 3 + 1] = GammaCorrect(g, gammaCorrection); // Green
                        blockData[i * 3 + 2] = GammaCorrect(b, gammaCorrection); // Blue
                    }
                    await LEDCharacteristic.WriteValueWithoutResponse(Command.TempImageWriteChunk((byte)(blockIndex + 1), blockData));
                    await Task.Delay(50);
                }
                await LEDCharacteristic.WriteValueWithoutResponse(Command.TempImageWriteDisable);
                await Task.Delay(50);
                if (save)
                {
                    await LEDCharacteristic.WriteValueWithoutResponse(Command.StaticImageWriteDisable);
                    await Task.Delay(50);
                }
            }
            finally
            {
                SetBusy(false);
            }
        }
        private async Task SendSlideShowImage(byte index, byte count, bool save, RGBImage imageData)
        {
            if (LEDCharacteristic == null) return;
            if (imageData.Width != 16 || imageData.Height != 16) throw new ArgumentOutOfRangeException("Image must be 16x16 pixels");
            SetBusy(true);
            try
            {
                // process gamma and send
                var gammaCorrection = 1d / Gamma;
                await LEDCharacteristic.WriteValueWithoutResponse(Command.SlideShowImageWriteEnable(count));
                await Task.Delay(5);
                for (int blockIndex = 0; blockIndex < 8; blockIndex++)
                {
                    var blockData = new byte[96];
                    for (var i = 0; i < 32; i++)
                    {
                        var pixelIndex = blockIndex * 32 + i;
                        var srcPixel = imageData[pixelIndex];
                        var r = srcPixel.R;
                        var g = srcPixel.G;
                        var b = srcPixel.B;
                        blockData[i * 3] = GammaCorrect(r, gammaCorrection);     // Red
                        blockData[i * 3 + 1] = GammaCorrect(g, gammaCorrection); // Green
                        blockData[i * 3 + 2] = GammaCorrect(b, gammaCorrection); // Blue
                    }
                    await LEDCharacteristic.WriteValueWithoutResponse(Command.SlideShowImageWriteChunk(index, (byte)(blockIndex + 1), blockData));
                    await Task.Delay(5);
                }
                if (count == index && save)
                {
                    await LEDCharacteristic.WriteValueWithoutResponse(Command.SlideShowMarker);
                    await Task.Delay(5);
                }
                await LEDCharacteristic.WriteValueWithoutResponse(Command.SlideShowImageWriteDisable(count));
                await Task.Delay(5);
            }
            finally
            {
                SetBusy(false);
            }
        }
        //private async Task SendSlideShowImage(byte index, byte count, bool save, RGBAImage imageData)
        //{
        //    if (LEDCharacteristic == null) return;
        //    if (imageData.Width != 16 || imageData.Height != 16) throw new ArgumentOutOfRangeException("Image must be 16x16 pixels");
        //    SetBusy(true);
        //    try
        //    {
        //        // process transparency
        //        var DrawnData = imageData.ToRGBImage(BackgroundColor);
        //        // process gamma and send
        //        var gammaCorrection = 1d / Gamma;
        //        await LEDCharacteristic.WriteValueWithoutResponse(Command.SlideShowImageWriteEnable(count));
        //        await Task.Delay(5);
        //        for (int blockIndex = 0; blockIndex < 8; blockIndex++)
        //        {
        //            var blockData = new byte[96];
        //            for (var i = 0; i < 32; i++)
        //            {
        //                var pixelIndex = blockIndex * 32 + i;
        //                var srcPixel = DrawnData[pixelIndex];
        //                var r = srcPixel.R;
        //                var g = srcPixel.G;
        //                var b = srcPixel.B;
        //                blockData[i * 3] = GammaCorrect(r, gammaCorrection);     // Red
        //                blockData[i * 3 + 1] = GammaCorrect(g, gammaCorrection); // Green
        //                blockData[i * 3 + 2] = GammaCorrect(b, gammaCorrection); // Blue
        //            }
        //            await LEDCharacteristic.WriteValueWithoutResponse(Command.SlideShowImageWriteChunk(index, (byte)(blockIndex + 1), blockData));
        //            await Task.Delay(5);
        //        }
        //        if (count == index && save)
        //        {
        //            await LEDCharacteristic.WriteValueWithoutResponse(Command.SlideShowMarker);
        //            await Task.Delay(5);
        //        }
        //        await LEDCharacteristic.WriteValueWithoutResponse(Command.SlideShowImageWriteDisable(count));
        //        await Task.Delay(5);
        //    }
        //    finally
        //    {
        //        SetBusy(false);
        //    }
        //}
        /// <summary>
        /// Disconnect and release resources
        /// </summary>
        public void Dispose()
        {
            Disconnect();
        }

        int _busy = 0;
        public bool Busy => _busy > 0;
        void SetBusy(bool busy)
        {
            var busyi = busy ? _busy + 1 : _busy - 1;
            if (busyi < 0) busyi = 0;
            if (_busy == busyi) return;
            _busy = busyi;
            StateHasChanged();
        }
    }
}
