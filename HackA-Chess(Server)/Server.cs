using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Concurrent;
using HackA_Chess_Server_;
using Microsoft.Data.SqlClient;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace HackA_Chess_Server_
{
    public partial class Server : Form
    {
        private TcpListener listener;
        private bool isRunning = false;
        public Server()
        {
            InitializeComponent();
        }
        #region Các hàm Login, Register
        static bool CheckLogin(string username, string hashedPassword)
        {
            using (SqlConnection conn = Connection.GetSqlConnection())
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM UserDB WHERE Username=@user AND PasswordHash=@pass";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@user", username);
                    cmd.Parameters.AddWithValue("@pass", hashedPassword); // so sánh hash
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        private bool UsernameExist(string username) //có rồi thì trả về 1 chưa có thì trả về 0
        {
            try
            {
                using (SqlConnection conn = Connection.GetSqlConnection())
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM UserDB WHERE Username = @user";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user", username);
                        int count = (int)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                AppendText("Lỗi kiểm tra UsernameExist: " + ex.Message);
                return true; // giả định tồn tại để tránh thêm trùng khi lỗi
            }
            return false;
        }

        private void AddUsertoDatabase(string username, string password, string email, string fullname, string sdt, int elo = 1200, int totalwin = 0, int totaldraw = 0, int totalloss = 0, string AVATAR = "")
        {
            try
            {
                using (SqlConnection conn = Connection.GetSqlConnection())
                {
                    conn.Open();
                    string query = "INSERT INTO UserDB (USERNAME, PASSWORDHASH, EMAIL, PHONE, FULLNAME, ELO, TOTALWIN, TOTALDRAW, TOTALLOSS, AVATAR) VALUES (@user, @pass, @mail, @phone, @name, @point, @win, @draw, @loss, @avatar)";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user", username);
                        cmd.Parameters.AddWithValue("@pass", password); // đã là hash từ client
                        cmd.Parameters.AddWithValue("@mail", email);
                        cmd.Parameters.AddWithValue("@name", fullname);
                        cmd.Parameters.AddWithValue("@phone", sdt);
                        cmd.Parameters.AddWithValue("@point", elo);
                        cmd.Parameters.AddWithValue("@win", totalwin);
                        cmd.Parameters.AddWithValue("@draw", totaldraw);
                        cmd.Parameters.AddWithValue("@loss", totalloss);
                        cmd.Parameters.AddWithValue("@avatar", AVATAR);

                        cmd.ExecuteNonQuery();
                    }
                }
                AppendText($"Đã thêm user {username} vào database");
            }
            catch (Exception ex)
            {
                AppendText("Lỗi " + ex.Message);
            }
        }
        #endregion
        private void TCPServer_Load(object sender, EventArgs e)
        {
            StartServer();
            
        }


        #region Xử lí request từ client
        private void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 8080);
            listener.Start();
            isRunning = true;

            Task.Run(() => ListenForClient());
        }
        private void ListenForClient()
        {
            while (isRunning)
            {
                TcpClient client = listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(HandleClient, client);
            }
        }

        private async Task<string> ReceiveMessage(NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead <= 0) return null;
            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }
        private async void HandleClient(object obj)
        {
            TcpClient client = obj as TcpClient;
            if (client == null) return;
            IPEndPoint clientEP = client.Client.RemoteEndPoint as IPEndPoint;
            string currentUsername = null;
            lock (connectedClients)
            {
                connectedClients[clientEP] = clientEP.Address;
            }
            UpdateClientList();
            AppendText($"Client {clientEP} đã kết nối.");
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    bool isLogin = false;
                    bool isRegister = false;
                    while (client.Connected && !isLogin) //vòng while này để kết nối 1 lần rồi close dành cho login và register
                    {
                        string data = await ReceiveMessage(stream);
                        data.Trim();
                        if (data == null) break;
                        AppendText($"Client ({clientEP}) gửi 1 thông điệp: {data}");
                        string[] parts = data.Split('|', StringSplitOptions.RemoveEmptyEntries);
                        if (parts[0] == "LOGIN") //login
                        {
                            if (parts.Length < 3)
                            {
                                AppendText($"Client {clientEP} gửi dữ liệu không hợp lệ: {data}");
                                byte[] InvalidData = Encoding.UTF8.GetBytes("Fail: Invalid data");
                                await stream.WriteAsync(InvalidData, 0, InvalidData.Length);
                                continue;
                            }
                            //format:LOGIN|username|password
                            string username = parts[1].Trim();
                            string password = parts[2].Trim();
                            if (OnlineUsers.ContainsKey(username))
                            {
                                byte[] usernameexist = Encoding.UTF8.GetBytes("LOGIN|FAILED_Tài khoản của bạn đã được đăng nhập ở nơi khác\n");
                                AppendText($"Tài khoản {username} đang đăng nhập trong server");
                                await stream.WriteAsync(usernameexist, 0, usernameexist.Length);
                                break;
                            }

                            bool status = CheckLogin(username, password);
                            string response = status ? "LOGIN|SUCCESS\n" : "LOGIN|FAILED\n";

                            byte[] responsebytes = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(responsebytes, 0, responsebytes.Length);

                            AppendText($"Trạng thái đăng nhập client ({clientEP}): {response}");

                            if (status)
                            {
                                isLogin = true;
                                currentUsername = username;
                                AppendText($"Client {clientEP} đăng nhập thành công.");
                                lock (OnlineUsers)
                                {
                                    OnlineUsers[currentUsername] = client; // Lưu client theo username
                                }
                                break;
                            }
                            else
                            {
                                AppendText($"Client {clientEP} đăng nhập thất bại, đóng kết nối.");
                                return;
                            }
                        }
                        if (parts[0] == "REGISTER") //register
                        {
                            if (parts.Length < 6)
                            {
                                AppendText($"Client {clientEP} gửi dữ liệu không hợp lệ: {data}");
                                byte[] InvalidData = Encoding.UTF8.GetBytes("Fail: Invalid data");
                                await stream.WriteAsync(InvalidData, 0, InvalidData.Length);
                                continue;
                            }
                            //REGISTER|username|password|email|fullname|phone
                            string Username = parts[1];
                            string Password = parts[2];
                            string Email = parts[3];
                            string Fullname = parts[4];
                            string Sdt = parts[5].Trim();
                            bool isUsernameExist = UsernameExist(Username); //cái đoạn này logic hơi ngược tí =)))))
                            string response;
                            if (isUsernameExist)
                                response = "REGISTER|FAILED\n";
                            else
                            {
                                AddUsertoDatabase(Username, Password, Email, Fullname, Sdt, 1200, 0, 0, 0, "Avatar/icons8-avatar-50");
                                currentUsername = Username;
                                response = "REGISTER|SUCCESS\n";
                            }

                            byte[] responsebytes = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(responsebytes, 0, responsebytes.Length);

                            AppendText($"Trạng thái đăng ký của client ({clientEP}): {response}");

                            if (!isUsernameExist)
                            {
                                isLogin = true;
                                AppendText($"Client {clientEP} đăng ký thành công.");
                                return;
                            }
                            else
                            {
                                AppendText($"Client {clientEP} đăng ký thất bại, đóng kết nối.");
                                return;
                            }
                        }
                    }
                    while (client.Connected) //còn vòng while này dành cho các tác vụ khác khi đã login vào server và nó sẽ giữ connected cho đến khi logout
                    {
                        string msg = await ReceiveMessage(stream);
                        if (msg == null)
                        {
                            break;
                        }
                        AppendText($"[Đã đăng nhập] {clientEP}: {msg}");
                        string[] parts = msg.Split('|');
                        if (parts[0] == "LOGOUT")
                        {
                            if (!string.IsNullOrEmpty(currentUsername))
                            {
                                lock (OnlineUsers)
                                {
                                    OnlineUsers.Remove(currentUsername);
                                }
                            }
                            string response = "Logout success";
                            byte[] responsebytes = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(responsebytes, 0, responsebytes.Length);
                            return;
                        }
                        if (parts[0] == "GETINFO")
                        {
                            string username = parts[1].Trim();
                            //truy vấn sql từ username
                            string response = GetUserInfoFromDatabase(username);
                            response += '\n';
                            AppendText(response);
                            //gửi về client
                            byte[] data = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(data, 0, data.Length);
                        }
                        if (parts[0] == "CREATE")
                        {
                            if (currentUsername == null)
                            {
                                string resp = "CREATE|FAILED\n"; //phải login vào rồi mới đc tạo phòng (có thể bỏ dòng này để test nha)
                                byte[] respBytes = Encoding.UTF8.GetBytes(resp);
                                await stream.WriteAsync(respBytes, 0, respBytes.Length);
                                continue;
                            }

                            string status = "public";
                            if (parts.Length >= 2)
                                status = parts[1].Trim().ToLower();

                            string roomId = CreateRoom(currentUsername, status);
                            string response;
                            if (roomId != null)
                            {
                                response = $"CREATE|{roomId}\n";
                            }
                            else
                            {
                                response = "CREATE|FAILED\n";
                            }
                            AppendText($"Server trả về ID phòng vừa tạo {response}");
                            byte[] respData = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(respData, 0, respData.Length);
                            continue;
                        }
                        if (parts[0].Trim() == "ListRoom")
                        {
                            var rooms = GetPublicRooms();  // giờ trả về RoomID, NumberPlayer, HostUsername, HostElo
                            var sb = new StringBuilder("ListRoom|");
                            foreach (var room in rooms)
                            {
                                //định dạng: RoomID,NumberPlayer,HostUsername,HostElo
                                sb.Append($"{room.RoomID},{room.NumberPlayer},{room.HostUsername},{room.HostElo}|");
                            }

                            string response = sb.ToString();  //có thể rỗng("") nếu không có phòng nào
                            response =response.Substring(0,response.Length - 1);
                            response += '\n';
                            AppendText("[SERVER] Gửi danh sách phòng:\n" + (string.IsNullOrEmpty(response) ? "(trống)" : response));

                            byte[] respData = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(respData, 0, respData.Length);
                            continue;
                        }
                        if (parts[0] == "JOIN")
                        {
                            // Kiểm tra format gói tin
                            if (parts.Length < 2)
                            {
                                string resp = "JOIN|FAILED\n";
                                byte[] respBytes = Encoding.UTF8.GetBytes(resp);
                                await stream.WriteAsync(respBytes, 0, respBytes.Length);

                                AppendText($"Client {clientEP} gửi msg Join không hợp lệ: {msg}");
                                continue;
                            }

                            string roomId = parts[1].Trim();

                            //ID (6 chữ số)
                            if (roomId.Length != 6 || !roomId.All(char.IsDigit))
                            {
                                string resp = "JOIN|FAILED\n";
                                byte[] respBytes = Encoding.UTF8.GetBytes(resp);
                                await stream.WriteAsync(respBytes, 0, respBytes.Length);

                                AppendText($"Client {clientEP} gửi msg Join với RoomID không hợp lệ: {roomId}");
                                continue;
                            }

                            bool result = TryJoinRoom(roomId, currentUsername);
                            string response = result ? "JOIN|SUCCESS\n" : "JOIN|FAILED\n";

                            AppendText($"Client {clientEP} Join room {roomId}: {response}");

                            byte[] data = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(data, 0, data.Length);

                            // ❌ KHÔNG gọi StartGameForRoomAsync ở đây nữa
                            continue;
                        }

                        if (parts[0] == "JOINID")
                        {
                            if (parts.Length < 2)
                            {
                                string resp = "JOINID|FAILED\n";
                                byte[] respBytes = Encoding.UTF8.GetBytes(resp);
                                await stream.WriteAsync(respBytes, 0, respBytes.Length);

                                AppendText($"Client {clientEP} gửi msg JoinID không hợp lệ: {msg}");
                                continue;
                            }

                            string roomId = parts[1].Trim();

                            if (roomId.Length != 6 || !roomId.All(char.IsDigit)) //ID
                            {
                                string resp = "JOINID|FAILED\n";
                                byte[] respBytes = Encoding.UTF8.GetBytes(resp);
                                await stream.WriteAsync(respBytes, 0, respBytes.Length);

                                AppendText($"Client {clientEP} gửi msg JoinID với RoomID không hợp lệ: {roomId}");
                                continue;
                            }

                            bool result = TryJoinRoom(roomId, currentUsername);
                            string response = result ? "JOINID|SUCCESS\n" : "JOINID|FAILED\n";

                            AppendText($"Client {clientEP} JoinID room {roomId}: {response}");

                            byte[] data = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(data, 0, data.Length);

                            // ❌ KHÔNG gọi StartGameForRoomAsync ở đây nữa
                            continue;
                        }
                        if (parts[0] == "READY")
                        {
                            if (parts.Length < 2)
                            {
                                AppendText($"Client {clientEP} gửi READY không hợp lệ: {msg}");
                                continue;
                            }

                            string roomId = parts[1].Trim();
                            AppendText($"[READY] {currentUsername} ready in room {roomId}");

                            // Gọi hàm này: nó tự check trong DB xem room đã đủ 2 người chưa
                            await StartGameForRoomAsync(roomId);
                            continue;
                        }
                        if (parts[0] == "MOVE")
                        {
                            // MOVE|roomId|fromX|fromY|toX|toY
                            if (parts.Length < 6)
                                continue;

                            string roomId = parts[1];
                            string opponent = GetOpponentOf(roomId, currentUsername);
                            if (string.IsNullOrEmpty(opponent))
                                continue;

                            TcpClient oppClient = null;
                            lock (OnlineUsers)
                            {
                                OnlineUsers.TryGetValue(opponent, out oppClient);
                            }

                            if (oppClient != null)
                            {
                                try
                                {
                                    string forward = $"OPP_MOVE|{roomId}|{parts[2]}|{parts[3]}|{parts[4]}|{parts[5]}";
                                    byte[] data = Encoding.UTF8.GetBytes(forward);
                                    await oppClient.GetStream().WriteAsync(data, 0, data.Length);
                                }
                                catch (Exception ex)
                                {
                                    AppendText($"[MOVE] Lỗi gửi cho {opponent}: {ex.Message}");
                                }
                            }
                            continue;
                        }
                        if (parts[0] == "GET_RANK")
                        {
                            int page = 1;
                            int pageSize = 10;

                            if (parts.Length >= 2 && int.TryParse(parts[1], out int p))
                                page = p;
                            if (parts.Length >= 3 && int.TryParse(parts[2], out int ps))
                                pageSize = ps;

                            AppendText($"Client {clientEP} yêu cầu bảng xếp hạng: page={page}, pageSize={pageSize}");

                            int totalCount;
                            var entries = GetLeaderboardPage(page, pageSize, out totalCount);

                            //cấu trúc response: RANK_PAGE|page|totalCount|user,fullname,elo,win,draw,loss;...
                            var sb = new StringBuilder();
                            foreach (var e in entries)
                            {
                                //tránh ký tự '|' trong fullname nếu có
                                string safeFullname = e.Fullname?.Replace("|", "/") ?? "";
                                if(currentUsername==e.Username) sb.Append(e.Username + "(Me)").Append(',').Append(safeFullname).Append(',').Append(e.Elo).Append(',').Append(e.TotalWin).Append(',').Append(e.TotalDraw).Append(',').Append(e.TotalLoss).Append(';');
                                else sb.Append(e.Username).Append(',').Append(safeFullname).Append(',').Append(e.Elo).Append(',').Append(e.TotalWin).Append(',').Append(e.TotalDraw).Append(',').Append(e.TotalLoss).Append(';');
                            }
                            string dataPart = sb.ToString();
                            string response = $"RANK_PAGE|{page}|{totalCount}|{dataPart}\n";

                            byte[] respBytes = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(respBytes, 0, respBytes.Length);

                            AppendText($"[SERVER] Gửi leaderboard: page={page}, totalCount={totalCount}");
                            continue;
                        }

                        if (parts[0] == "GAME_OVER")
                        {
                            // GAME_OVER|roomId|winnerColor
                            if (parts.Length < 3)
                                continue;

                            string roomId = parts[1];
                            string winnerColor = parts[2];

                            string opponent = GetOpponentOf(roomId, currentUsername);
                            if (!string.IsNullOrEmpty(opponent))
                            {
                                TcpClient oppClient = null;
                                lock (OnlineUsers)
                                {
                                    OnlineUsers.TryGetValue(opponent, out oppClient);
                                }

                                if (oppClient != null)
                                {
                                    try
                                    {
                                        string forward = $"GAME_OVER|{roomId}|{winnerColor}";
                                        byte[] data = Encoding.UTF8.GetBytes(forward);
                                        await oppClient.GetStream().WriteAsync(data, 0, data.Length);
                                    }
                                    catch (Exception ex)
                                    {
                                        AppendText($"[GAME_OVER] Lỗi gửi cho {opponent}: {ex.Message}");
                                    }
                                }
                            }

                            // TODO: sau này update lịch sử đấu / ELO ở đây

                            continue;
                        }
                        if (parts[0] == "CHATGLOBAL")
                        {
                            if (parts.Length < 3)
                                continue;
                            string username = parts[1];
                            string chatmsg = parts[2];
                            AppendText($"Server gửi thông điệp CHATGLOBAL|{username}|{chatmsg} lại cho tất cả các client");

                            await BroadcastGlobalChat(username, chatmsg);
                            continue;   
                        }
                        if (parts[0] == "RANDOM") 
                        {
                            // Tạo ds phòng hợp lệ để random
                            var rooms = GetPublicRooms();
                            var ListRoomPublic = rooms.Select(r => r.RoomID).ToList();
                            string chosenRoomId = null;
                            string response;

                            if (ListRoomPublic.Count == 0)
                            {
                                response = "RANDOM|fail";
                            }
                            else
                            {
                                // Random các phòng đã có trong ds
                                int index;
                                lock (_rand) { index = _rand.Next(ListRoomPublic.Count); }
                                chosenRoomId = ListRoomPublic[index];

                                // Gửi phản hồi về Client
                                // RANDOM|ID
                                response = $"RANDOM|{chosenRoomId}";
                            }

                            byte[] respBytes = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(respBytes, 0, respBytes.Length);

                            AppendText($"[RANDOM] {clientEP} -> {response}");

                            // Cập nhật database
                            if (!string.IsNullOrEmpty(chosenRoomId))
                            {
                                using (var conn = Connection.GetSqlConnection())
                                {
                                    conn.Open();
                                    string sql = @"
                                        UPDATE ROOM
                                        SET 
                                            NumberPlayer = NumberPlayer + 1,
                                            RoomIsFull   = CASE 
                                                WHEN NumberPlayer + 1 = 2 THEN 1 
                                                ELSE RoomIsFull 
                                            END
                                        WHERE RoomID = @id
                                          AND IsClosed   = 0
                                          AND RoomIsFull = 0
                                          AND NumberPlayer < 2;";
                                    using (var cmd = new SqlCommand(sql, conn))
                                    {
                                        cmd.Parameters.AddWithValue("@id", chosenRoomId);
                                        int rows = cmd.ExecuteNonQuery();
                                        AppendText($@"[RANDOM->DB] {chosenRoomId}: {(rows == 1 ? "UPDATED" : "SKIP")}");
                                    }
                                }
                            }


                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendText($"Client {clientEP} lỗi: {ex.Message}");
            }
            finally
            {
                AppendText($"Client {clientEP} đã thoát kết nối");
                lock (connectedClients)
                {
                    connectedClients.Remove(clientEP);
                }
                if (currentUsername != null)
                {
                    lock (OnlineUsers)
                    {
                        OnlineUsers.Remove(currentUsername);
                    }
                }
                UpdateClientList();
                client.Close();
            }
        }
        #endregion

        #region Lấy info user từ database
        private string GetUserInfoFromDatabase(string username)
        {
            string response = "GETINFO|NOT_FOUND"; //response mặc định

            try
            {
                using (SqlConnection conn = Connection.GetSqlConnection())
                {
                    conn.Open();
                    string query = @"
                SELECT FULLNAME, ELO, TOTALWIN, TOTALDRAW, TOTALLOSS, AVATAR
                FROM UserDB
                WHERE USERNAME = @user";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user", username);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string fullName = reader["FULLNAME"].ToString();
                                int elo = reader["ELO"] != DBNull.Value ? Convert.ToInt32(reader["ELO"]) : 1200;
                                int totalWin = reader["TOTALWIN"] != DBNull.Value ? Convert.ToInt32(reader["TOTALWIN"]) : 0;
                                int totalDraw = reader["TOTALDRAW"] != DBNull.Value ? Convert.ToInt32(reader["TOTALDRAW"]) : 0;
                                int totalLoss = reader["TOTALLOSS"] != DBNull.Value ? Convert.ToInt32(reader["TOTALLOSS"]) : 0;
                                string avatar = reader["AVATAR"] != DBNull.Value ? reader["AVATAR"].ToString() : "";

                                //gửi response về cho client
                                response = $"GETINFO|{fullName}|{elo}|{totalWin}|{totalDraw}|{totalLoss}|{avatar}";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB Error GetUserInfo: " + ex.Message);
                response = "GETINFO|ERROR";
            }
            return response;
        }
        #endregion


        #region Các hàm update UI server
        private void AppendText(string text) //thêm cập nhập của client vào richtextbox thông báo
        {
            if (rtb_servernotify.InvokeRequired)
                rtb_servernotify.Invoke(new Action<string>(AppendText), text);
            else
                rtb_servernotify.AppendText(text + Environment.NewLine);
        }



        private Dictionary<IPEndPoint, IPAddress> connectedClients = new Dictionary<IPEndPoint, IPAddress>();
        private void UpdateClientList() //thêm ip của client đang kết nối vào richtextbox
        {
            if (rtb_clientsconnection.InvokeRequired)
            {
                rtb_clientsconnection.Invoke(new Action(UpdateClientList));
            }
            else
            {
                rtb_clientsconnection.Clear();
                lock (connectedClients)
                {
                    foreach (var keyvalue in connectedClients)
                    {
                        rtb_clientsconnection.AppendText(keyvalue.Value.ToString() + Environment.NewLine);
                    }
                }
            }
        }

        #endregion
        #region Xử lí room

        private static readonly Random _rand = new();

        public static string CreateRoom(string hostUsername, string status)
        {
            bool isPublic = status.Equals("public", StringComparison.OrdinalIgnoreCase);

            using (var conn = Connection.GetSqlConnection())
            {
                conn.Open();

                for (int attempts = 0; attempts < 50; attempts++)
                {
                    string roomId = _rand.Next(0, 1_000_000).ToString("D6");

                    string sql = @"INSERT INTO ROOM (RoomID, UsernameHost, NumberPlayer, RoomIsFull, IsPublic, IsClosed) VALUES (@id, @host, @num, 0, @isPublic, 0);";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", roomId);
                        cmd.Parameters.AddWithValue("@host", hostUsername);
                        cmd.Parameters.AddWithValue("@num", 1);          // host vào là 1 player
                        cmd.Parameters.AddWithValue("@isPublic", isPublic ? 1 : 0);

                        try
                        {
                            int rows = cmd.ExecuteNonQuery();
                            if (rows == 1)
                                return roomId;
                        }
                        catch (SqlException ex)
                        {
                            // nếu trùng PK RoomID thì thử lại vòng tiếp theo
                            if (ex.Number != 2627) // PK violation
                                throw;
                        }
                    }
                }

                return null;
            }
        }


        public static List<(string RoomID, int NumberPlayer, string HostUsername, int HostElo)> GetPublicRooms()
        {
            var list = new List<(string, int, string, int)>();

            using (var conn = Connection.GetSqlConnection())
            {
                conn.Open();
                string sql = @"
            SELECT  R.RoomID, R.NumberPlayer, R.UsernameHost, U.ELO
            FROM ROOM R
            JOIN UserDB U ON U.USERNAME = R.UsernameHost
            WHERE R.IsPublic = 1 AND R.IsClosed = 0 AND R.RoomIsFull = 0;";

                using (var cmd = new SqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string roomId = reader["RoomID"].ToString();
                        int numberPlayer = Convert.ToInt32(reader["NumberPlayer"]);
                        string hostUsername = reader["UsernameHost"].ToString();
                        int hostElo = reader["ELO"] != DBNull.Value ? Convert.ToInt32(reader["ELO"]) : 1200;

                        list.Add((roomId, numberPlayer, hostUsername, hostElo));
                    }
                }
            }

            return list;
        }



        public static bool TryJoinRoom(string roomId, string clientUsername)
        {
            using (var conn = Connection.GetSqlConnection())
            {
                conn.Open();
                string sql = @"UPDATE ROOM
                             SET UsernameClient = @client, NumberPlayer = NumberPlayer + 1, RoomIsFull = CASE WHEN NumberPlayer + 1 >= 2 THEN 1 ELSE 0 END
                             WHERE RoomID = @id AND IsClosed = 0 AND RoomIsFull = 0 AND NumberPlayer < 2;";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@client", clientUsername);
                    cmd.Parameters.AddWithValue("@id", roomId);

                    int rows = cmd.ExecuteNonQuery();
                    return rows == 1;
                }
            }
        }
        #endregion

        #region LAN 
        // trong class Server
        private static readonly Dictionary<string, TcpClient> OnlineUsers = new(); 

        private async Task StartGameForRoomAsync(string roomId)
        {
            using (var conn = Connection.GetSqlConnection())
            {
                conn.Open();
                string sql = @"
SELECT UsernameHost, UsernameClient, NumberPlayer, RoomIsFull, IsClosed
FROM ROOM
WHERE RoomID = @id";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", roomId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            AppendText($"[GAME_START] Không tìm thấy room {roomId} trong DB.");
                            return;
                        }

                        string host = reader["UsernameHost"]?.ToString();
                        string clientUser = reader["UsernameClient"]?.ToString();
                        int number = Convert.ToInt32(reader["NumberPlayer"]);
                        bool isFull = Convert.ToBoolean(reader["RoomIsFull"]);
                        bool isClosed = Convert.ToBoolean(reader["IsClosed"]);

                        // Chỉ start khi đủ 2 người, full và chưa đóng
                        if (number != 2 || !isFull || isClosed ||
                            string.IsNullOrEmpty(host) || string.IsNullOrEmpty(clientUser))
                        {
                            AppendText($"[GAME_START] Room {roomId} chưa đủ điều kiện start.");
                            return;
                        }

                        AppendText($"[GAME_START] Room {roomId} đủ 2 người: {host} vs {clientUser}");

                        // Lấy TcpClient của 2 user
                        TcpClient hostClient = null;
                        TcpClient clientClient = null;
                        lock (OnlineUsers)
                        {
                            OnlineUsers.TryGetValue(host, out hostClient);
                            OnlineUsers.TryGetValue(clientUser, out clientClient);
                        }

                        if (hostClient != null)
                        {
                            try
                            {
                                var s = hostClient.GetStream();
                                string msg = $"GAME_START|white|{roomId}|{clientUser}\n";
                                byte[] data = Encoding.UTF8.GetBytes(msg);
                                await s.WriteAsync(data, 0, data.Length);
                            }
                            catch (Exception ex)
                            {
                                AppendText($"[GAME_START] Lỗi gửi cho host {host}: {ex.Message}");
                            }
                        }
                        else
                        {
                            AppendText($"[GAME_START] Không tìm thấy TcpClient của host {host}.");
                        }

                        if (clientClient != null)
                        {
                            try
                            {
                                var s = clientClient.GetStream();
                                string msg = $"GAME_START|black|{roomId}|{host}\n";
                                byte[] data = Encoding.UTF8.GetBytes(msg);
                                await s.WriteAsync(data, 0, data.Length);
                            }
                            catch (Exception ex)
                            {
                                AppendText($"[GAME_START] Lỗi gửi cho client {clientUser}: {ex.Message}");
                            }
                        }
                        else
                        {
                            AppendText($"[GAME_START] Không tìm thấy TcpClient của client {clientUser}.");
                        }
                    }
                }
            }
        }

        private string GetOpponentOf(string roomId, string currentUser)
        {
            using (var conn = Connection.GetSqlConnection())
            {
                conn.Open();
                string sql = "SELECT UsernameHost, UsernameClient FROM ROOM WHERE RoomID = @id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", roomId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read()) return null;

                        string host = reader["UsernameHost"].ToString();
                        string client = reader["UsernameClient"].ToString();

                        if (currentUser == host) return client;
                        if (currentUser == client) return host;
                        return null;
                    }
                }
            }
        }
        #endregion
        #region Raking
        private static List<(string Username, string Fullname, int Elo, int TotalWin, int TotalDraw, int TotalLoss)> GetLeaderboardPage(int page, int pageSize, out int totalCount)
        {
            var list = new List<(string, string, int, int, int, int)>();
            totalCount = 0;

            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            int offset = (page - 1) * pageSize;

            using (var conn = Connection.GetSqlConnection())
            {
                conn.Open();

                //lấy tổng số user  có trong server
                using (var cmdCount = new SqlCommand("SELECT COUNT(*) FROM UserDB;", conn))
                {
                    totalCount = (int)cmdCount.ExecuteScalar();
                }

                if (totalCount == 0)
                    return list;

                //lấy 1 trang user theo 1 trang
                string sql = @"SELECT Username, Fullname, Elo, TotalWin, TotalDraw, TotalLoss
                               FROM UserDB
                               ORDER BY Elo DESC, TotalWin DESC, Username ASC
                               OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@offset", offset);
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                            string username = reader["Username"].ToString();
                            string fullname = reader["Fullname"].ToString();
                            int elo = reader["Elo"] != DBNull.Value ? Convert.ToInt32(reader["Elo"]) : 1200;
                            int win = reader["TotalWin"] != DBNull.Value ? Convert.ToInt32(reader["TotalWin"]) : 0;
                            int draw = reader["TotalDraw"] != DBNull.Value ? Convert.ToInt32(reader["TotalDraw"]) : 0;
                            int loss = reader["TotalLoss"] != DBNull.Value ? Convert.ToInt32(reader["TotalLoss"]) : 0;

                            list.Add((username, fullname, elo, win, draw, loss));
                        }
                    }
                }
            }

            return list;
        }
        #endregion

        #region ChatGlobal
        private static readonly ConcurrentDictionary<TcpClient, SemaphoreSlim> SendLock = new(); //tránh ghi trồng lên nhau 
        private async Task BroadcastGlobalChat(string username, string chatmsg, TcpClient ExceptClient = null)
        {
            string broadcast = $"CHATGLOBAL|{username}|{chatmsg}\n";
            byte[] bytes = Encoding.UTF8.GetBytes(broadcast);
            List<TcpClient> targets;
            lock (OnlineUsers)
            {
                targets = OnlineUsers.Values.Where(c => c != null).Distinct().ToList(); //lấy danh sách clients đang online, lọc và làm sạch trước khi gửi lại broadcast
                
            }
            foreach (var c in targets)
            {
                if (c == ExceptClient) continue;
                if (!c.Connected) continue;
                var sem = SendLock.GetOrAdd(c, _ => new SemaphoreSlim(1, 1)); //semaphoreslim(1,1) quăng cho client 1 cái key => giúp chặn các msg gửi chồng lên nhau vào cùng 1 client vì tính chất của tcp có thể bị đan xen 
                await sem.WaitAsync();
                try
                {
                    await c.GetStream().WriteAsync(bytes, 0, bytes.Length);
                }
                catch
                {

                }
                finally
                {
                    sem.Release();
                }
            }
            
        }


        #endregion
        #region cập nhập UI waitngroom broacast
        private static string KeyUser(string u) => (u ?? "").Trim().ToLowerInvariant();

        private async Task SendLineAsync(TcpClient cli, string line)
        {
            if (cli == null || !cli.Connected) return;
            var data = Encoding.UTF8.GetBytes(line.EndsWith("\n") ? line : line + "\n");
            await cli.GetStream().WriteAsync(data, 0, data.Length);
        }

        private async Task BroadcastRoomUpdateAsync(string roomId)
        {
            string host = null, client = null;

            using (var conn = Connection.GetSqlConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(@"SELECT UsernameHost, UsernameClient FROM ROOM WHERE RoomID=@id", conn);
                cmd.Parameters.AddWithValue("@id", roomId);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) return;

                host = r["UsernameHost"]?.ToString();
                client = r["UsernameClient"]?.ToString();
            }

            var hostKey = KeyUser(host);
            var clientKey = KeyUser(client);

            TcpClient hostCli = null, clientCli = null;
            lock (OnlineUsers)
            {
                OnlineUsers.TryGetValue(hostKey, out hostCli);
                OnlineUsers.TryGetValue(clientKey, out clientCli);
            }

            string msg = $"ROOM_UPDATE|{roomId}|{host}|{client}\n";

            // gửi cho host nếu online
            await SendLineAsync(hostCli, msg);

            // gửi cho client nếu online và khác host
            if (!string.IsNullOrEmpty(clientKey) && clientKey != hostKey)
                await SendLineAsync(clientCli, msg);

            AppendText($"[ROOM_UPDATE] {msg.Trim()}");
        }
        #endregion
    }
}
