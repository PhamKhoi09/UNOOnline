using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace UnoOnline
{
    public class GameManager
    {
        private static readonly object lockObject = new object();
        private static GameManager instance;

        public List<Player> Players { get; set; }
        public Card CurrentCard { get; set; }
        public int CurrentPlayerIndex { get; set; }
        public bool IsSpecialDraw { get; set; }
        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new GameManager();
                        }
                    }
                }
                return instance;
            }
        }
        private GameManager()
        {
            Players = new List<Player>();
            CurrentCard = new Card();
            CurrentPlayerIndex = 0;
            IsSpecialDraw = false;
        }

        public void UpdateOtherPlayerName(string otherPlayerName)
        {
            lock (lockObject)
            {
                if (Players == null)
                {
                    Players = new List<Player>();
                }

                bool playerExists = Players.Exists(p => p.Name == otherPlayerName);
                if (!playerExists)
                {
                    Players.Add(new Player(otherPlayerName));
                }
            }

            WaitingLobby form = (WaitingLobby)Application.OpenForms.OfType<WaitingLobby>().FirstOrDefault();
            if (form != null)
            {
                form.Invoke(new Action(() =>
                {
                    form.UpdatePlayerList(Players);
                }));
            }
        }

        public static void InitializeStat(Message message)
        {
            if (Instance == null)
            {
                instance = new GameManager();
            }

            string[] data = message.Data.ToArray();
            string playerName = data[0];
            int turnOrder = int.Parse(data[1]);
            int cardCount = int.Parse(data[2]);

            // Lấy danh sách các lá bài
            List<string> cardNames = new List<string>(data.Skip(3).Take(cardCount));

            // Thêm các lá bài vào tay người chơi
            Instance.Players[0].Hand = cardNames.Select(cardData =>
            {
                string[] card = cardData.Split('_');
                string color = card[0];
                string value = card[1];
                if (color == "Wild" && value != "Draw")
                    if (color == "Wild" && value != "Draw")
                    {
                        value = "Wild";
                    }
                return new Card(cardData, color, value);
            }).ToList();

            string currentCardName = data[10];
            string[] currentCard = data[10].Split('_');
            string currentColor = currentCard[0];
            string currentValue = currentCard[1];
            Instance.CurrentCard = new Card(currentCardName, currentColor, currentValue);
        }

        public static void UpdateOtherPlayerStat(Message message)
        {
            //ID;Lượt;Số lượng bài
            string[] data = message.Data.ToArray();
            string playerName = data[0];
            int turnOrder = int.Parse(data[1]);
            int cardCount = int.Parse(data[2]);

            Player player = Instance.Players.FirstOrDefault(p => p.Name == playerName);
            if (player == null)
            {
                player = new Player(playerName);
                Instance.Players.Add(player);
            }
            player.Hand = new List<Card>(new Card[cardCount]); // Update the Hand property to reflect the correct number of cards

            Form1 form = Application.OpenForms.OfType<Form1>().FirstOrDefault();
            if (form != null)
            {
                form.Invoke(new Action(() => form.InitializeDeckImages()));
            }
        }

        Player player = Instance.Players.FirstOrDefault(p => p.Name == playerName);
            if (player == null)
            {
                player = new Player(playerName);
        Instance.Players.Add(player);
            }
    player.Hand = new List<Card>(new Card[cardCount]); // Update the Hand property to reflect the correct number of cards

            Form1 form = Application.OpenForms.OfType<Form1>().FirstOrDefault();
            if (form != null)
            {
                form.Invoke(new Action(() => form.InitializeDeckImages()));
            }
        }

        public void AddPlayer(Player player)
{
    Instance.Players.Add(player);
}


public static void Boot()
{
    //Mở màn hình game mở (nếu chưa)
    if (!Program.IsFormOpen(typeof(Form1)))
    {
        Application.OpenForms[0].Invoke(new Action(() =>
        {
            if (!Program.IsFormOpen(typeof(Form1)))
            {
                Form1 form1 = new Form1();
                form1.Show();
                form1.DisplayPlayerHand(Instance.Players[0].Hand);
                form1.UpdateCurrentCardDisplay(Instance.CurrentCard);
                //DisplayOtherPlayerHand
            }
        }));
    }
}

<<<<<<< Updated upstream
public void PlayCard(Player player, Card card)
{
}

=======
>>>>>>> Stashed changes
public bool IsValidMove(Card card)
{
    return card.Color == Instance.CurrentCard.Color || card.Value == Instance.CurrentCard.Value || card.Color == "Wild" || (Instance.CurrentCard.CardName.Contains("Wild") && card.CardName.Contains("Wild"));
}


public void HandleUpdate(Message message)
{
    try
    {
        // Message received: Update; ID; RemainingCards; CardName (if a card is played); color (if the card is a wild card)
        string playerId = message.Data[0];
        int remainingCards = int.Parse(message.Data[1]);

        // Find the player in the list
        Player player = Instance.Players.FirstOrDefault(p => p.Name == playerId);
        if (player != null)
        {
            player.Hand = new List<Card>(new Card[remainingCards]); // Update the Hand property to reflect the correct number of cards
            player.Hand = new List<Card>(new Card[remainingCards]); // Update the Hand property to reflect the correct number of cards
        }

        if (playerId != Program.player.Name)
        {
            // If another player has played a card
            if (message.Data.Count == 3)
            {
                if (Instance.CurrentCard == null)
                {
                    Instance.CurrentCard = new Card();
                }
                Instance.CurrentCard.CardName = message.Data[2];
                string[] card = message.Data[2].Split('_');
                Instance.CurrentCard.Color = card[0];
                Instance.CurrentCard.Value = card[1];
            }
            // If the card is a wild card or draw 4
            else if (message.Data.Count == 4)
            {
                if (Instance.CurrentCard == null)
                {
                    Instance.CurrentCard = new Card();
                }
                Instance.CurrentCard.CardName = message.Data[2];
                string[] card = message.Data[2].Split('_');
                Instance.CurrentCard.Value = card[1];
                Instance.CurrentCard.Color = message.Data[3];
            }
        }

        // Update the UI
        Form1 form1 = (Form1)Application.OpenForms.OfType<Form1>().FirstOrDefault();
        if (form1 != null)
        {
            form1.Invoke(new Action(() =>
            {
                if (Instance.CurrentCard != null)
                {
                    form1.UpdateCurrentCardDisplay(Instance.CurrentCard);
                }
                else
                {
                    MessageBox.Show("CurrentCard is null.");
                }
                form1.InitializeDeckImages(); // Refresh the deck images and labels
                form1.InitializeDeckImages(); // Refresh the deck images and labels
            }));
        }
        else
        {
            MessageBox.Show("Form1 is null.");
        }
    }
    catch (NullReferenceException ex)
    {
        MessageBox.Show("Object not initialized: " + ex.Message);
    }
}


<<<<<<< Updated upstream
=======
>>>>>>> Stashed changes
public static void HandleTurnMessage(Message message)
{
    try
    {
        string playerId = message.Data[0];
        MessageBox.Show($"Handling Turn message for player: {playerId}");

        if (playerId == Program.player.Name)
        {
            MessageBox.Show("It's the current player's turn.");

<<<<<<< Updated upstream
            if (Instance.CurrentCard.CardName.Contains("Draw"))
=======
                    if (Instance.CurrentCard.CardName.Contains("Draw") && Instance.IsSpecialDraw == true) //Bị rút bài
>>>>>>> Stashed changes
            {
                if (Instance.CurrentCard.CardName.Contains("Wild"))
                {
                    // Draw 4
                    ClientSocket.SendData(new Message(MessageType.SpecialCardEffect, new List<string> { Program.player.Name, (Instance.Players[0].Hand.Count + 4).ToString() }));
                    //Thoát hàm

                }
                else
                {
                    // Draw 2
                    ClientSocket.SendData(new Message(MessageType.SpecialCardEffect, new List<string> { Program.player.Name, (Instance.Players[0].Hand.Count + 2).ToString() }));
                }
            }
            else
            {
                //Enable EnableCardAndDrawButton on form1
                Form1 form1 = Application.OpenForms.OfType<Form1>().FirstOrDefault();
                if (form1 != null)
                {
                    form1.Invoke(new Action(() =>
                    {
                        form1.EnableCardAndDrawButton();
                    }));
                }
                else
                {
                    MessageBox.Show("Form1 is null.");
                }
            }
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error in HandleTurnMessage: {ex.Message}\n{ex.StackTrace}");
        throw;
    }
}
public static void HandleSpecialDraw(Message message)
{
    //Specialdraws; ID; CardName; CardName...
    string playerId = message.Data[0];
    Player player = Instance.Players.FirstOrDefault(p => p.Name == playerId);
    for (int i = 1; i < message.Data.Count; i++)
    {
        string cardName = message.Data[i];
        string[] card = cardName.Split('_');
        string color = card[0];
        string value = card[1];
        player.Hand.Add(new Card(cardName, color, value));
    }

    //Hiển thị bài trên tay
    Form1.ActiveForm.Invoke(new Action(() =>
    {
        Form1 form1 = (Form1)Application.OpenForms.OfType<Form1>().FirstOrDefault();
        if (form1 != null)
        {
<<<<<<< Updated upstream
            form1.DisplayPlayerHand(player.Hand);
=======
                    form1.Invoke(new Action(() =>
                    {
                        form1.DisplayPlayerHand(Instance.Players[0].Hand);
                        form1.InitializeDeckImages(); // Refresh the deck images and labels
                    }));
>>>>>>> Stashed changes
        }
    }));
}

public static void HandleCardDraw(Message message)
{
    string playerName = message.Data[0];
    string cardName = message.Data[1];
    string[] card = cardName.Split('_');
    string color = card[0];
    string value = card[1];
    Instance.Players[0].Hand.Add(new Card(cardName, color, value));

    Form1 form = Application.OpenForms.OfType<Form1>().FirstOrDefault();
    if (form != null)
    {
        form.Invoke(new Action(() => form.InitializeDeckImages())); // Refresh the deck images and labels
    }

    Form1 form = Application.OpenForms.OfType<Form1>().FirstOrDefault();
    if (form != null)
    {
        form.Invoke(new Action(() => form.InitializeDeckImages())); // Refresh the deck images and labels
    }
}


public static void Penalty(Message message)
{
    string playerGotPenalty = message.Data[0];
    if (playerGotPenalty == Program.player.Name)
    {
        MessageBox.Show("You got penalty for not pressing the UNO button!");
        ClientSocket.SendData(new Message(MessageType.DrawPenalty, new List<string> { Program.player.Name }));
        ClientSocket.SendData(new Message(MessageType.DrawPenalty, new List<string> { Program.player.Name }));
    }
}


public static void HandleChatMessage(Message message)
{
    string playerName = message.Data[0];
    string chatMessage = message.Data[1];
    //Hiển thị lên form1
    // VD vầy Form1.DisplayChatMessage(playerName, chatMessage);
}

public static void HandleEndMessage(Message message)
        public static void HandleEndMessage(Message message)
{
    string[] data = message.Data.ToArray();
    string winnerName = data[0];
    int PenaltyPoint = Instance.Players[0].Hand.Count * 10;
    if (winnerName == Program.player.Name)
    {
        WinResult winResult = new WinResult();
        winResult.Show();
    }
    else
    {
        ClientSocket.SendData(new Message(MessageType.Diem, new List<string> { Program.player.Name, PenaltyPoint.ToString() }));
        LoseResult loseResult = new LoseResult();
        loseResult.Show();
    }
}

public static void HandleResult(Message message)
{
    //Result;ID;Diem;Rank
    string playerId = message.Data[0];
    int points = int.Parse(message.Data[1]);
    int rank = int.Parse(message.Data[2]);
    Player player = Instance.Players.FirstOrDefault(p => p.Name == playerId);
    if (player != null)
    {
        player.Points = points;
        player.Rank = rank;
    }
    //Hiển thị bảng xếp hạng cho tất cả người chơi
    FinalRanking.DisplayRanking(Instance.Players);
}

public static void HandleDisconnect(Message message)
{
    var disconnectingPlayer = ClientSocket.gamemanager.Players.FirstOrDefault(p => p.Name == message.Data[0]);
    if (disconnectingPlayer != null)
    {
        ClientSocket.gamemanager.Players.Remove(disconnectingPlayer);
    }
}
    }
    public class Player
{
    public string Name { get; set; }
    public List<Card> Hand { get; set; }
    public int HandCount => Hand.Count; // Update this property to reflect the actual number of cards in the hand
    public bool IsTurn { get; set; }
    public int Points { get; set; }
    public int Rank { get; set; }

    public Player(string name)
    {
        Name = name;
        Hand = new List<Card>();
    }
}
public class Player
{
    public string Name { get; set; }
    public List<Card> Hand { get; set; }
    public int HandCount => Hand.Count; // Update this property to reflect the actual number of cards in the hand
    public bool IsTurn { get; set; }
    public int Points { get; set; }
    public int Rank { get; set; }

    public Player(string name)
    {
        Name = name;
        Hand = new List<Card>();
    }
}
}