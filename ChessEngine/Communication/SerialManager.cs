using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace ChessEngine.Communication.Serial
{
    public class ArduinoDevice
    {
        public const UInt16 Vid = 0x2341;
        public const UInt16 Pid = 0x0042;
    }

    public static class SerialManager
    {
        public static DataWriter writer = null;
        public static DataReader reader = null;
        public static SerialDevice serialDevice = null;
        public static async Task Initiate()
        {
            var deviceSelector = SerialDevice.GetDeviceSelectorFromUsbVidPid(ArduinoDevice.Vid, ArduinoDevice.Pid);
            var deviceWatcher = DeviceInformation.CreateWatcher(deviceSelector);
            deviceWatcher.Added += DeviceWatcher_AddedAsync;
            deviceWatcher.Removed += DeviceWatcher_Removed;

            var device = await DeviceInformation.FindAllAsync(deviceSelector);
            serialDevice = await SerialDevice.FromIdAsync(device[0].Id);
            serialDevice.BaudRate = 9600;
            serialDevice.DataBits = 8;
            serialDevice.Parity = SerialParity.None;
            serialDevice.StopBits = SerialStopBitCount.One;

            writer = new DataWriter(serialDevice.OutputStream);
            reader = new DataReader(serialDevice.InputStream);
        }

        private static void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            writer.DetachStream();
            reader.DetachStream();
            writer = null;
            reader = null;
            serialDevice = null;
        }

        private static async void DeviceWatcher_AddedAsync(DeviceWatcher sender, DeviceInformation args)
        {
            var deviceSelector = SerialDevice.GetDeviceSelectorFromUsbVidPid(ArduinoDevice.Vid, ArduinoDevice.Pid);
            var device = await DeviceInformation.FindAllAsync(deviceSelector);
            serialDevice = await SerialDevice.FromIdAsync(device[0].Id);
            serialDevice.BaudRate = 9600;
            serialDevice.DataBits = 8;
            serialDevice.Parity = SerialParity.None;
            serialDevice.StopBits = SerialStopBitCount.One;

            writer = new DataWriter(serialDevice.OutputStream);
            reader = new DataReader(serialDevice.InputStream);
        }
    }
}
