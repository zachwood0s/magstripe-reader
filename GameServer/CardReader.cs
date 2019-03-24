using HidLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameServer
{
    public class CardReader: IDisposable
    {
        public const int VENDOR_ID = 0x0801;
        public const int PRODUCT_ID = 0x0003;
        private const int MAX_COMMAND_LEN = 64;

        private static class Commands
        {
            public const byte ESCAPE = 0x1B;
            public const byte READ = 0x72;
            public const byte WRITE = 0x77;
        }

        private HidDevice _device;

        public bool Connect()
        {
            var device = HidDevices.Enumerate(VENDOR_ID, PRODUCT_ID).FirstOrDefault();
            if (device == null) return false;

            _device = device;

            _device.OpenDevice();

            _device.Inserted += _DeviceAttachedHandler;
            _device.Removed += _DeviceRemovedHandler;

            _device.MonitorDeviceEvents = true;

            return true;
        }

        public CardData Read()
        {
            var report = _SendCommandWait(0xC5, Commands.ESCAPE, Commands.READ);
            return CardData.Create(report);
        }

        public HidReport Write(CardData data)
        {
            var command = new byte[] { 0xC5, Commands.ESCAPE, Commands.WRITE };
            var report = _SendCommandWait(command.Concat(data.GetWritableData()).ToArray());
            return report;
        }

        private void _SendCommand(params byte[] command)
        {
            _device.WriteFeatureData(_ExtendCommand(command.Prepend((byte) 0x00).ToArray()));
        }

        private HidReport _SendCommandWait(params byte[] command)
        {
            _SendCommand(command);
            var report = _device.ReadReport();
            return report;
        }

        private static byte[] _ExtendCommand(params byte[] command)
            => command.Concat(Enumerable.Repeat<byte>(0x00, MAX_COMMAND_LEN - command.Length)).ToArray();

        private void _DeviceAttachedHandler()
        {
            Console.WriteLine("device attached");
        }

        private void _DeviceRemovedHandler()
        {
            Console.WriteLine("removed");
        }

        public void Dispose()
        {
            if(_device == null)
            {
                Console.WriteLine("No device was ever opened! Did you forget to call Connect()?");
            }
            else
            {
                _device.CloseDevice();
            }
        }
    }
}
