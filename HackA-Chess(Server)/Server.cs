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
using System.Security.Cryptography.Pkcs;
using System.Diagnostics.Eventing.Reader;
using System.Web;

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
                            if (OnlineUsers.ContainsKey(KeyUser(username)))
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
                                    OnlineUsers[KeyUser(currentUsername)] = client;

                                }
                                await NotifyFriendsOnlineState(currentUsername, true);
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
                            AppendText($"[DISCONNECT] user={currentUsername} ep={client.Client.RemoteEndPoint}");
                            break; //thoát loop để cleanup
                        }
                        var lines = msg.Split('\n', StringSplitOptions.RemoveEmptyEntries);
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
                                    OnlineUsers.Remove(KeyUser(currentUsername));
                                }
                                await NotifyFriendsOnlineState(currentUsername, false);
                            }
                            string response = "LOGOUT|SUCCESS";
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
                            string status = "", password = "";
                            if (parts.Length == 2)
                            {
                                status = parts[1].Trim();
                            }
                            if (parts.Length == 3)
                            {
                                status = parts[1].Trim();
                                password = parts[2].Trim();
                            }

                            string roomId = CreateRoom(currentUsername, status, password);
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
                            await BroadcastRoomUpdateAsync(roomId);
                            continue;
                        }
                        if (parts[0].Trim() == "ListRoom")
                        {
                            var rooms = GetListRooms();  // giờ trả về RoomID, NumberPlayer, HostUsername,isPublic, HostElo
                            var sb = new StringBuilder("ListRoom|");
                            foreach (var room in rooms)
                            {
                                //định dạng: RoomID,NumberPlayer,HostUsername,isPublic,HostElo
                                sb.Append($"{room.RoomID},{room.NumberPlayer},{room.HostUsername},{room.status}, {room.HostElo}|");
                            }

                            string response = sb.ToString();  //có thể rỗng("") nếu không có phòng nào
                            response = response.Substring(0, response.Length - 1);
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
                            await BroadcastRoomUpdateAsync(roomId);
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

                            await BroadcastRoomUpdateAsync(roomId);
                            // ❌ KHÔNG gọi StartGameForRoomAsync ở đây nữa
                            continue;
                        }
                        if (parts[0] == "READY")
                        {
                            if (parts.Length < 2) continue;
                            await HandleReadyAsync(parts[1].Trim(), currentUsername);

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
                                OnlineUsers.TryGetValue(KeyUser(opponent), out oppClient);
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
                                if (currentUsername == e.Username) sb.Append(e.Username + "(Me)").Append(',').Append(safeFullname).Append(',').Append(e.Elo).Append(',').Append(e.TotalWin).Append(',').Append(e.TotalDraw).Append(',').Append(e.TotalLoss).Append(';');
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
                                    OnlineUsers.TryGetValue(KeyUser(opponent), out oppClient);
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
                        if (parts[0] == "UNREADY")
                        {

                            if (parts.Length < 2) continue;
                            await HandleUnreadyAsync(parts[1].Trim(), currentUsername);

                            continue;

                        }
                        if (parts[0] == "OUTROOM")
                        {
                            if (parts.Length < 2)
                            {
                                AppendText("[OUTROOM] invalid");
                                continue;
                            }
                            string roomId = parts[1].Trim();
                            AppendText($"[OUTROOM] {currentUsername} leave {roomId}");

                            await HandleOutRoom(roomId, currentUsername);

                            await SendLineAsync(client, $"OUTROOM|OK|{roomId}\n");
                            continue;
                        }
                        if (parts[0] == "CHECKPASS")
                        {
                            string roomId = parts.Length >= 2 ? parts[1].Trim() : "";
                            string pass = parts.Length >= 3 ? parts[2].Trim() : "";

                            bool ok = await CheckRoomPassword(roomId, pass);

                            string resp = ok ? "CHECKPASS|OK\n" : "CHECKPASS|NOTOK\n";
                            byte[] data = Encoding.UTF8.GetBytes(resp);
                            await stream.WriteAsync(data, 0, data.Length);
                            await stream.FlushAsync();
                            continue;
                        }
                        foreach (var raw in lines)
                        {
                            string msg1 = raw.Trim();        // bỏ \r, space
                            if (msg1.Length == 0) continue;

                            string[] parts1 = msg1.Split('|'); // giờ parts[0] sạch
                            string cmd = parts1[0].Trim();

                            if (cmd == "FRIEND_INVITE")
                            {
                                if (parts.Length < 2)
                                {
                                    await SendLineAsync(client, "FRIEND_INVITE|INVALID\n");
                                    continue;
                                }

                                string target = parts[1].Trim();
                                string me = currentUsername;

                                if (string.IsNullOrWhiteSpace(me))
                                {
                                    await SendLineAsync(client, "FRIEND_INVITE|NOLOGIN\n");
                                    continue;
                                }

                                if (KeyUser(me) == KeyUser(target))
                                {
                                    await SendLineAsync(client, "FRIEND_INVITE|SELF\n");
                                    continue;
                                }

                                if (!UserExists(target))
                                {
                                    await SendLineAsync(client, $"FRIEND_INVITE|NOT_FOUND|{target}\n");
                                    continue;
                                }

                                var row = GetFriendRow(me, target);
                                if (row != null)
                                {
                                    var (st, reqBy) = row.Value;

                                    if (st == 1)
                                    {
                                        await SendLineAsync(client, $"FRIEND_INVITE|ALREADY|{target}\n");
                                        continue;
                                    }

                                    // nếu đang pending:
                                    if (st == 0)
                                    {
                                        // nếu target đã gửi cho mình trước đó -> auto accept
                                        if (KeyUser(reqBy) == KeyUser(target))
                                        {
                                            AcceptInvite(target, me);
                                            await SendLineAsync(client, $"FRIEND_INVITE|AUTO_ACCEPT|{target}\n");

                                            // báo cho target nếu đang online
                                            await PushToUserIfOnline(target, $"FRIEND_RESP_PUSH|{KeyUser(me)}|ACCEPT\n");

                                            // sync online status 2 chiều
                                            await PushToUserIfOnline(target, $"FRIEND_STATUS|{KeyUser(me)}|1\n");
                                            await SendLineAsync(client, $"FRIEND_STATUS|{KeyUser(target)}|{(IsOnline(target) ? 1 : 0)}\n");
                                        }
                                        else
                                        {
                                            await SendLineAsync(client, $"FRIEND_INVITE|PENDING|{target}\n");
                                        }
                                        continue;
                                    }
                                }

                                // CHƯA CÓ ROW => TẠO LỜI MỜI
                                InsertPendingInvite(me, target);

                                await SendLineAsync(client, $"FRIEND_INVITE|OK|{target}\n");

                                // push cho người nhận nếu họ đang online (để hiện “incoming request” ngay)
                                await PushToUserIfOnline(target, $"FRIEND_INVITE_PUSH|{KeyUser(me)}\n");

                                continue;
                            }
                            if (cmd == "FRIEND_RESP")
                            {
                                if (parts.Length < 3) { await SendLineAsync(client, "FRIEND_RESP|INVALID\n"); continue; }

                                string fromUser = parts[1].Trim();//người đã gửi lời mời
                                string decision = parts[2].Trim().ToUpperInvariant(); //ACCEPT/DECLINE
                                string me = currentUsername;
                                var row = GetFriendRow(me, fromUser);
                                if (row == null || row.Value.status != 0)
                                {
                                    await SendLineAsync(client, $"FRIEND_RESP_ACK|NOT_FOUND|{fromUser}\n");
                                    continue;
                                }
                                //check đúng chiều: incoming request nghĩa là RequestedBy phải là fromUser
                                if (row.Value.RequestedBy != KeyUser(fromUser))
                                {
                                    await SendLineAsync(client, $"FRIEND_RESP_ACK|INVALID|{fromUser}\n");
                                    continue;
                                }
                                if (decision == "ACCEPT")
                                {
                                    AcceptInvite(fromUser, me);
                                    await SendLineAsync(client, $"FRIEND_RESP_ACK|OK|{fromUser}|ACCEPT\n");
                                    await PushToUserIfOnline(fromUser, $"FRIEND_RESP_PUSH|{KeyUser(me)}|ACCEPT\n");

                                    //sau khi accept, 2 bên có thể update online ngay
                                    await PushToUserIfOnline(fromUser, $"FRIEND_STATUS|{KeyUser(me)}|1\n");
                                    await SendLineAsync(client, $"FRIEND_STATUS|{KeyUser(fromUser)}|{(IsOnline(fromUser) ? 1 : 0)}\n");
                                }
                                else //DECLINE (từ chối)
                                {
                                    DeclineInvite(fromUser, me);
                                    await SendLineAsync(client, $"FRIEND_RESP_ACK|OK|{fromUser}|DECLINE\n");
                                    await PushToUserIfOnline(fromUser, $"FRIEND_RESP_PUSH|{KeyUser(me)}|DECLINE\n");
                                }
                                continue;
                            }
                            if (cmd == "FRIEND_LIST")
                            {
                                string me = currentUsername;
                                var friends = GetAcceptedFriends(me);

                                //format: FRIEND_LIST|count|user,online;user,online
                                var sb = new StringBuilder();
                                foreach (var f in friends)
                                {
                                    int on = IsOnline(f) ? 1 : 0;
                                    sb.Append(f).Append(',').Append(on).Append(';');
                                }
                                string payload = sb.Length > 0 ? sb.ToString(0, sb.Length - 1) : "";
                                await SendLineAsync(client, $"FRIEND_LIST|{friends.Count}|{payload}\n");
                                continue;
                            }
                            if (cmd == "FRIEND_LIST_DETAIL")
                            {
                                string me = currentUsername;
                                var cards = GetFriendCards(me); // hàm mới bên dưới

                                // format: FRIEND_LIST_DETAIL|count|u,fullname,elo,win,draw,loss,avatar,online;...
                                var sb = new StringBuilder();
                                foreach (var c in cards)
                                {
                                    // nhớ làm sạch ký tự phân cách
                                    string safeName = SafeField(c.Fullname);
                                    string safeAvatar = SafeField(c.Avatar);

                                    sb.Append(c.FriendKey).Append(',')
                                      .Append(safeName).Append(',')
                                      .Append(c.Elo).Append(',')
                                      .Append(c.Win).Append(',')
                                      .Append(c.Draw).Append(',')
                                      .Append(c.Loss).Append(',')
                                      .Append(safeAvatar).Append(',')
                                      .Append(c.Online ? 1 : 0)
                                      .Append(';');
                                }

                                string payload = sb.Length > 0 ? sb.ToString(0, sb.Length - 1) : "";
                                await SendLineAsync(client, $"FRIEND_LIST_DETAIL|{cards.Count}|{payload}\n");
                                continue;
                            }
                            if (cmd == "FRIEND_INBOX")
                            {
                                string me = currentUsername;
                                var incoming = GetIncomingPending(me);

                                string payload = incoming.Count == 0 ? "" : string.Join(";", incoming);
                                await SendLineAsync(client, $"FRIEND_INBOX|{incoming.Count}|{payload}\n");
                                continue;
                            }
                            if (cmd == "FRIEND_REMOVE")
                            {
                                // FRIEND_REMOVE|target
                                if (parts.Length < 2)
                                {
                                    await SendLineAsync(client, "FRIEND_REMOVE|INVALID\n");
                                    continue;
                                }

                                string me = currentUsername; // username sau login
                                if (string.IsNullOrEmpty(me))
                                {
                                    await SendLineAsync(client, "FRIEND_REMOVE|NOLOGIN\n");
                                    continue;
                                }

                                string target = KeyUser(parts[1]);
                                if (string.IsNullOrEmpty(target))
                                {
                                    await SendLineAsync(client, "FRIEND_REMOVE|INVALID\n");
                                    continue;
                                }

                                if (!UserExists(target))
                                {
                                    await SendLineAsync(client, "FRIEND_REMOVE|NOT_FOUND\n");
                                    continue;
                                }

                                if (!AreFriends(me, target))
                                {
                                    await SendLineAsync(client, "FRIEND_REMOVE|NOT_FRIEND\n");
                                    continue;
                                }

                                RemoveFriendship(me, target);

                                //ACK cho người xóa
                                await SendLineAsync(client, $"FRIEND_REMOVE|OK|{target}\n");

                                // Push cho đối phương nếu online
                                await PushToUserIfOnline(target, $"FRIEND_REMOVED_PUSH|{KeyUser(me)}\n");
                                continue;
                            }
                        }
                        if (parts[0] == "CHAT")
                        {
                            // CHAT|roomId|username|message
                            if (parts.Length < 4) continue;

                            string roomId = (parts[1] ?? "").Trim();
                            string sender = (parts[2] ?? "").Trim();

                            // lấy message phần sau dấu | thứ 3 (đúng như bạn làm)
                            int p1 = msg.IndexOf('|');
                            int p2 = msg.IndexOf('|', p1 + 1);
                            int p3 = msg.IndexOf('|', p2 + 1);
                            if (p3 < 0) continue;
                            string chatMsg = msg.Substring(p3 + 1).Trim();

                            AppendText($"[CHAT] recv sender='{sender}' room='{roomId}' msg='{chatMsg}'");

                            // === 1) Echo lại cho sender (giữ như bạn) ===
                            try
                            {
                                string selfMsg = $"CHAT|{roomId}|{sender}|{chatMsg}\n";
                                byte[] selfData = Encoding.UTF8.GetBytes(selfMsg);
                                await stream.WriteAsync(selfData, 0, selfData.Length);
                            }
                            catch (Exception ex)
                            {
                                AppendText($"[CHAT] Echo fail sender='{sender}': {ex.Message}");
                            }

                            // === 2) Tìm opponent (QUAN TRỌNG: Trim + case-insensitive) ===
                            string opponent = GetOpponentOf(roomId, sender);
                            opponent = (opponent ?? "").Trim();

                            AppendText($"[CHAT] opponent='{opponent}'");

                            // === 3) Gửi cho opponent ===
                            if (!string.IsNullOrEmpty(opponent))
                            {
                                TcpClient oppClient = null;
                                string oppKey = KeyUser(opponent);

                                lock (OnlineUsers)
                                {
                                    AppendText("[CHAT] OnlineUsers keys: " + string.Join(",", OnlineUsers.Keys));
                                    OnlineUsers.TryGetValue(oppKey, out oppClient);
                                }

                                AppendText($"[CHAT] opponent='{opponent}' oppKey='{oppKey}'");

                                if (oppClient != null && oppClient.Connected)
                                {
                                    try
                                    {
                                        string forward = $"CHAT|{roomId}|{sender}|{chatMsg}\n";
                                        byte[] data = Encoding.UTF8.GetBytes(forward);
                                        await oppClient.GetStream().WriteAsync(data, 0, data.Length);

                                        AppendText($"[CHAT] forwarded to '{opponent}' OK");
                                    }
                                    catch (Exception ex)
                                    {
                                        AppendText($"[CHAT] forward fail to '{opponent}': {ex.Message}");
                                    }
                                }
                                else
                                {
                                    AppendText($"[CHAT] opponent '{opponent}' offline or not found in OnlineUsers");
                                }
                            }

                            continue;
                        }

                        if (parts[0].Trim() == "RANDOM")
                        {
                            string chosenRoomId = null;
                            string response;

                            // Lấy trực tiếp 1 RoomID hợp lệ từ DB bằng random ở phía SQL
                            try
                            {
                                using (var conn = Connection.GetSqlConnection())
                                {
                                    conn.Open();
                                    string sql = @"
                                        SELECT TOP 1 RoomID
                                        FROM ROOM
                                        WHERE IsPublic = 1
                                          AND IsClosed = 0
                                          AND RoomIsFull = 0
                                        ORDER BY NEWID();";

                                    using (var cmd = new SqlCommand(sql, conn))
                                    {
                                        object result = cmd.ExecuteScalar();
                                        chosenRoomId = result?.ToString();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                AppendText("[RANDOM] Lỗi truy vấn DB: " + ex.Message);
                                chosenRoomId = null;
                            }

                            // RANDOM|ID hoặc fail nếu không có phòng nào hợp lệ
                            if (string.IsNullOrEmpty(chosenRoomId))
                            {
                                response = "RANDOM|fail\n";
                                AppendText("Danh sách phòng trống");
                                AppendText("RANDOM|fail");
                            }
                            else
                            {
                                response = $"RANDOM|{chosenRoomId}\n";
                            }

                            // Gửi phản hồi về Client
                            byte[] respBytes = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(respBytes, 0, respBytes.Length);

                            AppendText($"{response}");
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
                        OnlineUsers.Remove(KeyUser(currentUsername));
                    }
                }
                UpdateClientList();
                if (!string.IsNullOrEmpty(currentUsername))
                {
                    await NotifyFriendsOnlineState(currentUsername, false);
                }
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
                                response = $"GETINFO|{username}|{fullName}|{elo}|{totalWin}|{totalDraw}|{totalLoss}|{avatar}";
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

        public static string CreateRoom(string hostUsername, string status, string password)
        {
            bool isPublic = status.Equals("public", StringComparison.OrdinalIgnoreCase);
            if (!isPublic)
            {
                password = (password ?? "").Trim();
                if (password.Length != 4) return null;
                for (int i = 0; i < 4; i++)
                    if (!char.IsDigit(password[i])) return null;
            }
            else
            {
                password = null; //public thì không lưu pass
            }
            using (var conn = Connection.GetSqlConnection())
            {
                conn.Open();

                for (int attempts = 0; attempts < 50; attempts++)
                {
                    string roomId = _rand.Next(0, 1_000_000).ToString("D6");
                    string sql = @"INSERT INTO ROOM (RoomID, UsernameHost, NumberPlayer, RoomIsFull, IsPublic, IsClosed, Password) VALUES (@id, @host, @num, 0, @isPublic, 0, @pass);";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@id", SqlDbType.Char, 6).Value = roomId;
                        cmd.Parameters.Add("@host", SqlDbType.VarChar, 50).Value = hostUsername;
                        cmd.Parameters.Add("@num", SqlDbType.Int).Value = 1;
                        cmd.Parameters.Add("@isPublic", SqlDbType.Bit).Value = isPublic;
                        cmd.Parameters.Add("@pass", SqlDbType.Char, 4).Value =
                            (object)password ?? DBNull.Value;
                        try
                        {
                            int rows = cmd.ExecuteNonQuery();
                            if (rows == 1) return roomId;
                        }
                        catch (SqlException ex)
                        {
                            if (ex.Number != 2627) throw; //trùng RoomID thì thử lại
                        }
                    }
                }
                return null;
            }
        }


        public static List<(string RoomID, int NumberPlayer, string HostUsername, bool status, int HostElo)> GetListRooms()
        {
            var list = new List<(string, int, string, bool, int)>();

            using (var conn = Connection.GetSqlConnection())
            {
                conn.Open();
                string sql = @"
            SELECT  R.RoomID, R.NumberPlayer, R.UsernameHost, R.IsPublic, U.ELO
            FROM ROOM R
            JOIN UserDB U ON U.USERNAME = R.UsernameHost";
                using (var cmd = new SqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string roomId = reader["RoomID"].ToString();
                        int numberPlayer = Convert.ToInt32(reader["NumberPlayer"]);
                        string hostUsername = reader["UsernameHost"].ToString();
                        bool isPublic = reader["IsPublic"] != DBNull.Value ? Convert.ToBoolean(reader["IsPublic"]) : false;
                        int hostElo = reader["ELO"] != DBNull.Value ? Convert.ToInt32(reader["ELO"]) : 1200;

                        list.Add((roomId, numberPlayer, hostUsername,isPublic, hostElo));
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
        #region BroadcastMessage
        private async Task BroadcastRoom(string host, string client, string line)
        {
            if (string.IsNullOrEmpty(host) && string.IsNullOrEmpty(client)) return;

            TcpClient Host = null, Client = null;
            lock (OnlineUsers)
            {
                OnlineUsers.TryGetValue(KeyUser(host), out Host);
                OnlineUsers.TryGetValue(KeyUser(client), out Client);
            }

            await SendLineAsync(Host, line);
            if (!string.IsNullOrEmpty(client) && KeyUser(client) != KeyUser(host))
                await SendLineAsync(Client, line);

            AppendText($"[BROADCAST] {line.Trim()}");
        }
        private (string host, string client)? GetRoomUsers(string roomId)
        {
            using var conn = Connection.GetSqlConnection();
            conn.Open();
            using var cmd = new SqlCommand(@"SELECT UsernameHost, UsernameClient FROM ROOM WHERE RoomID=@id", conn);
            cmd.Parameters.AddWithValue("@id", roomId);
            using var info = cmd.ExecuteReader();
            if (!info.Read()) return null;

            return (info["UsernameHost"]?.ToString(), info["UsernameClient"]?.ToString());
        }

        private async Task HandleReadyAsync(string roomId, string currentUsername)
        {
            var users = GetRoomUsers(roomId);
            if (users == null) return;
            var (host, client) = users.Value;

            //chưa đủ 2 người thì vẫn broadcast trạng thái (để UI biết)
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(client))
                return;

            bool shouldStart = false;
            bool hostReady, clientReady, running;

            lock (RoomStateLock)
            {
                if (!RoomState.TryGetValue(roomId, out var st))
                {
                    st = new RoomCountdownState();
                    RoomState[roomId] = st;
                }

                var me = KeyUser(currentUsername);
                if (me == KeyUser(host)) st.HostReady = true;
                else if (me == KeyUser(client)) st.ClientReady = true;

                hostReady = st.HostReady;
                clientReady = st.ClientReady;

                //chỉ start 1 lần
                if (st.HostReady && st.ClientReady && !st.CountdownRunning)
                {
                    st.CountdownRunning = true;
                    st.Cts = new CancellationTokenSource();
                    shouldStart = true;
                }

                running = st.CountdownRunning;
            }

            //gửi msg cho cả 2 
            await BroadcastRoom(host, client,
                $"READY_STATE|{roomId}|{(hostReady ? 1 : 0)}|{(clientReady ? 1 : 0)}|{(running ? 1 : 0)}\n");

            //ko await để ko block luồng đang xử lý
            if (shouldStart)
                _ = RunCountdownAndStartGameAsync(roomId, host, client, 10);
        }

        private async Task HandleUnreadyAsync(string roomId, string currentUsername)
        {
            var users = GetRoomUsers(roomId);
            if (users == null) return;
            var (host, client) = users.Value;

            bool needCancel = false;
            RoomCountdownState st;
            lock (RoomStateLock)
            {
                if (!RoomState.TryGetValue(roomId, out st))
                {
                    st = new RoomCountdownState();
                    RoomState[roomId] = st;
                }
                //1 người UNREADY -> reset cả 2 về 0
                st.HostReady = false;
                st.ClientReady = false;

                if (st.CountdownRunning)
                {
                    needCancel = true;
                    st.Cts?.Cancel(); //countdown task phải có finally reset CountdownRunning/Cts
                }
            }

            //gửi msg cho cả phòng để đồng bộ UI
            await BroadcastRoom(host, client,
                $"READY_STATE|{roomId}|0|0|{(st.CountdownRunning ? 1 : 0)}\n");

            //hủy countdown cho cả phòng
            if (needCancel)
                await BroadcastRoom(host, client, $"COUNTDOWN_CANCEL|{roomId}\n");
        }
        private async Task RunCountdownAndStartGameAsync(string roomId, string host, string client, int seconds)
        {
            long startUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await BroadcastRoom(host, client, $"COUNTDOWN|{roomId}|{seconds}|{startUnixMs}\n");
            CancellationToken token;
            lock (RoomStateLock)
            {
                if (!RoomState.TryGetValue(roomId, out var st) || st.Cts == null) return;
                token = st.Cts.Token;
            }
            try
            {
                await Task.Delay(seconds * 1000, token);
                await SendGameStart(roomId, host, client);
            }
            catch (TaskCanceledException) { }
            finally
            {
                lock (RoomStateLock)
                {
                    if (RoomState.TryGetValue(roomId, out var st))
                    {
                        st.CountdownRunning = false;
                        st.Cts?.Dispose();
                        st.Cts = null;
                    }
                }

                //hủy coutdown để 2 bên UI đồng bộ (không countdown)
                await BroadcastRoom(host, client, $"READY_STATE|{roomId}|0|0|0\n");
            }
        }
        private async Task SendGameStart(string roomId, string host, string client)
        {
            TcpClient Host = null, Client = null;
            lock (OnlineUsers)
            {
                OnlineUsers.TryGetValue(KeyUser(host), out Host);
                OnlineUsers.TryGetValue(KeyUser(client), out Client);
            }

            await SendLineAsync(Host, $"GAME_START|white|{roomId}|{client}\n");
            await SendLineAsync(Client, $"GAME_START|black|{roomId}|{host}\n");

            AppendText($"[GAME_START] {roomId} {host} vs {client}");
        }
        #endregion
        #region cập nhập UI waitngroom broacast
        private static string KeyUser(string u) => (u ?? "").Trim().ToLowerInvariant();
        //đảm bảo (a, b) luôn đúng thứ tự
        private static (string a, string b) NormalizePair(string u1, string u2)
        {
            string x = KeyUser(u1);
            string y = KeyUser(u2);
            if (string.CompareOrdinal(x, y) < 0) return (x, y);
            return (y, x);
        }

        private bool IsOnline(string username)
        {
            lock (OnlineUsers)
                return OnlineUsers.ContainsKey(KeyUser(username));
        }

        private async Task SendLineAsync(TcpClient client, string line)
        {
            if (client == null || !client.Connected) return;
            try
            {
                if (!line.EndsWith("\n")) line += "\n";
                var data = Encoding.UTF8.GetBytes(line);
                await client.GetStream().WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                AppendText($"[SendLineAsync] Lỗi gửi: {ex.Message}");
            }
        }

        private async Task BroadcastRoomUpdateAsync(string roomId)
        {
            AppendText($"[ROOM_UPDATE] Enter roomId={roomId}");

            string host = null, client = null;

            try
            {
                using (var conn = Connection.GetSqlConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"SELECT UsernameHost, UsernameClient FROM ROOM WHERE RoomID=@id", conn);
                    cmd.Parameters.AddWithValue("@id", roomId);

                    using var r = cmd.ExecuteReader();
                    if (!r.Read())
                    {
                        AppendText($"[ROOM_UPDATE] DB không có RoomID={roomId}");
                        return;
                    }

                    host = r["UsernameHost"]?.ToString();
                    client = r["UsernameClient"]?.ToString();
                }

                var hostKey = KeyUser(host);
                var clientKey = KeyUser(client);

                TcpClient hostClient = null, clientClient = null;
                lock (OnlineUsers)
                {
                    OnlineUsers.TryGetValue(KeyUser(hostKey), out hostClient);
                    OnlineUsers.TryGetValue(KeyUser(clientKey), out clientClient);
                }

                AppendText($"[ROOM_UPDATE] host='{host}' client='{client}' hostFound={hostClient != null} clientFound={clientClient != null}");

                string msg = $"ROOM_UPDATE|{roomId}|{host}|{client}\n";
                await BroadcastRoom(host, client, msg);

                AppendText($"[ROOM_UPDATE] Broadcast done: {msg.Trim()}");
            }
            catch (Exception ex)
            {
                AppendText($"[ROOM_UPDATE] EX: {ex}");
            }
        }
        #endregion
        private sealed class RoomCountdownState
        {
            public bool HostReady;
            public bool ClientReady;
            public bool CountdownRunning;
            public CancellationTokenSource Cts;
        }

        private readonly Dictionary<string, RoomCountdownState> RoomState = new();
        private readonly object RoomStateLock = new();
        private async Task HandleOutRoom(string roomid, string username)
        {
            bool pendingBroadcastRoomUpdate = false;
            bool pendingCountdownCancel = false;
            if (string.IsNullOrWhiteSpace(roomid) || string.IsNullOrWhiteSpace(username))
                return;
            roomid = roomid.Trim();
            username = username.Trim();
            bool deleted = false;
            bool changed = false;

            using (var conn = Connection.GetSqlConnection())
            {
                string host = null, client = null;
                int numberplayer = 0;
                conn.Open();
                using var tx = conn.BeginTransaction(System.Data.IsolationLevel.Serializable);

                using (var cmd = new SqlCommand(@"SELECT UsernameHost, UsernameClient, NumberPlayer FROM ROOM WITH (UPDLOCK, ROWLOCK) WHERE RoomID=@id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", roomid);
                    using var read = await cmd.ExecuteReaderAsync();
                    if (!await read.ReadAsync())
                    {
                        tx.Commit();
                        return;
                    }
                    host = read["UsernameHost"]?.ToString();
                    client = read["UsernameClient"]?.ToString();
                    numberplayer = Convert.ToInt32(read["NumberPlayer"]);
                }
                string me = KeyUser(username);
                string hostkey = KeyUser(host);
                string clientkey = KeyUser(client);

                if (me == hostkey)
                {
                    if (string.IsNullOrEmpty(client))
                    {
                        using var del = new SqlCommand("DELETE FROM ROOM WHERE RoomID=@id", conn, tx);
                        del.Parameters.AddWithValue("@id", roomid);
                        await del.ExecuteNonQueryAsync();
                        deleted = true;
                        changed = true;
                    }
                    //còn client thì chuyển client làm host
                    using var update = new SqlCommand(@"UPDATE ROOM
                                                SET UsernameHost   = @newHost,
                                                    UsernameClient = NULL,
                                                    NumberPlayer   = 1,
                                                    RoomIsFull     = 0,
                                                    IsClosed       = 0
                                                WHERE RoomID = @id;", conn, tx);
                    update.Parameters.AddWithValue("@newHost", client);
                    update.Parameters.AddWithValue("@id", roomid);
                    await update.ExecuteNonQueryAsync();
                    changed = true;
                    //reset state ready/countdown trong RAM
                    bool needCancel = false;
                    lock (RoomStateLock)
                    {
                        if (RoomState.TryGetValue(roomid, out var st))
                        {
                            st.HostReady = false;
                            st.ClientReady = false;
                            if (st.CountdownRunning)
                            {
                                needCancel = true;
                                st.Cts?.Cancel();
                            }
                            st.CountdownRunning = false;
                            st.Cts = null;
                        }
                        else
                        {
                            RoomState[roomid] = new RoomCountdownState(); //update state mới sạch
                        }
                    }
                    //commit xong mới broadcast (để client nhận ROOM_UPDATE mới)
                    pendingBroadcastRoomUpdate = true;
                    pendingCountdownCancel = needCancel;
                }
                else if (me == clientkey)
                {
                    using var update = new SqlCommand(@"UPDATE ROOM
                                                        SET UsernameClient=NULL,
                                                            NumberPlayer = CASE WHEN NumberPlayer > 0 THEN NumberPlayer - 1 ELSE 0 END,
                                                            RoomIsFull=0
                                                        WHERE RoomID=@id", conn, tx);
                    update.Parameters.AddWithValue("@id", roomid);
                    await update.ExecuteNonQueryAsync();
                    changed = true;
                    //nếu sau update mà NumberPlayer = 0 thì delete
                    using var check = new SqlCommand(@"SELECT NumberPlayer, UsernameHost, UsernameClient
                                                     FROM ROOM
                                                     WHERE RoomID=@id", conn, tx);
                    check.Parameters.AddWithValue("@id", roomid);
                    using var read = await check.ExecuteReaderAsync();
                    if (await read.ReadAsync())
                    {
                        int n2 = Convert.ToInt32(read["NumberPlayer"]);
                        string h2 = read["UsernameHost"]?.ToString();
                        string c2 = read["UsernameClient"]?.ToString();

                        if (n2 <= 0 || (string.IsNullOrEmpty(h2) && string.IsNullOrEmpty(c2)))
                        {
                            read.Close();
                            using var del = new SqlCommand("DELETE FROM ROOM WHERE RoomID=@id", conn, tx);
                            del.Parameters.AddWithValue("@id", roomid);
                            await del.ExecuteNonQueryAsync();
                            deleted = true;
                        }
                    }
                }
                else
                {
                    tx.Commit(); // user không thuộc room
                    return;
                }
                tx.Commit();
                //dọn state RAM để không dính READY/COUNTDOWN cũ
                lock (RoomStateLock)
                {
                    if (RoomState.TryGetValue(roomid, out var st))
                    {
                        try { st.Cts?.Cancel(); } catch { }
                        try { st.Cts?.Dispose(); } catch { }
                        RoomState.Remove(roomid);
                    }
                }
                if (deleted)
                {
                    AppendText($"[ROOM] Deleted room {roomid} (empty).");
                    return;
                }

                if (changed)
                {
                    await BroadcastRoomUpdateAsync(roomid);
                    AppendText($"[ROOM] OUTROOM handled: {username} left room {roomid}");
                }
            }
        }
        private async Task<bool> CheckRoomPassword(string roomid, string password)
        {
            if (string.IsNullOrEmpty(roomid) || string.IsNullOrEmpty(password))
            {
                return false;
            }
            roomid = roomid.Trim();
            password = password.Trim();
            using (var conn = Connection.GetSqlConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(
                    "SELECT Password FROM ROOM WHERE RoomID = @id AND IsClosed = 0", conn))
                {
                    cmd.Parameters.AddWithValue("@id", roomid);
                    object obj = await cmd.ExecuteScalarAsync(); //lấy 1 giá trị
                    string PassInDb = null;
                    if (obj == null)
                    {
                        PassInDb = null;
                    }
                    else if (obj == DBNull.Value)
                    {
                        PassInDb = null;
                    }
                    else
                    {
                        PassInDb = obj.ToString().Trim();
                    }

                    if (string.IsNullOrEmpty(PassInDb))
                        return false;

                    return string.Equals(password, PassInDb, StringComparison.Ordinal);
                }
            }
        }
        #region Friend
        private bool UserExists(string username)
        {//đã có hàm UsernameExist nhưng trong nó chỉ trả về true hoặc false theo kiểu "tồn tại" nên sẽ có thêm hàm này ở đây để kiểm tra theo cách khác
            username = username.Trim();
            using (var conn = Connection.GetSqlConnection())
            {
                conn.Open();
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM UserDB WHERE Username=@u", conn);
                cmd.Parameters.AddWithValue("@u", username);
                return (int)cmd.ExecuteScalar() > 0;
            }
        }
        //lấy trạng thái quan hệ của 2 user trong bảng nếu có quan hệ bạn bè hoặc đang chờ đồng ý(pending) 
        private (byte status, string RequestedBy)? GetFriendRow(string u1, string u2) //có thể null hoặc ko
        {
            var (a, b) = NormalizePair(u1, u2);
            using (var conn = Connection.GetSqlConnection())
            {
                conn.Open();
                using var cmd = new SqlCommand("SELECT Status, RequestedBy FROM Friendship WHERE UserA=@a AND UserB=@b", conn);
                cmd.Parameters.AddWithValue("@a", a);
                cmd.Parameters.AddWithValue("@b", b);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) return null;

                byte st = Convert.ToByte(r["Status"]);
                string reqby = r["RequestedBy"].ToString();
                return (st, reqby);
            }
        }
        //tạo 1 lời mới kết bạn từ User này đến User kia
        private void InsertPendingInvite(string fromUser, string toUser)
        {
            var (a, b) = NormalizePair(fromUser, toUser);
            string fromKey = KeyUser(fromUser);
            using var conn = Connection.GetSqlConnection();
            conn.Open();
            using var cmd = new SqlCommand(@"INSERT INTO Friendship(UserA, UserB, Status, RequestedBy, CreatedAt, UpdatedAt) VALUES(@a, @b, 0, @request, SYSUTCDATETIME(), SYSUTCDATETIME())", conn);
            cmd.Parameters.AddWithValue("@a", a);
            cmd.Parameters.AddWithValue("@b", b);
            cmd.Parameters.AddWithValue("@request", fromKey);
            cmd.ExecuteNonQuery();
        }
        //đồng ý lời mời kết bạn và chuyển status thành accepted (1)
        private void AcceptInvite(string fromUser, string toUser)
        {
            var (a, b) = NormalizePair(fromUser, toUser);
            using var conn = Connection.GetSqlConnection();
            conn.Open();
            using var cmd = new SqlCommand(@"UPDATE Friendship
                                             SET Status=1, UpdatedAt=SYSUTCDATETIME()
                                             WHERE UserA=@a AND UserB=@b AND Status=0", conn);
            cmd.Parameters.AddWithValue("@a", a);
            cmd.Parameters.AddWithValue("@b", b);
            cmd.ExecuteNonQuery();
        }
        private sealed class FriendCard
        {
            public string FriendKey;
            public string Fullname;
            public int Elo, Win, Draw, Loss;
            public string Avatar;
            public bool Online;
        }

        private static string SafeField(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("|", "/").Replace(",", "/").Replace(";", "/").Replace("\n", " ").Replace("\r", " ");
        }

        private List<FriendCard> GetFriendCards(string username)
        {
            string me = KeyUser(username);
            var list = new List<FriendCard>();

            using var conn = Connection.GetSqlConnection();
            conn.Open();

            // Friendship lưu lower-case key, nên join với LOWER(UserDB.Username)
            using var cmd = new SqlCommand(@"
        SELECT
            FriendKey = CASE WHEN F.UserA=@me THEN F.UserB ELSE F.UserA END,
            U.Fullname,
            U.Elo,
            U.TotalWin,
            U.TotalDraw,
            U.TotalLoss,
            U.Avatar
        FROM Friendship F
        JOIN UserDB U
          ON LOWER(U.Username) = CASE WHEN F.UserA=@me THEN F.UserB ELSE F.UserA END
        WHERE F.Status=1 AND (F.UserA=@me OR F.UserB=@me)
        ORDER BY U.Elo DESC, U.Username ASC;
    ", conn);

            cmd.Parameters.AddWithValue("@me", me);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                string friendKey = r["FriendKey"].ToString();

                var card = new FriendCard
                {
                    FriendKey = friendKey,
                    Fullname = r["Fullname"]?.ToString() ?? "",
                    Elo = r["Elo"] != DBNull.Value ? Convert.ToInt32(r["Elo"]) : 1200,
                    Win = r["TotalWin"] != DBNull.Value ? Convert.ToInt32(r["TotalWin"]) : 0,
                    Draw = r["TotalDraw"] != DBNull.Value ? Convert.ToInt32(r["TotalDraw"]) : 0,
                    Loss = r["TotalLoss"] != DBNull.Value ? Convert.ToInt32(r["TotalLoss"]) : 0,
                    Avatar = r["Avatar"]?.ToString() ?? "",
                    Online = IsOnline(friendKey)
                };

                list.Add(card);
            }

            return list;
        }
        //xử lí từ chối lời mời kết bạn thì sẽ xóa cột đó trong bảng
        private void DeclineInvite(string fromUser, string toUser)
        {
            var (a, b) = NormalizePair(fromUser, toUser);
            using var conn = Connection.GetSqlConnection();
            conn.Open();
            using var cmd = new SqlCommand("DELETE FROM Friendship WHERE UserA=@a AND UserB=@b AND Status=0", conn);
            cmd.Parameters.AddWithValue("@a", a);
            cmd.Parameters.AddWithValue("@b", b);
            cmd.ExecuteNonQuery();
        }
        //lấy danh sách bạn bè của username
        private List<string> GetAcceptedFriends(string username)
        {
            string me = KeyUser(username);
            var list = new List<string>();

            using var conn = Connection.GetSqlConnection();
            conn.Open();
            using var cmd = new SqlCommand(@"SELECT CASE WHEN UserA=@me THEN UserB ELSE UserA END AS Friend
                                             FROM Friendship
                                             WHERE Status=1 AND (UserA=@me OR UserB=@me)", conn);
            cmd.Parameters.AddWithValue("@me", me);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(r["Friend"].ToString());

            return list;
        }
        //lấy danh sách lời mời kết bạn của username
        private List<string> GetIncomingPending(string username)
        {
            string me = KeyUser(username);
            var list = new List<string>();

            using var conn = Connection.GetSqlConnection();
            conn.Open();
            using var cmd = new SqlCommand(@"SELECT CASE WHEN UserA=@me THEN UserB ELSE UserA END AS FromUser
                                             FROM Friendship
                                             WHERE Status=0 AND RequestedBy <> @me AND (UserA=@me OR UserB=@me)", conn);
            cmd.Parameters.AddWithValue("@me", me);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(r["FromUser"].ToString());
            return list;
        }
        //gửi cho tất cả các player đang online về trạng thái của mình
        private async Task NotifyFriendsOnlineState(string username, bool online)
        {
            var friends = GetAcceptedFriends(username);
            if (friends.Count == 0) return;

            string msg = $"FRIEND_STATUS|{KeyUser(username)}|{(online ? 1 : 0)}\n";
            foreach (var f in friends)
            {
                TcpClient c = null;
                lock (OnlineUsers)
                    OnlineUsers.TryGetValue(KeyUser(f), out c);
                await SendLineAsync(c, msg); 
            }
        }
        //gửi trạng thái đến cho người đó nếu người đó đang online
        private async Task PushToUserIfOnline(string username, string line)
        {
            TcpClient c = null;
            lock (OnlineUsers)
                OnlineUsers.TryGetValue(KeyUser(username), out c);

            await SendLineAsync(c, line);
        }
        private void RemoveFriendship(string u1, string u2)
        {
            var (a, b) = NormalizePair(u1, u2);
            using var conn = Connection.GetSqlConnection();
            conn.Open();
            using var cmd = new SqlCommand(@"DELETE FROM Friendship WHERE UserA=@a AND UserB=@b AND Status=1", conn);
            cmd.Parameters.AddWithValue("@a", a);
            cmd.Parameters.AddWithValue("@b", b);
            cmd.ExecuteNonQuery();
        }
        private bool AreFriends(string u1, string u2)
        {
            var (a, b) = NormalizePair(u1, u2); //sort + lowercase để ra đúng PK (UserA<UserB)

            using var conn = Connection.GetSqlConnection();
            conn.Open();

            using var cmd = new SqlCommand(@"
        SELECT COUNT(*)
        FROM Friendship
        WHERE UserA=@a AND UserB=@b AND Status=1", conn);

            cmd.Parameters.AddWithValue("@a", a);
            cmd.Parameters.AddWithValue("@b", b);

            return (int)cmd.ExecuteScalar() > 0;
        }
        #endregion
    }
}
