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
        public bool IsOver { get; set; }
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
                if (color =="Wild" && value != "Draw")
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

            Player player = new Player(playerName);
            player.HandCount = cardCount;

            Instance.AddPlayer(player);
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

        public void PlayCard(Player player, Card card)
        {
        }

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
                    player.HandCount = remainingCards;
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


        public static void HandleTurnMessage(Message message)
        {
            try
            {
                string playerId = message.Data[0];
                MessageBox.Show($"Handling Turn message for player: {playerId}");

                if (playerId == Program.player.Name)
                {
                    MessageBox.Show("It's the current player's turn.");

                    if (Instance.CurrentCard.CardName.Contains("Draw"))
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
                    form1.DisplayPlayerHand(player.Hand);
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
        }
        public static void Penalty(Message message)
        {
            string playerGotPenalty = message.Data[0];
            if (playerGotPenalty == Program.player.Name)
            {
                MessageBox.Show("You got penalty for not pressing the UNO button!");
                ClientSocket.SendData(new Message(MessageType.DrawPenalty, new List<string> { Program.player.Name}));
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
}