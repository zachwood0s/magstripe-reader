using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public interface IServerController
    {
        void OnOpen(Station station);
        void OnMessage(string ID, string message);
        void OnClose(string ID);
    }
}
