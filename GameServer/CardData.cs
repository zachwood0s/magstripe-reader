using HidLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class CardData
    {
        private byte[] _bytes;
        private CardData()
        {
        }

        public static CardData Create(string cardData)
        {
            var data = new CardData
            {
                _bytes = Encoding.ASCII.GetBytes(cardData)
            };
            return data;
        }

        public static CardData Create(HidReport report)
        {
            var data = new CardData();
            var bytes = report.Data.SkipWhile(b => b != 1).Skip(2).TakeWhile(b => b != 27);
            data._bytes = bytes.Take(bytes.Count() - 1).ToArray();
            return data;
        }

        public byte[] GetWritableData()
        {
            return _bytes;
        }

        public string GetReadableData()
        {
            return Encoding.ASCII.GetString(_bytes);
        }
        
    }
}
