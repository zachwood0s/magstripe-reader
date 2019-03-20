using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class CardData
    {
        private CardData()
        {
        }

        public static CardData Create(string cardData)
        {
            var data = new CardData();
            //Do conversion
            return data;
        }

        public static CardData Create(HidReport report)
        {
            var data = new CardData();
            // convert
            return data;
        }

        public byte[] GetWritableData()
        {
            return new byte[0];
        }

        public string GetReadableData()
        {
            return "";
        }
        
    }
}
