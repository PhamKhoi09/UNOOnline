﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace UNOServer
{

    class Program
    {
        private static Socket ServerSocket; //Socket cho server (socket server)
        private static Socket ClientSocket; //Socket cho client (socket client)
        private static Thread ClientThread; //Thread để xử lý kết nối từ client khác
        private static List<PLAYER> PLAYERLIST = new List<PLAYER>(); //List các người chơi kết nối đến server với các thông tin của người chơi từ class PLAYER
        private static int HienTai = 1; //Đến lượt đánh của người chơi nào
        private static bool ChieuDanh = true; //Chiều đánh
        private static int RUT = 0; //Số bài rút (cho lá df, dt)
        private static List<string> YELLUNOLIST = new List<string>(); //List các id chỉ còn 1 lá mà chưa hô uno
        private static int DemRestart = 0; //Đếm số lượng đồng ý restart (màn hình kết quả thắng thua)
        private static int DemFinish = 0; //Đếm số lượng muốn finish (màn hình kết quả thắng thua)
        private static string WinnerName = ""; //Lưu tên người thắng
        private static bool TrangThai = false; //Trạng thái game: chưa bắt đầu/đã kết thúc ván game hoặc kết thúc hẳn (false), đang diễn ra (true)
        
        /* Hàm thiết lập (khởi động) server */
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8; //Sử dụng console để cập nhật thông tin (tiện theo dõi bên server)    
            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //Tạo socket cho server
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000); //Tạo endpoint với IP của host và cổng
            ServerSocket.Bind(serverEP); //Socket server kết nối đến endpoint đó => địa chỉ của server
            ServerSocket.Listen(4);
            Console.WriteLine("Server đã được tạo và đang chạy! Đợi các kết nối từ Clients...");
            //Lặp vô hạn để xử lý các kết nối đến server từ nhiều client
            while (true)
            {
                //Nếu server chưa full (4 người) thì cho phép thiết lập kết nối
                if (PLAYERLIST.Count < 4)
                {
                    ClientSocket = ServerSocket.Accept(); //Server chấp nhận kết nối từ 1 client nào đó và tạo socket client tương ứng
                    Console.WriteLine("Nhận kết nối từ " + ClientSocket.RemoteEndPoint);
                    ClientThread = new Thread(() => HandleNewPlayer(ClientSocket)); //Tạo thread mới để chạy hàm HandleNewPlayer để xử lý cho socket client tương ứng 
                    ClientThread.Start();
                }
                else //Nếu server đã full, tạo socket tạm thời để gửi thông báo đã đầy rồi đóng
                {                   
                    Socket tempSocket = ServerSocket.Accept(); //Tạo socket tạm thời để chấp nhận
                    string note = "Server đã đầy.";
                    byte[] data = Encoding.UTF8.GetBytes(note);
                    tempSocket.Send(data); //Gửi thông báo cho client
                    Console.WriteLine("Số lượng người chơi đã đạt tối đa (4)!");
                    tempSocket.Shutdown(SocketShutdown.Both);
                    tempSocket.Close(); //Đóng socket tạm thời
                }
            }
        }

        /* Hàm quản lý từng kết nối client và xử lý yêu cầu từng người chơi khác nhau */
        public static void HandleNewPlayer(Socket client)
        {
            PLAYER User = new PLAYER(); //Tạo đối tượng người chơi
            User.PlayerSocket = client; //Thông tin socket client được gán cho socket người chơi (PlayerSocket) là thuộc tính socket của người chơi trong class PLAYER 
            PLAYERLIST.Add(User); //Thêm người chơi đó vào list người chơi kết nối đến server
            byte[] data = new byte[1024]; //Tạo mảng byte tên data để chứa dữ liệu nhận được từ client
            //Vòng lặp kiểm tra kết nối và xử lý dữ liêu từ client 
            while (User.PlayerSocket.Connected)
            {
                try
                {
                    if (User.PlayerSocket.Available > 0) //Nếu có dữ liệu đến từ client thì server sẽ bắt đầu nhận
                    {
                        string receivedata = ""; //Tạo chuỗi chứa thông điệp (dữ liệu từ client gửi đến)
                        while (User.PlayerSocket.Available > 0)
                        {
                            int bRead = User.PlayerSocket.Receive(data); //Nhận dữ liệu client và ghi từng byte dữ liệu vào mảng byte tên data, số byte lưu vào bRead
                            receivedata += Encoding.UTF8.GetString(data, 0, bRead); //Chuyển đổi mảng byte dữ liệu thành dạng chuỗi và nối chuỗi vào receivedata thành thông điệp
                        }
                        Console.WriteLine(User.PlayerSocket.RemoteEndPoint + ": " + receivedata);
                        DecryptingMessage(receivedata, User); //Thông điệp được đưa vào hàm này để xử lý yêu cầu (thông điệp) từ người chơi (client) tương ứng
                    }
                }
                catch //Xử lý việc người chơi mất kết nối ko gửi DISCONNECT
                {
                    //Nếu chỉ có hoặc còn 2 người chơi trong game đang diễn ra mà có người mất kết nối thì đóng kết nối với người còn lại luôn
                    if (PLAYERLIST.Count == 2 && TrangThai == true)
                    {
                        string ID = User.ID;
                        foreach (var user in PLAYERLIST.ToList()) //Duyệt qua các người chơi trong PLAYERLIST
                        {
                            if (user.ID != ID) //Nếu ID trùng với ID của người chơi không là người muốn ngắt kết nối
                            {
                                string SendData = "NotEnoughPlayers;" + user.ID;
                                byte[] senddata = Encoding.UTF8.GetBytes(SendData);
                                user.PlayerSocket.Send(senddata);
                                Console.WriteLine("Quá ít người chơi, không thể bắt đầu");
                            }
                            user.PlayerSocket.Shutdown(SocketShutdown.Both); //Ngắt kết nối hai chiều
                            user.PlayerSocket.Close(); //Đóng socket của người chơi
                            PLAYERLIST.Remove(user);
                        }
                        TrangThai = false;
                    }
                    else 
                    { 
                    User.PlayerSocket.Shutdown(SocketShutdown.Both); //Ngắt kết nối hai chiều
                    User.PlayerSocket.Close(); //Đóng socket của người chơi
                    if (PLAYERLIST[HienTai - 1].ID == User.ID && TrangThai == true) //Nếu game đang diễn ra và người đang đến lượt lại mất, update lượt cho người khác
                    {
                        if (ChieuDanh == true)
                            HienTai--;
                        PLAYERLIST.Remove(User); //Xóa người chơi khỏi danh sách PLAYERLIST
                        UpdateTurn();
                    }
                    else if (TrangThai == true) //Nếu game đang diễn ra và người đang không đến lượt lại mất, update lượt cho người khác
                    {
                        if (User.Luot < PLAYERLIST[HienTai - 1].Luot)
                            HienTai--;
                        PLAYERLIST.Remove(User); //Xóa người chơi khỏi danh sách PLAYERLIST
                        UpdateTurn();
                    }
                    else PLAYERLIST.Remove(User); //Xóa người chơi khỏi danh sách PLAYERLIST các trường hợp còn lại
                    }
                }
            }
        }

        /* Hàm xử lý yêu cầu (thông điệp) của người chơi (client) tương ứng */
        public static void DecryptingMessage(string receivedata, PLAYER User)
        {
            //Tạo mảng chuỗi Signal với mỗi phần tử chứa từng phần trong tham số chuỗi receivedata chứa thông điệp
            //Mỗi phần trong thông điệp được phân biệt bởi dấu ; và được lưu lần lượt vào từng phần tử
            //Ví dụ thông điệp là "CONNECT;User1;..." thì lưu vào mảng sẽ là ["CONNECT", "User1", ...]
            string[] Signal = receivedata.Split(';');
            switch (Signal[0]) //Xét phần tử đầu tiên trong mảng Signal chứa loại thông điệp (phần đầu tiên trong thông điệp) được gửi từ client
            {
                case "CONNECT":
                    HandleConnect(Signal, User);
                    break;
                case "DISCONNECT":
                    HandleDisconnect(Signal);
                    break;
                case "START":
                    SetupGame(Signal, User);
                    break;
                case "DanhBai":
                    DanhBai(Signal, User);
                    break;
                case "RutBai":
                    RutBai(Signal, User);
                    break;
                case "SpecialCardEffect":
                    HandleSpecialDraw(Signal, User);
                    break;
                case "YellUNO":
                    YELLUNOLIST.Remove(Signal[1]);
                    break;
                case "DrawPenalty":
                    HandleSpecialDraw(Signal, User);
                    break;
                case "Diem":
                    HandleDiem(Signal, User);
                    break;
                case "Chat":
                    HandleChat(Signal, User);
                    break;
                case "Restart":
                    HandleAfterMatch(Signal, User);
                    break;
                case "Finish":
                    HandleAfterMatch(Signal, User);
                    break;
                default:
                    break;
            }
        }

        /*                                                          Cấu trúc thông điệp giữa Server và Client
         *                  Client -> Server                                          |                                     Server -> Client
         * CONNECT;ID                                                                 | Info;ID         
         * DISCONNNECT;ID                                                             | InitializeStat;ID;Luot;SoLuongBai;CardName;CardName...;CardName (7 bài người chơi + 1 bài hệ thống tự đánh)              
         * START;ID                                                                   | OtherPlayerStat;ID;Luot;SoLuongBai
         * DanhBai;ID;SoLuongBai;CardName;color(wild draw, wild)                      | Boot;ID                                   
         * RutBai;ID;SoLuongBai                                                       | Update;ID;SoluongBai;CardName(Nếu đánh bài);color(wild draw, wild) (Nếu đánh bài)          
         * SpecialCardEffect;ID;SoLuongBai;                                           | Turn;ID                      
         * Chat;ID;<Content>                                                          | CardDraw;ID;CardName                
         * YellUNO;ID                                                                 | Specialdraws;ID;CardName;CardName...
         * DrawPenalty;ID;SoLuongBai;                                                 | End;ID
         * Diem;ID;<Diem so>                                                          | MESSAGE;ID;<Content>
         * Restart;ID                                                                 | YellUNOEnable;ID
         * Finish;ID                                                                  | Penalty;ID
         *                                                                            | Result;ID;Diem;Rank
         *                                                                            | NotEnoughPlayers;ID
         * LƯU Ý: 
         * Bên client sẽ tự động disable nút hô UNO sau khi người chơi ấn nút hô UNO hoặc khi lại đến lượt người chơi đó quên ấn.
         * Bên client sẽ xử lý logic việc show những lá bài có thể đánh hoặc không trong bộ bài dựa trên thông điệp Update lá bài face up card bên server gửi đến (cùng màu, cùng số, đặc biệt trường hợp face up card là lá df, wd phải dựa trên màu người chơi đánh lá đó đã chọn).
         * Về restart/finish game: bên client sau khi hiện màn hình thắng thua thì có 2 nút restart/finish và gửi thông điệp tương ứng, server khi nào nhận đủ thông điệp từ tất cả người chơi thì mới xử lý và quyết định restart hay hiện màn hình xếp hạng kết thúc.
         * Client ấn nút restart/finish xong thì disable tất cả các nút đi tránh gửi nhiều lần. 
        */

        /* Hàm khởi tạo lượt và gán số bài ban đầu cho người chơi */
        public static void SettingUpTurn()
        {
            int[] turns = new int[PLAYERLIST.Count]; //Tạo mảng int tên turns với kích thước là số lượng người chơi kết nối đến server trong list 
            for (int i = 1; i <= PLAYERLIST.Count; i++)
            {
                turns[i - 1] = i; //Gán vào từng phần tử mảng turns các số từ 1 đến số lượng người chơi trong list để lưu thứ tự chơi
            }
            Random rd = new Random(); //Tạo đối tượng rd thuộc lớp Random 
            //Trộn thứ tự chơi ngẫu nhiên cho các người chơi
            foreach (var user in PLAYERLIST)
            {
                int pick = rd.Next(turns.Length); //Tạo biến int tên pick để lưu thứ tự chơi được rd random từ mảng turns
                user.Luot = turns[pick]; //Gán thứ tự chơi đó cho thuộc tính Luot của người chơi trong class PLAYER
                turns = turns.Where(val => val != turns[pick]).ToArray(); //Xóa thứ tự chơi đã được gán ra khỏi mảng turns để các người chơi tiếp theo không bị chọn trùng thứ tự
                user.SoLuongBai = 7; //Gán số lượng bài lúc bắt đầu game người chơi là 7
            }
        }

        /* Hàm xào bộ bài */
        public static void XaoBai()
        {
            Random rd = new Random(); //Tạo đối tượng rd thuộc lớp Random
            BOBAI.CardName = BOBAI.CardName.OrderBy(x => rd.Next()).ToArray(); //Sắp xếp tên các lá bài trong mảng CardName 1 cách ngẫu nhiên do rd random là thuộc tính của lớp BOBAI
        }

        /* Hàm tạo bài ban đầu cho người chơi */
        public static string CreatePlayerCards()
        {
            Random rd = new Random(); //Tạo đối tượng rd thuộc lớp Random
            string playercards = ""; //Tạo chuỗi playercards
            //Lấy 7 lá bài
            for (int i = 0; i < 7; i++)
            {
                int pick = rd.Next(BOBAI.CardName.Length); //Tạo biến int tên pick để lưu chỉ số trong mảng CardName ngẫu nhiên do rd random
                playercards += BOBAI.CardName[pick] + ";"; //Thêm lá bài vào chuỗi playercards
                BOBAI.CardName = BOBAI.CardName.Where(val => val != BOBAI.CardName[pick]).ToArray(); //Xóa lá bài đã được chọn ra khỏi mảng CardName để các lá sau không bị chọn trùng
            }
            return playercards; //Trả về chuỗi playercards chứa 7 lá bài được ghép lại với nhau, mỗi lá cách nhau bởi dấu chấm phẩy ;
        }

        /* Hàm hệ thống tự đánh (mở) lá bài đầu tiên */
        public static string ShowPileCard()
        {
            string temp = ""; //Tạo chuỗi temp để lưu lá bài 
            //Duyệt qua tất cả các lá bài chỉ lựa các lá bài số để đánh đầu tiên
            for (int i = 0; i < BOBAI.CardName.Length; i++)
            {
                temp = BOBAI.CardName[i]; //Lấy lá bài của mảng CardName và lưu vào chuỗi temp
                //Nếu thỏa điều kiện chỉ là lá bài số thì break khỏi vòng lặp
                if (!temp.Contains("Reverse") && !temp.Contains("Skip") && !temp.Contains("Wild") && !temp.Contains("Wild_Draw") && !temp.Contains("Draw")) //Sử dụng Contains() để xác định trong lá bài có phần cần tìm hay không
                //Lưu ý là contains() sử dụng với chuỗi không yêu cầu chuỗi gốc phải khớp hoàn toàn nên đáng lý ra là chỉ cần contains Wild và Draw nhưng t trình bày hết cho dễ hiểu
                    break;
            }
            BOBAI.CardName = BOBAI.CardName.Where(val => val != temp).ToArray(); //Xóa lá bài đã được chọn ra khỏi mảng CardName để không bị sử dụng lại
            MoBai.mobai.Add(temp); //Lưu lá bài đã đánh vào list MoBai lưu trữ các lá bài đã đánh
            return temp; //Trả về chuỗi temp chứa lá bài đã lật
        }

        /* Hàm gửi thông tin của tất cả người chơi đã kết nối cho người chơi mới và ngược lại */
        private static void HandleConnect(string[] Signal, PLAYER User)
        {
            User.ID = Signal[1]; // Thiết lập ID (danh tính) của người chơi từ dữ liệu đã nhận
            //Gửi thông tin của những người chơi khác đến người chơi mới
            foreach (var user in PLAYERLIST)
            {
                byte[] data = Encoding.UTF8.GetBytes("Info;" + user.ID); //Tạo mảng byte tên data để chứa thông điệp theo cấu trúc
                User.PlayerSocket.Send(data); // Gửi data chứa thông điệp mang ID của mỗi người chơi trong PLAYERLIST đến người chơi mới
                Thread.Sleep(210);
            }
            //Gửi thông tin của người chơi mới đến những người chơi khác
            foreach (var user in PLAYERLIST)
            {
                if (user.PlayerSocket != User.PlayerSocket)
                {
                    byte[] data = Encoding.UTF8.GetBytes("Info;" + User.ID); //Tạo mảng byte tên data để chứa thông điệp theo cấu trúc
                    user.PlayerSocket.Send(data); // Gửi data chứa thông điệp mang ID của người chơi mới đến các người chơi khác
                    Thread.Sleep(210);
                }
            }
        }

        /* Hàm xử lý yêu cầu ngắt kết nối từ một người chơi (hàm này còn sử dụng cho việc ấn nút thoát ở màn hình xếp hạng) */
        private static void HandleDisconnect(string[] Signal)
        {
            //Nếu chỉ có hoặc còn 2 người chơi trong game đang diễn ra mà lại có người disconnect thì đóng kết nối với người còn lại luôn
            if (PLAYERLIST.Count == 2 && TrangThai == true)
            {
                foreach (var user in PLAYERLIST.ToList()) //Duyệt qua các người chơi trong PLAYERLIST
                {
                    if (user.ID != Signal[1]) //Nếu ID trùng với ID của người chơi không là người muốn ngắt kết nối
                    {
                        string SendData = "NotEnoughPlayers;" + Signal[1];
                        byte[] data = Encoding.UTF8.GetBytes(SendData);
                        user.PlayerSocket.Send(data);
                        Console.WriteLine("Quá ít người chơi, không thể bắt đầu");
                    }
                    user.PlayerSocket.Shutdown(SocketShutdown.Both); //Ngắt kết nối hai chiều
                    user.PlayerSocket.Close(); //Đóng socket của người chơi
                    PLAYERLIST.Remove(user);
                }
                TrangThai = false;
            }
            else
            {
                foreach (var user in PLAYERLIST.ToList()) //Duyệt qua các người chơi trong PLAYERLIST
                {
                    if (user.ID == Signal[1]) //Nếu ID trùng với ID của người chơi muốn ngắt kết nối
                    {
                        user.PlayerSocket.Shutdown(SocketShutdown.Both); //Ngắt kết nối hai chiều
                        user.PlayerSocket.Close(); //Đóng socket của người chơi                  
                        if (PLAYERLIST[HienTai - 1].ID == user.ID && TrangThai == true) //Nếu game đang diễn ra và người đang đến lượt lại disconnect, update lượt cho người khác
                        {
                            if (ChieuDanh == true)
                                HienTai--;
                            PLAYERLIST.Remove(user); //Xóa người chơi khỏi danh sách PLAYERLIST
                            UpdateTurn();
                        }
                        else if (TrangThai == true) //Nếu game đang diễn ra và người đang không đến lượt lại disconnect, update lượt cho người khác
                        {
                            if (user.Luot < PLAYERLIST[HienTai - 1].Luot)
                                HienTai--;
                            PLAYERLIST.Remove(user); //Xóa người chơi khỏi danh sách PLAYERLIST
                            UpdateTurn();
                        }
                        else PLAYERLIST.Remove(user); //Xóa người chơi khỏi danh sách PLAYERLIST các trường hợp còn lại
                    }
                }
            }
        }

        /* Hàm thiết lập bắt đầu trò chơi */
        private static void SetupGame(string[] Signal, PLAYER User)
        {
            //Nếu bắt đầu game khi không đủ 2 người trở lên, gửi thông điệp ko thể bắt đầu game
            if (PLAYERLIST.Count < 2)
            {
                string SendData = "NotEnoughPlayers;" + Signal[1];
                byte[] data = Encoding.UTF8.GetBytes(SendData);
                User.PlayerSocket.Send(data);
                Console.WriteLine("Quá ít người chơi, không thể bắt đầu");
                return;
            }    
            TrangThai = true;
            SettingUpTurn(); //Tạo lượt và gán số bài 7 bài cho mỗi người chơi
            PLAYERLIST.Sort((x, y) => x.Luot.CompareTo(y.Luot)); //Sắp xếp lại các người chơi trong PLAYERLIST theo lượt tăng dần
            XaoBai(); //Xào bộ bài
            BOBAI.currentCard = ShowPileCard(); //Tự động rút lá bài đầu tiên và cập nhật lá bài hiện tại đã đánh
            //Gửi thông điệp cho tất cả người chơi InitializeStat: Gửi thông điệp thông tin khởi tạo về danh tính, thứ tự lượt, số bài, tên các lá cụ thể cho mỗi người chơi lúc ban đầu 
            foreach (var user in PLAYERLIST)
            {
                string SendData = "InitializeStat;" + user.ID + ";" + user.Luot + ";" + user.SoLuongBai + ";" + CreatePlayerCards() + BOBAI.currentCard;
                byte[] data = Encoding.UTF8.GetBytes(SendData);
                user.PlayerSocket.Send(data);
                Thread.Sleep(200);
            }
            //Gửi thông điệp OtherPlayerStat: Gửi thông điệp chứa thông tin khởi tạo về danh tính, thứ tự lượt, số bài, những người chơi khác cho mỗi người chơi 
            //Ví dụ t là người chơi thì OtherPlayerStat này sẽ gửi thông tin những người chơi còn lại cho bên t để game cập nhật giao diên...và mỗi người chơi khác cũng thế
            foreach (var user in PLAYERLIST)
            {
                foreach (var player_ in PLAYERLIST)
                {
                    if (user.ID != player_.ID)
                    {
                        string SendData = "OtherPlayerStat;" + player_.ID + ";" + player_.Luot + ";" + player_.SoLuongBai;
                        byte[] data = Encoding.UTF8.GetBytes(SendData);
                        user.PlayerSocket.Send(data);
                        Thread.Sleep(200);
                    }
                }
            }
            //Gửi thông điệp cho tất cả người chơi Boot: Gửi thông điệp yêu cầu mở màn hình game
            foreach (var user in PLAYERLIST)
            {
                string SendData = "Boot;" + user.ID;
                byte[] data = Encoding.UTF8.GetBytes(SendData);
                user.PlayerSocket.Send(data);
                Thread.Sleep(200);
            }

            //Gửi thông điệp cho tất cả người chơi Turn: Gửi thông điệp về việc đến lượt của người chơi nào (bắt đầu game)
            foreach (var user in PLAYERLIST)
            {
                string SendData = "Turn;" + PLAYERLIST[HienTai - 1].ID;
                byte[] buffer = Encoding.UTF8.GetBytes(SendData);
                user.PlayerSocket.Send(buffer);
                Thread.Sleep(200);
            }
        }

        /* Hàm xử lý việc sau khi đánh 1 lá bài và chuyển lượt */
        private static void DanhBai(string[] Signal, PLAYER User)
        {
            BOBAI.currentCard = Signal[3]; //Cập nhật lá bài hiện tại
            MoBai.mobai.Add(Signal[3]); //Thêm lá bài đã đánh vào bài đã mở 
            PLAYERLIST[HienTai - 1].SoLuongBai = int.Parse(Signal[2]); //Lấy số lượng bài còn lại của người chơi sau khi đánh đó
            if (PLAYERLIST[HienTai - 1].SoLuongBai == 0) //Kiểm tra nếu số lượng bài còn lại của người chơi sau khi đánh đó là 0
            {
                TrangThai = false;
                WinnerName = PLAYERLIST[HienTai - 1].ID; //Lưu tên người thắng
                //Gửi thông điệp cho tất cả người chơi End: kết thúc game và bật màn hình kết quả thắng thua, người thắng (Signal[1]) sẽ mở màn hình thắng, còn lại màn hình thua
                foreach (var user in PLAYERLIST)
                {
                    string SendData = "End;" + Signal[1];
                    byte[] data = Encoding.UTF8.GetBytes(SendData);
                    user.PlayerSocket.Send(data);
                    Thread.Sleep(200);
                }
            }
            else
            {
                //Gửi thông điệp cho phép enable nút hô Uno bên client đó.
                //Sẽ theo cơ chế nếu người đó quên ấn nút hô Uno kể từ khi hết lượt của người đó còn 1 lá cho đến khi lại đến lượt của mình thì sẽ bị phạt rút thêm 2 lá (có thông báo bị phạt gì đó) và chuyển lượt.
                if (PLAYERLIST[HienTai - 1].SoLuongBai == 1)
                {
                    YELLUNOLIST.Add(Signal[1]);
                    string SendData ="YellUNOEnable;" + Signal[1];
                    byte[] data = Encoding.UTF8.GetBytes(SendData);
                    PLAYERLIST[HienTai - 1].PlayerSocket.Send(data);
                }
                //Gửi thông điệp Update: Cập nhật lá bài mới đánh ra và số lượng bài còn lại của người chơi đó cho toàn bộ người chơi 
                foreach (var user in PLAYERLIST)
                {
                    string SendData = "Update;" + Signal[1] + ";" + Signal[2] + ";" + Signal[3];
                    if (Signal[3].Contains("Wild_Draw") || Signal[3].Contains("Wild")) //Đáng lý là chỉ cần contains Wild nhưng t trình bày hết cho dễ hiểu trường hợp này do chỉ có 2 lá đó là có màu được chọn đi kèm
                    {
                        SendData += ";" + Signal[4];
                    }
                    byte[] data = Encoding.UTF8.GetBytes(SendData);
                    user.PlayerSocket.Send(data);
                    Thread.Sleep(200);
                }
                if (Signal[3].Contains("Blue_Draw") || Signal[3].Contains("Red_Draw") || Signal[3].Contains("Green_Draw") || Signal[3].Contains("Yellow_Draw")) //Nếu lá bài người chơi đánh là draw 2 (sử dụng Contains() xác nhận trong lá bài có phần cần tìm tương ứng)
                    RUT += 2;
                if (Signal[3].Contains("Wild_Draw")) //Nếu lá bài người chơi đánh là draw 4 (sử dụng Contains() xác nhận trong lá bài có phần cần tìm tương ứng)
                {
                    RUT += 4;
                }
                if (Signal[3].Contains("Reverse")) //Nếu lá bài người chơi đánh là reverse (sử dụng Contains() xác nhận trong lá bài có phần cần tìm tương ứng)
                {
                    if (ChieuDanh == true) //Đang thuận chiều thì ngược chiều và ngược lại
                        ChieuDanh = false;
                    else
                        ChieuDanh = true;
                }
                if (ChieuDanh == true) //Nếu thuận chiều 
                {
                    if (Signal[3].Contains("Skip")) //Nếu lá bài người chơi đánh là skip
                    {
                        if (HienTai == PLAYERLIST.Count) //Nếu HienTai là người chơi có thứ tự lượt cuối cùng trong PLAYERLIST đã sắp xếp thứ tự theo lượt chơi
                        {
                            HienTai = 2; //HienTai sẽ là người chơi có thứ tự 2 trong PLAYERLIST
                        }
                        else
                        {
                            HienTai = HienTai + 2; //HienTai sẽ là người chơi kế người chơi ở lượt tiếp theo
                        }
                    }
                    else
                    {
                        HienTai++; //HienTai sẽ là người chơi ở lượt tiếp theo như bth
                    }
                }
                else //Nếu ngược chiều
                {
                    if (Signal[3].Contains("Skip")) //Nếu lá bài người chơi đánh là skip
                    {
                        if (HienTai == 1) //Nếu HienTai là người chơi có thứ tự lượt đầu tiên trong PLAYERLIST đã sắp xếp thứ tự theo lượt chơi
                        {
                            HienTai = PLAYERLIST.Count - 1; //HienTai sẽ là người chơi có thứ tự cuối cùng trong PLAYERLIST 
                        }
                        else
                        {
                            HienTai = HienTai - 2; //HienTai sẽ là người chơi kế người chơi ở lượt tiếp theo
                        }
                    }
                    else
                    {
                        HienTai--; //HienTai sẽ là người chơi ở lượt tiếp theo như bth
                    }
                }
                if (HienTai > PLAYERLIST.Count) //Nếu HienTai sau khi tính toán qua các điều kiện trên vượt quá số người trong PLAYERLIST thì đến lượt người chơi đầu tiên trong PLAYERLIST
                    HienTai = 1;

                if (HienTai < 1) //Nếu HienTai sau khi tính toán qua các điều kiện trên nhỏ số người trong PLAYERLIST thì đến lượt người chơi đầu tiên trong PLAYERLIST
                    HienTai = PLAYERLIST.Count;
                //Nếu người chơi lượt kế tiếp là người còn 1 lá bài nhưng vẫn chưa hô Uno, gửi thông điệp xử phạt người chơi đó
                if (YELLUNOLIST.Contains(PLAYERLIST[HienTai - 1].ID))
                {
                    string SendData = "Penalty;" + PLAYERLIST[HienTai - 1].ID;
                    byte[] data = Encoding.UTF8.GetBytes(SendData);
                    PLAYERLIST[HienTai - 1].PlayerSocket.Send(data);
                }
                //Gửi thông điệp cho tất cả người chơi Turn: Gửi thông điệp về việc đến lượt của người chơi nào
                foreach (var user in PLAYERLIST)
                {
                    string SendData_ = "Turn;" + PLAYERLIST[HienTai - 1].ID;
                    byte[] buffer_ = Encoding.UTF8.GetBytes(SendData_);
                    user.PlayerSocket.Send(buffer_);
                    Thread.Sleep(200);
                }
            }
        }

        /* Hàm xử lý mỗi lần người chơi rút 1 bài và chuyển lượt */
        private static void RutBai(string[] Signal, PLAYER User)
        {
            PLAYERLIST[HienTai - 1].SoLuongBai = int.Parse(Signal[2]); //Lấy thông tin về số bài còn lại của người chơi hiện tại 
            string mkmsg = "CardDraw;" + Signal[1] + ";" + BOBAI.CardName[0]; //Tạo chuỗi mkmsg chứa thông điệp CardDraw: bài mà người chơi rút được
            BOBAI.CardName = BOBAI.CardName.Where(val => val != BOBAI.CardName[0]).ToArray(); //Xóa lá bài đã rút ra khỏi mảng CardName
            byte[] bf = Encoding.UTF8.GetBytes(mkmsg); //Tạo mảng byte tên bf chứa thông điệp CardDraw
            PLAYERLIST[HienTai - 1].PlayerSocket.Send(bf); //Gửi thông điệp CardDraw đến người chơi rút bài
            Console.WriteLine("Note: " + mkmsg);
            //Gửi thông điệp Update: cập nhật số lượng bài mới của người chơi đó cho toàn bộ người chơi
            foreach (var user in PLAYERLIST)
            {
                string SendData = "Update;" + Signal[1] + ";" + Signal[2];
                byte[] data = Encoding.UTF8.GetBytes(SendData);
                user.PlayerSocket.Send(data);
                Thread.Sleep(200);
            }
            UpdateTurn();
        }

        /* Hàm xử lý việc bị rút nhiều lá bài do các lá bài đặc biệt hoặc bị phạt do không hô uno và chuyển lượt */
        private static void HandleSpecialDraw(string[] Signal, PLAYER User)
        {
            PLAYERLIST[HienTai - 1].SoLuongBai = int.Parse(Signal[2]); //Lấy thông tin về số bài còn lại của người chơi hiện tại
            string cardstack = "Specialdraws;" + PLAYERLIST[HienTai - 1].ID + ";"; //Tạo chuỗi cardstack chứa thông điệp Specialdraws: Các lá bài mà người chơi nhận được
            //Phạt người chơi không hô UNO rút thêm 2 lá
            if (YELLUNOLIST.Contains(PLAYERLIST[HienTai - 1].ID))
            {
                RUT += 2;
                YELLUNOLIST.Remove(PLAYERLIST[HienTai - 1].ID);
            }
            //Vòng lặp nối các lá bài vào cardstack để hoàn chỉnh SpecialDraw 
            for (int i = 0; i < RUT; i++)
            {
                cardstack += BOBAI.CardName[0] + ";";
                BOBAI.CardName = BOBAI.CardName.Where(val => val != BOBAI.CardName[0]).ToArray();
            }
            byte[] buff = Encoding.UTF8.GetBytes(cardstack);
            PLAYERLIST[HienTai - 1].PlayerSocket.Send(buff); //Gửi thông điệp SpecialDraw đến người chơi rút bài
            RUT = 0;
            Console.WriteLine("Note: " + cardstack);
            //Gửi thông điệp Update: cập nhật số lượng bài mới của người chơi đó cho toàn bộ người chơi
            foreach (var user in PLAYERLIST)
            {
                string SendData = "Update;" + Signal[1] + ";" + Signal[2];
                byte[] data = Encoding.UTF8.GetBytes(SendData);
                user.PlayerSocket.Send(data);
                Thread.Sleep(200);
            }
            UpdateTurn();
        }

        /* Hàm xử lý tin nhắn chat */
        private static void HandleChat(string[] Signal, PLAYER User)
        {
            string sender = Signal[1]; //Tạo chuỗi sender để lưu ID của người gửi tin chat 
            string ChatContent = Signal[2]; //Tạo chuỗi MessContent để lưu nội dung tin chat trong mảng chuỗi Signal
            //Gửi thông điệp MESSAGE: Tin chat của người chơi gửi chat đến tất cả người chơi còn lại
            foreach (var user in PLAYERLIST)
            {
                if (user.PlayerSocket != User.PlayerSocket)
                {
                    string MessToSend = $"MESSAGE;{sender};{ChatContent}";
                    byte[] buffer = Encoding.UTF8.GetBytes(MessToSend);
                    user.PlayerSocket.Send(buffer);
                }
            }
            Console.WriteLine($"{sender}: {ChatContent}");
        }

        /* Hàm cập nhật lượt kế tiếp */
        private static void UpdateTurn()
        {
            if (ChieuDanh == true) //Điều kiện để đổi chiều đánh
            {
                HienTai++;
            }
            else
            {
                HienTai--;
            }
            if (HienTai > PLAYERLIST.Count) // Nếu HienTai vượt quá số người chơi
                HienTai = 1; // Quay lại người chơi đầu tiên
            if (HienTai < 1) // Nếu HienTai giảm xuống dưới 1
                HienTai = PLAYERLIST.Count; // Quay về người chơi cuối cùng
            //Nếu người chơi lượt kế tiếp là người còn 1 lá bài nhưng vẫn chưa hô Uno, gửi thông điệp xử phạt người chơi đó
            if (YELLUNOLIST.Contains(PLAYERLIST[HienTai - 1].ID))
            {
                string SendData = "Penalty;" + PLAYERLIST[HienTai - 1].ID;
                byte[] data = Encoding.UTF8.GetBytes(SendData);
                PLAYERLIST[HienTai - 1].PlayerSocket.Send(data);
            }                 
            //Gửi thông điệp đến tất cả người chơi Turn: Gửi thông điệp về việc đến lượt của người chơi nào
            foreach (var user in PLAYERLIST)
            {
                string SendData = "Turn;" + PLAYERLIST[HienTai - 1].ID; // Tạo thông điệp chứa ID của người chơi hiện tại
                byte[] buffer = Encoding.UTF8.GetBytes(SendData);
                user.PlayerSocket.Send(buffer); // Gửi dữ liệu đến từng client
                Thread.Sleep(200); // Ngắt thời gian ngắn giữa các lần gửi
            }
        }

        /* Hàm cập nhật điểm của người thắng sau khi hết ván */
        private static void HandleDiem(string[] Signal, PLAYER User)
        {
            for (int i = 0; i < PLAYERLIST.Count; i++)
            {
                if (PLAYERLIST[i].ID == WinnerName)
                {
                    PLAYERLIST[i].Diem += int.Parse(Signal[2]);
                    Console.WriteLine("Điểm của người thắng " + WinnerName + ": " + PLAYERLIST[i].Diem);
                    break;
                }
            }
        }

        /* Hàm xử lý restart hoặc finish game */
        private static void HandleAfterMatch(string[] Signal, PLAYER User)
        {
            //Đếm Restart hoặc Finish 
            for (int i = 0; i < PLAYERLIST.Count; i++)
            {
                if (PLAYERLIST[i].ID == Signal[1])
                {
                    if (Signal[0] == "Restart")
                    {
                        DemRestart += 1;
                        Console.WriteLine(Signal[1] + " đã chọn Restart");
                        break;
                    }                       
                    if (Signal[0] == "Finish")
                    {
                        DemFinish += 1;
                        Console.WriteLine(Signal[1] + " đã chọn Finish");
                        break;
                    }                          
                }
            }
            //Nếu đủ số người đã chọn thì quyết định restart hay hiện màn hình xếp hạng kết thúc.
            if ((DemRestart + DemFinish) == PLAYERLIST.Count)
            {
                if (DemFinish > 0) //Có người chọn Finish thì gưi thông điệp Result: hiện màn hình xếp hạng kết thúc và điểm số.
                {
                    PLAYERLIST.Sort((x, y) => x.Diem.CompareTo(y.Diem)); //Sắp xếp lại PLAYERLIST theo điểm tăng dần
                    for (int i = 0; i < PLAYERLIST.Count; i++)
                        PLAYERLIST[i].Rank = PLAYERLIST.Count - i; //Gán hạng của người chơi (4-1)
                    //Gửi thông điệp result chứa thông tin từng người chơi cho từng người chơi tương ứng
                    foreach (var user in PLAYERLIST)
                    {
                        string SendData = "Result;" + user.ID + ";" + user.Diem + ";" + user.Rank;
                        byte[] data = Encoding.UTF8.GetBytes(SendData);
                        user.PlayerSocket.Send(data);
                        Thread.Sleep(200);
                    }
                    //Gửi thông điệp result chứa thông tin những người chơi khác cho mỗi người chơi (để thuận lợi tạo bảng xếp hạng bên client)
                    //Ví dụ t là người chơi thì sẽ gửi thông tin result của những người còn lại cho t để bên client của t cập nhập bảng xếp hạng.
                    foreach (var user in PLAYERLIST)
                    {
                        foreach (var player_ in PLAYERLIST)
                        {
                            if (user.ID != player_.ID)
                            {
                                string SendData = "Result;" + player_.ID + ";" + player_.Diem + ";" + player_.Rank;
                                byte[] data = Encoding.UTF8.GetBytes(SendData);
                                user.PlayerSocket.Send(data);
                                Thread.Sleep(200);
                            }
                        }
                    }
                    Console.WriteLine("Game đã kết thúc hoàn toàn! Bye bye.");
                }
                else
                {
                    HienTai = 1;
                    ChieuDanh = true;
                    RUT = 0;
                    YELLUNOLIST.Clear();
                    DemFinish = 0;
                    DemRestart = 0;
                    WinnerName = "";
                    SetupGame(Signal, User);  //Đủ người thì lại thiết lập bắt đầu trò chơi
                    Console.WriteLine("Đủ người chơi muốn restart, bắt đầu lại...");
                }
            }
        }
    }
}
