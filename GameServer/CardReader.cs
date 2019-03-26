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

        private byte[] _data;

        private List<HidReport> _responses;

        private static class Commands
        {
            public const byte RESET = 0x61;
            public const byte ESCAPE = 0x1B;
            public const byte READ = 0x72;
            public const byte WRITE = 0x77;
            public const byte WRITE_S = 0x73;
            public const byte WRITE_RAW = 0x6E;
            public const byte QUESTION = 0x3F;
            public const byte FS = 0x1C;
            public const byte TEST = 0x65;
            public const byte HI_CO = 0x78;
        }

        private HidDevice _device;

        public bool Connect()
        {
            _responses = new List<HidReport>();
            var device = HidDevices.Enumerate(VENDOR_ID, PRODUCT_ID).FirstOrDefault();
            if (device == null) return false;

            _device = device;

            _device.OpenDevice();

            _device.Inserted += _DeviceAttachedHandler;
            _device.Removed += _DeviceRemovedHandler;

            _device.MonitorDeviceEvents = true;

            return _Initialize();
        }

        private bool _Initialize()
        {
            Reset();
            if (CommunicationTest())
            {
                Reset();
                SetHico();
                return true;
            }
            return false;
        }

        public bool CommunicationTest()
        {
            var result = _SendCommandWait(0xC5, Commands.ESCAPE, Commands.TEST);
            return result.Data[2] == 121;
        }

        public CardData Read()
        {
            var report = _SendCommandWait(0xC2, Commands.ESCAPE, Commands.READ);
            _data = report.Data.TakeWhile(x => x != Commands.QUESTION).ToArray();
            
            return CardData.Create(report);
        }

        public HidReport Write(CardData data)
        {
            var command = new byte[] { Commands.ESCAPE, Commands.RESET, Commands.ESCAPE, Commands.WRITE };
            var pre = new byte[] { Commands.ESCAPE, Commands.WRITE_S, Commands.ESCAPE, 0x01 };
            var cardData = data.GetWritableData();
            var post = new byte[]{Commands.QUESTION, Commands.FS};//, Commands.ESCAPE, Commands.WRITE_S};
            var report = _SendCommandWait(command.Concat(pre).Concat(cardData).Concat(post).ToArray());
            return report;
        }

        public void Reset()
        {
            _SendCommand(new byte[] { 0xC2, Commands.ESCAPE, Commands.RESET });
        }

        public void SetHico()
        {
            var report = _SendCommandWait(new byte[] { 0xC2, Commands.ESCAPE, Commands.HI_CO });
        }

        private void _SendCommand(params byte[] command)
        {
            _device.WriteFeatureData(_ExtendCommand(command.Prepend((byte) 0x00).ToArray()));
        }

        private HidReport _SendCommandWait(params byte[] command)
        {
            _SendCommand(command);

            _device.ReadReport(_OnReport);
            while(_responses.Count == 0)
            {
                Thread.Sleep(2);
            }
            Thread.Sleep(2);
            var result = _responses.FirstOrDefault();
            _responses.Clear();
            return result;
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

        private void _OnReport(HidReport r)
        {
            _responses.Add(r);
        }
    }
}
