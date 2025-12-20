
using System;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class RandomRoom : MonoBehaviour
    {
        string idroom = "";

        [Header("Cấu hình Server")]
        public string ip = "127.0.0.1";
        public int port = 8080;

        [Header("Timeout")]
        public int connectTimeoutMs = 5000; // thời gian chờ kết nối
        public int readTimeoutMs = 5000; // thời gian chờ đọc phản hồi

        [Header("UI")]
        [SerializeField] public Button autoJoinButton;


        [Header("Tên Scene Đích")]
        [SerializeField] private string waitingroomScene = "WaitingRoom";


        public async void OnAutoJoinClick()
        {
            if (autoJoinButton) autoJoinButton.interactable = false;
            SetStatus("Đang ghép…");

            try
            {
                string resp = await SendRandomAsync();
                HandleResponse(resp);
            }
            catch (TimeoutException tex)
            {
                SetStatus("Hết thời gian chờ: " + tex.Message);
                RestoreButton();
            }
            catch (Exception ex)
            {
                SetStatus("Lỗi kết nối: " + ex.Message);
                RestoreButton();
            }
        }

        private async Task<string> SendRandomAsync()
        {
            using (var client = new TcpClient())
            {
                // Timeout cho CONNECT
                var connectTask = client.ConnectAsync(ip, port);
                var connectDelay = Task.Delay(connectTimeoutMs);
                var connectWinner = await Task.WhenAny(connectTask, connectDelay);

                if (connectWinner != connectTask || !client.Connected)
                    throw new TimeoutException($"Không thể kết nối {ip}:{port} trong {connectTimeoutMs}ms");

                using (var stream = client.GetStream())
                {
                    // Gửi req RANDOM (không newline, đúng protocol server)
                    byte[] toSend = Encoding.UTF8.GetBytes("RANDOM");
                    await stream.WriteAsync(toSend, 0, toSend.Length);
                    await stream.FlushAsync();

                    // Timeout cho READ
                    byte[] buffer = new byte[128];
                    var readTask = stream.ReadAsync(buffer, 0, buffer.Length);
                    var readDelay = Task.Delay(readTimeoutMs);
                    var readWinner = await Task.WhenAny(readTask, readDelay);

                    if (readWinner != readTask)
                        throw new TimeoutException($"Không nhận phản hồi từ server trong {readTimeoutMs}ms");

                    int bytes = readTask.Result;
                    if (bytes <= 0)
                        throw new System.IO.IOException("Server đóng kết nối mà không gửi dữ liệu.");

                    return Encoding.UTF8.GetString(buffer, 0, bytes).Trim();
                }
            }
        }

        private void HandleResponse(string resp)
        {
            if (string.IsNullOrWhiteSpace(resp))
            {
                SetStatus("Phản hồi rỗng từ server.");
                RestoreButton();
                return;
            }

            var parts = resp.Trim().Split('|');
            if (parts.Length != 2)
            {
                SetStatus("Phản hồi không hợp lệ: " + resp);
                RestoreButton();
                return;
            }

            var head = parts[0].Trim();
            var payload = parts[1].Trim();

            // Kiểm tra part[0] RANDOM
            if (!string.Equals(head, "RANDOM", StringComparison.OrdinalIgnoreCase))
            {
                SetStatus("Phản hồi không hợp lệ (không phải RANDOM): " + resp);
                RestoreButton();
                return;
            }

            // TH: fail
            if (string.Equals(payload, "fail", StringComparison.OrdinalIgnoreCase))
            {
                SetStatus("Gia nhập không thành công.");
                RestoreButton();
                return;
            }

            // TH: id 6 số
            if (Regex.IsMatch(payload, @"^\d{6}$"))
            {
                string matchId = payload;
                if (string.IsNullOrWhiteSpace(matchId)) idroom = matchId;

                PlayerPrefs.SetString("MatchId", matchId);
                PlayerPrefs.Save();

                SetStatus($"Ghép thành công! ID: {matchId}. Đang chuyển…");
                SceneManager.LoadScene(waitingroomScene, LoadSceneMode.Single);
                return;
            }

            // Các trường hợp khác
            SetStatus("Phản hồi không hợp lệ: " + resp);
            RestoreButton();
        }

        private void SetStatus(string msg)
        {
            Debug.Log(msg);
        }

        private void RestoreButton()
        {
            if (autoJoinButton) autoJoinButton.interactable = true;
        }
    }
}
