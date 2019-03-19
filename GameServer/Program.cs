using HidLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameServer
{
    class Program
    {
        private const int VendorId = 0x0801;
        private const int ProductId = 0x0003;
        private static HidDevice _device;

        private static byte ESCAPE = 0x1B;
        private static byte OFF_LED = 0x81;
        private static byte GREEN_LED = 0x83;
        private static byte YELLOW_LED = 0x84;
        private static byte RED_LED = 0x85;
        private static byte WRITE = 0x72;
        static void Main(string[] args)
        {
            using(var reader = new CardReader())
            {
                if (reader.Connect())
                {
                    var report = reader.Read();
                    Console.WriteLine(Encoding.ASCII.GetString(report.Data));
                }
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
        
        private static void Read()
        {
            _device.WriteFeatureData(extendCommand(0x00, 0xc5, ESCAPE, WRITE));
            _device.ReadReport(OnReport);
        }

        private static byte[] extendCommand(params byte[] command)
            => command.Concat(Enumerable.Repeat<byte>(0x00, 64 - command.Length)).ToArray();

        private static void OnReport(HidReport report)
        {
            Console.WriteLine(report.Data);
        }

        private static void DeviceAttachedHandler()
        {
            Console.WriteLine("device attached");
            _device.ReadReport(OnReport);
        }

        private static void DeviceRemovedHandler()
        {
            Console.WriteLine("removed");
        }
    }
}
