using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace GameServerTester
{
    class Program
    {
        private static WebSocket _ws;
        static async Task Main(string[] args)
        {
            using(var ws = new WebSocket("ws://localhost:8000/Station"))
            {
                _ws = ws;
                ws.Connect();
                ws.OnMessage += OnMessage;
                Console.Write("What station? ");
                string stationNum = Console.ReadLine();
                ws.Send($"S{stationNum}");
                await Task.Run(() => Handle());
            }
            Console.WriteLine("Press any key to quit...");
            Console.ReadKey(true);
        }

        public static void Handle()
        {
            Console.ReadKey();
        }

        public static void OnMessage(object sender, MessageEventArgs e)
        {
            Console.WriteLine($"Got message: {e.Data}");
            int baseNum = int.Parse(e.Data[0].ToString());
            string id = new string(e.Data.Skip(10).Take(3).ToArray());
            _ws.Send($"{baseNum + 1}011010101{id}");
        }
    }
}
