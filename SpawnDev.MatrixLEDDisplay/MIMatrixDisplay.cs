using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;

namespace SpawnDev.MatrixLEDDisplay
{
    public class MIMatrixDisplay : IDisposable
    {
        BlazorJSRuntime JS;

        // Define BLE Device Specs
        string deviceName = "MI Matrix Display";
        string bleService = "0000ffd0-0000-1000-8000-00805f9b34fb";
        string ledCharacteristicUUID = "0000ffd1-0000-1000-8000-00805f9b34fb";
        string sensorCharacteristic = "0000ffd2-0000-1000-8000-00805f9b34fb";

        // Global Variables to Handle Bluetooth
        public BluetoothDevice? device { get; private set; }
        public BluetoothRemoteGATTServer? bleServer { get; private set; }
        public BluetoothRemoteGATTService? bleServiceFound { get; private set; }
        public BluetoothRemoteGATTCharacteristic? sensorCharacteristicFound { get; private set; }
        public BluetoothRemoteGATTCharacteristic? characteristic { get; private set; }
        public bool Connected { get; private set; } = false;
        public MIMatrixDisplay(BlazorJSRuntime js)
        {
            JS = js;
        }
        public (byte r, byte g, byte b)[] CreateTestPicture(byte n)
        {
            var ret = new (byte r, byte g, byte b)[256];
            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    var r = x * 16;
                    var g = y * 16;
                    var b = n * 255;
                    var i = y * 16 + x;
                    ret[i] = ((byte)r, (byte)g, (byte)b);
                }
            }
            return ret;
        }
        public async Task SendTestPicture()
        {
            if (bleServer != null && bleServer.Connected && bleServiceFound != null && characteristic != null)
            {
                var initCommand = new byte[] { 0xbc, 0x0f, 0xf1, 0x08, 0x08, 0x55 };
                await characteristic.WriteValueWithoutResponse(initCommand);
                await Task.Delay(2);
                //
                var imageData = CreateTestPicture(1);
                await SendPicture(characteristic, imageData);
            }
        }
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
                    Filters = new BluetoothDeviceFilter[] {
                    new BluetoothDeviceFilter
                    {
                        Name  = deviceName,
                    },
                },
                    OptionalServices = new[] { bleService }
                };
                // 
                device = await bluetooth!.RequestDevice(options);
                device.OnGATTServerDisconnected += Device_OnGATTServerDisconnected;
                //bleState = $"Connected to device {device.Name}";
                bleServer = await device.GATT!.Connect();
                bleServiceFound = await bleServer.GetPrimaryService(bleService);
                // LED characteristic - WriteNoResponse
                characteristic = await bleServiceFound.GetCharacteristic(ledCharacteristicUUID);
                // ? characteristic - Notify
                sensorCharacteristicFound = await bleServiceFound.GetCharacteristic(sensorCharacteristic);
                sensorCharacteristicFound.OnCharacteristicValueChanged += SensorCharacteristicFound_OnCharacteristicValueChanged;
                await sensorCharacteristicFound.StartNotifications();
                Connected = true;
                //timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch (Exception ex)
            {
                //bleState = "Connect failed:" + ex.Message;
            }
            return Connected;
        }
        public event Action<MIMatrixDisplay> OnStateChanged = default!;
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
        public void Disconnect(bool forget = false)
        {
            if (!Connected) return;
            Connected = false;
            characteristic?.Dispose();
            characteristic = null;
            if (sensorCharacteristicFound != null)
            {
                sensorCharacteristicFound.OnCharacteristicValueChanged -= SensorCharacteristicFound_OnCharacteristicValueChanged;
                if (bleServer?.Connected == true)
                {
                    _ = sensorCharacteristicFound.StopNotifications();
                }
                sensorCharacteristicFound.Dispose();
                sensorCharacteristicFound = null;
            }
            if (bleServiceFound != null)
            {
                bleServiceFound.Dispose();
                bleServiceFound = null;
            }
            if (bleServer != null)
            {
                if (bleServer.Connected)
                {
                    // this will cause Device_OnGATTServerDisconnected to fire.
                    bleServer.Disconnect();
                }
                bleServer.Dispose();
                bleServer = null;
            }
            if (device != null)
            {
                if (forget) device.Forget();
                device.OnGATTServerDisconnected -= Device_OnGATTServerDisconnected;
                device.Dispose();
                device = null;
            }
        }
        public (byte r, byte g, byte b)[] Data { get; private set; } = new (byte r, byte g, byte b)[256];
        public async Task SendPicture(BluetoothRemoteGATTCharacteristic characteristic, (byte r, byte g, byte b)[] imageData)
        {
            for (var blockIndex = 0; blockIndex < 8; blockIndex++)
            {
                var blockData = new byte[100];
                blockData[0] = 0xbc;
                blockData[1] = 0x0f;
                blockData[2] = (byte)((blockIndex + 1) & 0xff);
                for (var i = 0; i < 32; i++)
                {
                    var pixelIndex = blockIndex * 32 + i;
                    blockData[3 + i * 3] = imageData[pixelIndex].r; // Red
                    blockData[3 + i * 3 + 1] = imageData[pixelIndex].g; // Green
                    blockData[3 + i * 3 + 2] = imageData[pixelIndex].b; // Blue
                }
                blockData[99] = 0x55;
                Console.WriteLine($"Sending block {blockIndex + 1} / 8");
                await characteristic.WriteValueWithoutResponse(blockData);
                await Task.Delay(5);
            }
            Data = imageData;
            StateHasChanged();
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
