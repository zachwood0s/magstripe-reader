using HidLibrary;
using System;
using System.Collections.Generic;
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
            Task t = Task.Run(controller.Start);
            
            var ws = new WebSocketServer(8000);
            ws.AddWebSocketService("/Station", () => new Station(controller));
            ws.Start();
            Console.WriteLine("Started server!");
            await t;
            ws.Stop();
            /*
            using(var reader = new CardReader())
            {
                if (reader.Connect())
                {
                    Console.WriteLine($"Read in: {reader.Read().GetReadableData()}");
                    reader.Write(CardData.Create("1"));
                    Console.WriteLine($"Read in: {reader.Read().GetReadableData()}"); 
                    reader.Write(CardData.Create("2"));
                    Console.WriteLine($"Read in: {reader.Read().GetReadableData()}"); 
                    reader.Write(CardData.Create("3"));
                    Console.WriteLine($"Read in: {reader.Read().GetReadableData()}"); 
                    
                }
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
            */
        }
    }
}
