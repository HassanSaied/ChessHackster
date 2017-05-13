using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace ChessMaster.I2C
{
    public static class I2CManager
    {
        private static I2cDevice device;
        public static void Initiate(int slaveAddress, I2cBusSpeed speedMode)
        {
            var settings = new I2cConnectionSettings(slaveAddress);
            settings.BusSpeed = speedMode;
            var controller = I2cController.GetDefaultAsync().GetResults();
            device = controller.GetDevice(settings);
           
        }

        public static void Send(byte[] buffer)
        {
            if (device == null)
                throw new InvalidOperationException();
            device.WritePartial(buffer); 
        }
        public static void Get(ref byte[] buffer)
        {
            if(device == null)
                throw new InvalidOperationException();
            device.ReadPartial(buffer);
        }
    }
}
