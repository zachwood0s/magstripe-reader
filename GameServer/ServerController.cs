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
        private readonly CLH.Menu _mainMenu;
        private readonly HashSet<string> _takenStations;
        private readonly List<PlayerCard> _incomingCards;

        public ServerController()
        {
            _allStations = new[] { new List<string>(), new List<string>(), new List<string>() };
            _takenStations = new HashSet<string>();
            _incomingCards = new List<PlayerCard>();
            _stationIds = new Dictionary<string, Station>();
            _mainMenu = new CLH.Menu("Main Menu");
            _mainMenu.AddOption(
                ("Formatting Mode", _FormattingMode),
                ("Card Entry", _CardEnterMode));
        }

        public void Start()
        {
            _mainMenu.DisplayMenu();
        }

        private void _CardEnterMode()
        {
            using (var reader = new CardReader())
            {
                if (reader.Connect())
                {
                    reader.Reset();
                    while (true)
                    {
                        _HandleCardSwipe(reader);
                    }
                }
                else
                {
                    Console.WriteLine("Failed to connect to card reader!");
                }
            }
        }

        private void _FormattingMode()
        {
            using (var reader = new CardReader())
            {
                if (reader.Connect())
                {
                    reader.Reset();
                    int counter = 0;
                    while (true)
                    {
                        _FormatCard(reader);
                        Console.WriteLine($"Formatted {++counter} cards");
                    }
                }
                else
                {
                    Console.WriteLine("Failed to connect to card reader!");
                }
            }
        }

        private void _HandleCardSwipe(CardReader reader)
        {
            bool retry = true;
            while (retry)
            {
                _ProcessCard(reader).Match(
                    Ok: () => retry = false,
                    Err: () => Console.WriteLine("Retrying..."));
            }
            
        }

        private Result _ProcessCard(CardReader reader)
        {
            #region Helper Methods
            Result<PlayerCard> isValidCard(string data)
            {
                var pc = new PlayerCard(data);
                return pc.IsValid()
                    ? Result.Ok(pc)
                    : Result.Fail<PlayerCard>("Card not setup correctly!");
            }

            Result<PlayerCard> formatCard(Result<PlayerCard> error)
                => CLH.PromptYesNo($"Error reading card data: {error.Error} Format?",
                    () => Result.Ok(_FormatCard(reader)),
                    () => error );

            Result<PlayerCard> getExistingCard(PlayerCard card)
                => _incomingCards.Contains(card)
                    ? Result.Ok(_incomingCards.Find(x => x.Equals(card)))
                    : Result.Fail<PlayerCard>($"No incoming cards with this player ID: {card.PlayerID}");

            Action<PlayerCard> handleExistingCard(PlayerCard currentCard)
                => existingCard =>
                {
                    Console.WriteLine($"Found an incoming card: {existingCard}");
                    if (existingCard.BaseNumber <= PlayerCard.MAX_BASE_NUMBER)
                    {
                        _SendToBase(reader, existingCard);
                    }
                    else
                    {
                        Console.WriteLine("Reached last base...");
                        _ChoseCardDestination(reader, currentCard);
                    }
                    Console.WriteLine(currentCard);
                };

            Action<string> handleNewCard(PlayerCard card)
            => msg =>
            {
                Console.WriteLine(msg);
                _ChoseCardDestination(reader, card);
            };
            #endregion Helper Methods

            Console.WriteLine("Waiting for card swipe...");
            return reader.Read().GetReadableData()
                .OnSuccess(isValidCard)
                .OnFailure(formatCard)
                .OnSuccess(card =>
                    getExistingCard(card).Match(
                        Ok: handleExistingCard(card),
                        Err: handleNewCard(card)));
        }

        private void _ChoseCardDestination(CardReader reader, PlayerCard card)
        {
            card.BaseNumber = CLH.PromptNumber("What station should I send this player to? ", 0, PlayerCard.MAX_BASE_NUMBER);
            _SendToBase(reader, card);
        }

        private PlayerCard _FormatCard(CardReader reader)
        {
            Console.WriteLine("Formatting card");
            var card = PlayerCard.Default;
            reader.Write(CardData.Create(card.ToString()));
            Console.WriteLine("Card successfully formatted");
            return card;
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
