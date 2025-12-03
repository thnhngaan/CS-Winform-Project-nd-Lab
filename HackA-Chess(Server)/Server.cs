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
using Microsoft.Data.SqlClient;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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

        private void AddUsertoDatabase(string username, string password, string email, string fullname, string sdt,int elo=1200,int totalwin=0,int totaldraw=0,int totalloss=0,string AVATAR="")
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
                                AddUsertoDatabase(Username, Password, Email, Fullname, Sdt,1200,0,0,0,"Avatar/icons8-avatar-50");
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
                        if(parts[0] == "GETINFO")
                        {
                            string username = parts[1];
                            //truy vấn sql từ username
                            string response = GetUserInfoFromDatabase(username);

                            //gửi về client
                            byte[] data = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(data, 0, data.Length);
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

        private void tb_ipserver_TextChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void rtb_clientsconnection_TextChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}
