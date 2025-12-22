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

        private string serverIP = "127.0.0.1";// chỉnh sửa IP server nha, để người chơi nhập
        private int Port = 8080;
        
        //hàm event ấn nút đăng nhập nè
        public async void OnLoginButtonClicked()
        {
            string username = usernameInput.text.Trim();
            string password = passwordInput.text.Trim();
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBoxManager.Instance.ShowMessageBox("BÁO LỖI", "Vui lòng nhập tài khoản và mật khẩu");
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
                bool ok = await NetworkClient.Instance.ConnectAsync(IP, Port);
                if (!ok)
                {
                    return "Lỗi: Không kết nối được tới server.";
                }
                await NetworkClient.Instance.SendAsync(message);
                string response = await NetworkClient.Instance.WaitForPrefixAsync("LOGIN|", 5000);
                if (response == null)
                {
                    return "Lỗi: Không nhận được dữ liệu từ server.";
                }
                return response;
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
            if(result.Equals("LOGIN|FAILED_Tài khoản của bạn đã được đăng nhập ở nơi khác"))
            {
                MessageBoxManager.Instance.ShowMessageBox("BÁO LỖI", "TÀI KHOẢN CỦA BẠN ĐÃ ĐƯỢC ĐĂNG NHẬP Ở NƠI KHÁC");
            }
            else if (result.Equals("LOGIN|SUCCESS", System.StringComparison.OrdinalIgnoreCase))
            {
                UserSession1.Username = username;
                Assets.Scripts.UserSession.CurrentUsername = username;
                ShowMessage("Đăng nhập thành công!");
                await Task.Delay(1000);
                SceneManager.LoadScene(NextScene);
            }
            else if (result.Equals("LOGIN|FAILED", System.StringComparison.OrdinalIgnoreCase))
            {
                MessageBoxManager.Instance.ShowMessageBox("BÁO LỖI", "SAI TÀI KHOẢN HOẶC MẬT KHẨU!");
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
        public static class UserSession1
        {
            public static string Username;
        }
    }
}
