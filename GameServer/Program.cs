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
        }
    }
}
