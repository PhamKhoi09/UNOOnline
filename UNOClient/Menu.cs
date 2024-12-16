﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UnoOnline
{
    public partial class Menu : Form
    {
        public List<Player> players = new List<Player>();

        public Menu()
        {
            InitializeComponent();
            ClientSocket.OnMessageReceived += ClientSocket_OnMessageReceived;
        }

        private void ClientSocket_OnMessageReceived(string message)
        {
            if (this.IsHandleCreated)
            {
                // Update the UI with the received message
                this.Invoke(new Action(() =>
                {
                    // Display the message in a label or text box
                    richTextBox1.Text = message;
                }));
            }
            else
            {
                // Handle the case where the handle has not been created
                this.HandleCreated += (s, e) =>
                {
                    this.Invoke(new Action(() =>
                    {
                        // Display the message in a label or text box
                        richTextBox1.Text = message;
                    }));
                };
            }
        }


        private static void Connect()
        {
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000); //Tạo endpoint với IP của host và cổng

            ClientSocket.ConnectToServer(serverEP);

            var message = new Message(MessageType.CONNECT, new List<string> { Program.player.Name });
            ClientSocket.SendData(message);
        }

        private void BtnJoinGame_Click(object sender, EventArgs e)
        {
            Connect();
            WaitingLobby waitingLobby = new WaitingLobby();
            waitingLobby.Show();
            this.Hide();
        }

        private void Menu_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Dừng chạy chương trình khi form đóng, stop debuging
            //Environment.Exit(0);
            //Application.Exit();
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
            Application.Exit();
        }

        private void BtnRules_Click(object sender, EventArgs e)
        {
            //Sẽ tạm thời mở trang web chứa luật chơi-https://www.unorules.com/
            System.Diagnostics.Process.Start("https://www.unorules.com/");
        }
    }
}