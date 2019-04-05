using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CLH = GameServer.CommandLineHelper;
using Console = Colorful.Console;

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
            get
            {
                int id = CurrentPlayerId;
                CurrentPlayerId++;
                Program.WriteStartingId(CurrentPlayerId);
                return new PlayerCard()
                {
                    ShipData = new string('0', SHIP_DATA_LENGTH),
                    PlayerID = string.Format("{0:000}", id),
                    BaseNumber = 0,
                    Username = ""
                };
            }
        }
        public string ShipData { get; set; }
        public string PlayerID { get; set; }
        public string Username { get; set; }
        public int BaseNumber { get; set; }

        private PlayerCard() { }
        public PlayerCard(string cardInfo)
        {
            BaseNumber = cardInfo[0] - '0';
            ShipData = cardInfo.Substring(BASE_NUMBER_LENGTH, SHIP_DATA_LENGTH);
            PlayerID = cardInfo.Substring(SHIP_DATA_LENGTH + BASE_NUMBER_LENGTH, PLAYER_ID_LENGTH);
            int len = SHIP_DATA_LENGTH + BASE_NUMBER_LENGTH + PLAYER_ID_LENGTH;
            Username = len < cardInfo.Length ? cardInfo.Substring(len, cardInfo.Length - len) : "";
        }
        public override string ToString()
            => $"{BaseNumber}{ShipData}{PlayerID}{Username}";

        public string Printable()
            => $"Base: {BaseNumber + 1}, ShipData: {ShipData}, PlayerId: {PlayerID}, Username: {Username}";

        public bool IsValid()
            => !(ShipData == null || PlayerID == null || BaseNumber > MAX_BASE_NUMBER);

        public override bool Equals(object obj)
            => obj is PlayerCard c ? PlayerID.Equals(c.PlayerID) : false;
    }

    public class Score
    {
        public string Username { get; set; }
        public int Value { get; set; }

        public override string ToString()
            => $"{Username}: {Value}";
    }

    public class ServerController : IServerController
    {
        public const string DASHES = "-----------------------------------------------";

        private readonly List<string>[] _allStations;
        private readonly Dictionary<string, Station> _stationIds;
        private readonly CLH.Menu _mainMenu;
        private readonly HashSet<string> _takenStations;
        private readonly HashSet<string> _cardsInSystem;
        private readonly List<PlayerCard> _incomingCards;

        private readonly Highscore _highScoreDialog;

        public List<Score> Scores { get; set; }

        public ServerController()
        {
            _allStations = new[] { new List<string>(), new List<string>(), new List<string>() };
            _takenStations = new HashSet<string>();
            _cardsInSystem = new HashSet<string>();
            _incomingCards = new List<PlayerCard>();
            _stationIds = new Dictionary<string, Station>();
            _mainMenu = new CLH.Menu("Main Menu");
            _mainMenu.AddOption(
                ("Formatting Mode", _FormattingMode),
                ("Card Entry", _CardEnterMode));

            Scores = new List<Score>();
            _highScoreDialog = new Highscore(Scores);
        }

        public void Start()
        {
            _highScoreDialog.ShowDialog();
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
                    Console.WriteLine("Failed to connect to card reader!", Color.Red);
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
                    Console.WriteLine("Failed to connect to card reader!", Color.Red);
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
                    Console.WriteLine($"Found an incoming card: {existingCard}", Color.Green);
                    if (existingCard.BaseNumber <= PlayerCard.MAX_BASE_NUMBER)
                    {
                        _SendToBase(reader, existingCard);
                    }
                    else
                    {
                        Console.WriteLine("Reached last base...");
                        _ChoseCardDestination(reader, currentCard, () => { });
                    }
                };

            void handleNewCard(PlayerCard card, string msg, Action sentAction)
            {
                Console.WriteLine(msg);
                _ChoseCardDestination(reader, card, sentAction);
            };
            #endregion Helper Methods

            Console.WriteLine($"\n{DASHES}", Color.Green);
            Console.WriteLine("Waiting for card swipe...");
            return reader.Read().GetReadableData()
                .OnSuccess(isValidCard)
                .OnFailure(formatCard)
                .OnSuccess(card =>
                    getExistingCard(card).Match(
                        Ok: handleExistingCard(card),
                        Err: (err) =>
                        {
                            if (_cardsInSystem.Contains(card.PlayerID))
                            {
                                handleNewCard(card, "This card appears to already be in the system!",
                                    () => _cardsInSystem.Remove(card.PlayerID));
                            }
                            else
                            {
                                handleNewCard(card, err, () => { });
                            }
                        }));
        }

        private void _ChoseCardDestination(CardReader reader, PlayerCard card, Action sentAction)
        {
            var baseNumber = CLH.PromptNumber("What station should I send this player to? (0 to ignore)", 0, PlayerCard.MAX_BASE_NUMBER+1);
            if (baseNumber > 0)
            {
                card.BaseNumber = baseNumber - 1;
                sentAction();
                _SendToBase(reader, card);
            }
        }

        private PlayerCard _FormatCard(CardReader reader)
        {
            Console.WriteLine(DASHES, Color.Yellow);
            Console.WriteLine("Formatting card");
            var card = PlayerCard.Default;
            reader.Write(CardData.Create(card.ToString()));
            Console.WriteLine("Card successfully formatted", Color.Green);
            Console.WriteLine(DASHES, Color.Yellow);
            return card;
        }

        private void _SendToBase(CardReader reader, PlayerCard card)
        {
            var availableStation = _GetAvailableStations(card.BaseNumber).FirstOrDefault();
            if (availableStation != null)
            {
                Console.WriteLine($"Sending to base {card.BaseNumber + 1}");
                Console.WriteLine("Awaiting card write...");
                reader.Write(CardData.Create(card.ToString()));
                var socket = _stationIds[availableStation];
                socket.SendData(card.ToString());
                _incomingCards.Remove(card);
                _takenStations.Add(availableStation);
                _cardsInSystem.Add(card.PlayerID);
                Console.WriteLine($"SEND PLAYER TO STATION {card.BaseNumber}", Color.HotPink);
                Console.WriteLine(DASHES, Color.Green);

                Console.WriteLine($"\n{DASHES}", Color.Gray);
                Console.WriteLine($"Sent {card.Printable()}", Color.Gray);
                Console.WriteLine(DASHES, Color.Gray);
            }
            else
            {
                Console.WriteLine("No available stations at this point. Please check back later", Color.Red);
            }
        }

        public void OnOpen(Station station)
        {
            Console.WriteLine($"Station connected with ID: {station.ID}", Color.Gray);
            _stationIds.Add(station.ID, station);
        }

        public void OnMessage(string ID, string message)
        {
            switch (message[0])
            {
                case 'S':
                    Console.WriteLine($"\n{DASHES}", Color.Gray);
                    Console.WriteLine($"Recieved first connect message from: {ID}", Color.Gray);
                    _AddStation(ID, int.Parse(message[1].ToString()));
                    break;
                case 'H':
                    Console.WriteLine($"Recieved highscore, {message}", Color.Gray);
                    _HandleScore(message);
                    //H,Carddata,score
                    break;

                case '9':
                    Console.WriteLine($"Recieved reset from: {ID}", Color.Gray);
                    _Reset(ID);
                    break;
                default:
                    Console.WriteLine($"\n{DASHES}", Color.Gray);
                    Console.WriteLine($"Recieved card info from: {ID}, {message}", Color.Gray);
                    _HandleCardNum(ID, message);
                    break;
            }
        }

        private void _AddStation(string ID, int station)
        {
            Console.WriteLine($"Adding {ID} to station {station}", Color.Gray);
            Console.WriteLine(DASHES, Color.Gray);
            _allStations[station].Add(ID);
        }

        private void _HandleCardNum(string ID, string message)
        {
            var card = new PlayerCard(message);
            Console.WriteLine($"Got card info: {card.Printable()}", Color.Gray);
            Console.WriteLine(DASHES, Color.Gray);
            _incomingCards.Add(card);
            _takenStations.Remove(ID);
            _cardsInSystem.Remove(card.PlayerID);
        }

        private void _HandleScore(string message)
        {
            var parts = message.Split(',');
            var cardData = new PlayerCard(parts[1]);
            var score = int.Parse(parts[2]);
            Scores.Add(new Score() { Username = cardData.Username, Value = score });
        }

        private void _Reset(string ID) {
            _takenStations.Remove(ID); 
        }

        public void OnClose(string ID)
        {
            Console.WriteLine($"Station disconnected: {ID}", Color.Red);
            _takenStations.Add(ID); //HACK FOR THIS
        }


        private IEnumerable<string> _GetAvailableStations(int stationNum)
            => from station in _allStations[stationNum]
               where !_takenStations.Contains(station)
               select station;


    }
}
