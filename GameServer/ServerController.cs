using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CLH = GameServer.CommandLineHelper;

namespace GameServer
{
    public class PlayerCard
    {
        public const int SHIP_DATA_LENGTH = 9;
        public const int PLAYER_ID_LENGTH = 3;
        public const int BASE_NUMBER_LENGTH = 1;
        public const int MAX_BASE_NUMBER = 2;
        public static int CurrentPlayerId = 0;

        public static PlayerCard Default
        {
            get =>
                new PlayerCard()
                {
                    ShipData = new string('0', SHIP_DATA_LENGTH),
                    PlayerID = CurrentPlayerId++.ToString(),
                    BaseNumber = 0
                };
        }
        public string ShipData { get; set; }
        public string PlayerID { get; set; }
        public int BaseNumber { get; set; }

        private PlayerCard() { }
        public PlayerCard(string cardInfo)
        {
            BaseNumber = cardInfo[0] - '0';
            ShipData = new string(cardInfo.Skip(BASE_NUMBER_LENGTH).Take(SHIP_DATA_LENGTH).ToArray());
            PlayerID = new string(cardInfo.Skip(SHIP_DATA_LENGTH + BASE_NUMBER_LENGTH).Take(PLAYER_ID_LENGTH).ToArray());
        }
        public override string ToString()
            => $"{BaseNumber}{ShipData}{PlayerID}";

        public string Printable()
            => $"Base: {BaseNumber}, ShipData: {ShipData}, PlayerId: {PlayerID}";

        public bool IsValid()
            => !(ShipData == null || PlayerID == null || BaseNumber > MAX_BASE_NUMBER);

        public override bool Equals(object obj)
            => obj is PlayerCard c ? PlayerID.Equals(c.PlayerID) : false;
    }

    public class ServerController : IServerController
    {
        private readonly List<string>[] _allStations;
        private readonly Dictionary<string, Station> _stationIds;
        private readonly HashSet<string> _takenStations;
        private readonly List<PlayerCard> _incomingCards;

        public ServerController()
        {
            _allStations = new[] { new List<string>(), new List<string>(), new List<string>() };
            _takenStations = new HashSet<string>();
            _incomingCards = new List<PlayerCard>();
            _stationIds = new Dictionary<string, Station>();
        }

        public void Start()
        {
            while (true)
            {
                using (var reader = new CardReader())
                {
                    if (reader.Connect())
                    {
                        reader.Reset();
                        _HandleCardSwipe(reader);
                    }
                    else
                    {
                        Console.WriteLine("Failed to connect to card reader!");
                    }
                }
            }
        }

        private void _HandleCardSwipe(CardReader reader)
        {
            //Wow this is actually terrible code. Need to refactor
            Console.WriteLine("Waiting for card swipe...");
            var card = reader.Read();
            PlayerCard playerCard = null;
            if(card.GetReadableData().Length == 0)
            {
                CLH.PromptYesNo("Error reading card data. Retrying. Format?", 
                    () => _FormatCard(reader, out playerCard),
                    () => {
                        Console.WriteLine("Retrying...");
                        _HandleCardSwipe(reader);
                    });
            }
            else
            {
                playerCard = new PlayerCard(card.GetReadableData());
            }

            if(playerCard != null && !playerCard.IsValid())
            {
                CLH.PromptYesNo("This card appears to not be setup correctly. Would you like to format?", 
                    () => _FormatCard(reader, out playerCard), 
                    () =>
                    {
                        _HandleCardSwipe(reader);
                        Console.WriteLine("Nothing left to do. Resetting.");
                    });
            }

            if (_incomingCards.Contains(playerCard))
            {
                var existingCard = _incomingCards.Find(x => x.Equals(playerCard));
                Console.WriteLine("Found an incoming card");
                Console.WriteLine(existingCard);
                if(existingCard.BaseNumber <= PlayerCard.MAX_BASE_NUMBER)
                {
                    _SendToBase(reader, existingCard);
                }
                else
                {
                    Console.WriteLine("Reached last base...");
                    _ChoseCardDestination(reader, playerCard);
                }
            }
            else
            {
                Console.WriteLine($"No incoming cards with this PlayerID ({playerCard.PlayerID})");
                _ChoseCardDestination(reader, playerCard);
            }

            Console.WriteLine(playerCard); 
        }

        private void _ChoseCardDestination(CardReader reader, PlayerCard card)
        {
            Console.Write("What station should I send this player to? ");
            int baseNumber = int.Parse(Console.ReadLine());
            card.BaseNumber = baseNumber > PlayerCard.MAX_BASE_NUMBER ? PlayerCard.MAX_BASE_NUMBER : baseNumber;
            _SendToBase(reader, card);
        }

        private void _FormatCard(CardReader reader, out PlayerCard card)
        {
            Console.WriteLine("Formatting card");
            card = PlayerCard.Default;
            reader.Write(CardData.Create(card.ToString()));
            Console.WriteLine("Card successfully formatted");
        }

        private void _SendToBase(CardReader reader, PlayerCard card)
        {
            var availableStation = _GetAvailableStations(card.BaseNumber).FirstOrDefault();
            if (availableStation != null)
            {
                Console.WriteLine($"Sending to base {card.BaseNumber}");
                Console.WriteLine("Awaiting card write...");
                reader.Write(CardData.Create(card.ToString()));
                var socket = _stationIds[availableStation];
                socket.SendData(card.ToString());
                _incomingCards.Remove(card);
                _takenStations.Add(availableStation);
            }
            else
            {
                Console.WriteLine("No available stations at this point. Please check back later");
            }
        }

        public void OnOpen(Station station)
        {
            Console.WriteLine($"Station connected with ID: {station.ID}");
            _stationIds.Add(station.ID, station);
        }

        public void OnMessage(string ID, string message)
        {
            switch (message[0])
            {
                case 'S':
                    Console.WriteLine($"Recieved first connect message from: {ID}");
                    _AddStation(ID, int.Parse(message[1].ToString()));
                    break;
                default:
                    Console.WriteLine($"Recieved card info from: {ID}, {message}");
                    _HandleCardNum(ID, message);
                    break;
            }
        }

        private void _AddStation(string ID, int station)
        {
            Console.WriteLine($"Adding {ID} to station {station}");
            _allStations[station].Add(ID);
        }

        private void _HandleCardNum(string ID, string message)
        {
            _incomingCards.Add(new PlayerCard(message));
            _takenStations.Remove(ID);
        }

        public void OnClose(string ID)
        {
            Console.WriteLine($"Station disconnected: {ID}");
            _takenStations.Add(ID); //HACK FOR THIS
        }


        private IEnumerable<string> _GetAvailableStations(int stationNum)
            => from station in _allStations[stationNum]
               where !_takenStations.Contains(station)
               select station;


    }
}
