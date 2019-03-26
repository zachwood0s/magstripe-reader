using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace GameServer
{
    public class Station: WebSocketBehavior
    {
        private readonly IServerController _controller;
        public Station(IServerController controller)
        {
            _controller = controller;
        }
        protected override void OnOpen()
        {
            _controller.OnOpen(this);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            _controller.OnClose(ID);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            _controller.OnMessage(ID, e.Data);
        }

        public void SendData(string data)
        {
            Send(data);
        }
    }
}
