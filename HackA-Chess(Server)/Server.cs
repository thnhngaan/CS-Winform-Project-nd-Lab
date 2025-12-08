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
                            string username = parts[1];
                            string password = parts[2];

                            bool status = CheckLogin(username, password);
                            string response = status ? "Login success" : "Login failed";

                            byte[] responsebytes = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(responsebytes, 0, responsebytes.Length);

                            AppendText($"Trạng thái đăng nhập client ({clientEP}): {response}");

                            if (status)
                            {
                                isLogin = true;
                                currentUsername = username;
                                AppendText($"Client {clientEP} đăng nhập thành công.");
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
                            string Sdt = parts[5];
                            bool isUsernameExist = UsernameExist(Username); //cái đoạn này logic hơi ngược tí =)))))
                            string response;
                            if (isUsernameExist)
                                response = "Register failed";
                            else
                            {
                                AddUsertoDatabase(Username, Password, Email, Fullname, Sdt, 1200, 0, 0, 0, "Avatar/icons8-avatar-50");
                                currentUsername = Username;
                                response = "Register success";
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
                        if (msg == null) continue;
                        AppendText($"[Đã đăng nhập] {clientEP}: {msg}");
                        string[] parts = msg.Split('|');
                        if (parts[0] == "LOGOUT")
                        {
                            string response = "Logout success";
                            byte[] responsebytes = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(responsebytes, 0, responsebytes.Length);
                            return;
                        }
                        if (parts[0] == "GETINFO")
                        {
                            string username = parts[1];
                            //truy vấn sql từ username
                            string response = GetUserInfoFromDatabase(username);

                            //gửi về client
                            byte[] data = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(data, 0, data.Length);
                        }
                        if (parts[0] == "CREATE")
                        {
                            if (currentUsername == null)
                            {
                                string resp = "Create fail"; //phải login vào rồi mới đc tạo phòng (có thể bỏ dòng này để test nha)
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
                                response = roomId;
                            }
                            else
                            {
                                response = "Create fail";
                            }
                            AppendText($"Server trả về ID phòng vừa tạo {response}");
                            byte[] respData = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(respData, 0, respData.Length);
                            continue;
                        }
                        if (parts[0] == "ListRoom")
                        {
                            var rooms = GetPublicRooms();  // giờ trả về RoomID, NumberPlayer, HostUsername, HostElo
                            var sb = new StringBuilder();
                            foreach (var room in rooms)
                            {
                                //định dạng: RoomID,NumberPlayer,HostUsername,HostElo
                                sb.AppendLine($"{room.RoomID},{room.NumberPlayer},{room.HostUsername},{room.HostElo}");
                            }

                            string response = sb.ToString();  //có thể rỗng("") nếu không có phòng nào

                            AppendText("[SERVER] Gửi danh sách phòng:\n" + (string.IsNullOrEmpty(response) ? "(trống)" : response));

                            byte[] respData = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(respData, 0, respData.Length);
                            continue;
                        }
                        if (parts[0] == "Join")
                        {
                            // Kiểm tra format gói tin
                            if (parts.Length < 2)
                            {
                                string resp = "Join fail";
                                byte[] respBytes = Encoding.UTF8.GetBytes(resp);
                                await stream.WriteAsync(respBytes, 0, respBytes.Length);

                                AppendText($"Client {clientEP} gửi msg Join không hợp lệ: {msg}");
                                continue;
                            }

                            string roomId = parts[1].Trim();

                            //ID (6 chữ số)
                            if (roomId.Length != 6 || !roomId.All(char.IsDigit))
                            {
                                string resp = "Join fail";
                                byte[] respBytes = Encoding.UTF8.GetBytes(resp);
                                await stream.WriteAsync(respBytes, 0, respBytes.Length);

                                AppendText($"Client {clientEP} gửi msg Join với RoomID không hợp lệ: {roomId}");
                                continue;
                            }

                            bool result = TryJoinRoom(roomId, currentUsername);
                            string response = result ? "Join success" : "Join fail";

                            AppendText($"Client {clientEP} Join room {roomId}: {response}");

                            byte[] data = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(data, 0, data.Length);
                            continue;
                        }
                        if (parts[0] == "JoinID")
                        {
                            if (parts.Length < 2)
                            {
                                string resp = "JoinID fail";
                                byte[] respBytes = Encoding.UTF8.GetBytes(resp);
                                await stream.WriteAsync(respBytes, 0, respBytes.Length);

                                AppendText($"Client {clientEP} gửi msg JoinID không hợp lệ: {msg}");
                                continue;
                            }

                            string roomId = parts[1].Trim();

                            if (roomId.Length != 6 || !roomId.All(char.IsDigit)) //ID
                            {
                                string resp = "JoinID fail";
                                byte[] respBytes = Encoding.UTF8.GetBytes(resp);
                                await stream.WriteAsync(respBytes, 0, respBytes.Length);

                                AppendText($"Client {clientEP} gửi msg JoinID với RoomID không hợp lệ: {roomId}");
                                continue;
                            }

                            bool result = TryJoinRoom(roomId, currentUsername);
                            string response = result ? "JoinID success" : "JoinID fail";

                            AppendText($"Client {clientEP} JoinID room {roomId}: {response}");

                            byte[] data = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(data, 0, data.Length);
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
                                // tránh ký tự '|' trong fullname nếu có
                                string safeFullname = e.Fullname?.Replace("|", "/") ?? "";
                                if(currentUsername==e.Username) sb.Append(e.Username + "(Me)").Append(',').Append(safeFullname).Append(',').Append(e.Elo).Append(',').Append(e.TotalWin).Append(',').Append(e.TotalDraw).Append(',').Append(e.TotalLoss).Append(';');
                                else sb.Append(e.Username).Append(',').Append(safeFullname).Append(',').Append(e.Elo).Append(',').Append(e.TotalWin).Append(',').Append(e.TotalDraw).Append(',').Append(e.TotalLoss).Append(';');
                            }
                            string dataPart = sb.ToString();
                            string response = $"RANK_PAGE|{page}|{totalCount}|{dataPart}";

                            byte[] respBytes = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(respBytes, 0, respBytes.Length);

                            AppendText($"[SERVER] Gửi leaderboard: page={page}, totalCount={totalCount}");
                            continue;
                        }

                        //và vô vàn tác vụ khác quăng dô đây hết nha mấy môm 
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
    }
}
