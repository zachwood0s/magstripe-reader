using HidLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace GameServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var controller = new ServerController();
            PlayerCard.CurrentPlayerId = GetStartingId();
            Task t = Task.Run(controller.Start);
            
            var ws = new WebSocketServer(8000);
            ws.AddWebSocketService("/Station", () => new Station(controller));
            ws.Start();
            Console.WriteLine("Started server!");
            await t;
            ws.Stop();
        }

        public static int GetStartingId()
        {
            using (var reader = new StreamReader("config.txt"))
            {
                if(int.TryParse(reader.ReadLine(), out var id))
                {
                    return id;
                }
                else
                {
                    return 0;
                }
            }
        }

        public static void WriteStartingId(int value)
        {
            using (var writer = new StreamWriter("config.txt"))
            {
                writer.WriteLine(value);
            }
        }
    }
}
