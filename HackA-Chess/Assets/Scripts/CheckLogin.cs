using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using UnityEngine.SceneManagement;
using TMPro;
using Assets.Scripts;

namespace Assets.Scripts
{
    public class CheckLogin : MonoBehaviour
    {
        [Header("UI Controls")]
        public TMP_InputField usernameInput;
        public TMP_InputField passwordInput;
        public TMP_Text messageText;

        [Header("Next Scene / Panel")]
        public string NextScene;

        private string serverIP = "127.0.0.1";
        private int Port = 8080;

        //hàm event ấn nút đăng nhập nè
        public async void OnLoginButtonClicked()
        {
            string username = usernameInput.text.Trim();
            string password = passwordInput.text.Trim();
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowMessage("Vui lòng nhập tài khoản và mật khẩu.");
                return;
            }

            string hashedPassword = HashToSHA256(password);
            await LoginAsync(username, hashedPassword); //chạy async mà không block main thread
        }

        //hàm để mã hóa mật khẩu 
        private string HashToSHA256(string input)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = sha.ComputeHash(bytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in hash)
                    sb.Append(b.ToString("x2")); //hex chữ thường
                return sb.ToString();
            }
        }

        //gửi thông điệp đến server
        private async Task<string> SendMessageAsync(string message, string IP, int Port)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(IP, Port);
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] data = Encoding.UTF8.GetBytes(message);
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[2048];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                        return response;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"TCP Error (IP {IP}) (Port {Port}): {ex.Message}");
                return $"Lỗi: {ex.Message}";
            }
        }

        //nhận msg từ server
        private async Task LoginAsync(string username, string hashedPassword)
        {
            string message = $"LOGIN|{username}|{hashedPassword}";
            string result = await SendMessageAsync(message, serverIP, Port);
            if (string.IsNullOrEmpty(result))
            {
                ShowMessage("Không nhận được phản hồi từ server.");
                return;
            }
            //xử lý phản hồi từ server
            result = result.Trim();
            if (result.Equals("Login success", System.StringComparison.OrdinalIgnoreCase))
            {
                ShowMessage("Đăng nhập thành công!");
                await Task.Delay(1000);
                SceneManager.LoadScene(NextScene);
            }
            else if (result.Equals("Login failed", System.StringComparison.OrdinalIgnoreCase))
            {
                ShowMessage("Sai tài khoản hoặc mật khẩu.");
            }
            else
            {
                ShowMessage($"Phản hồi lạ từ server: {result}");
            }
        }
        private void ShowMessage(string msg)
        {
            if (messageText != null)
                messageText.text = msg;
            else
                Debug.Log(msg);
        }
    }
}
