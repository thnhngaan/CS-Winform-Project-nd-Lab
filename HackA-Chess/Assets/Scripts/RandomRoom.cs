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
        private string idroom = "";

        [Header("Cấu hình Server")]
        public string ip = "127.0.0.1";
        public int port = 8080;

        [Header("Timeout (ms)")]
        public int connectTimeoutMs = 5000; // thời gian chờ kết nối
        public int readTimeoutMs = 5000;    // thời gian chờ đọc phản hồi

        [Header("UI")]
        [SerializeField] public Button autoJoinButton;


        [Header("Next Scene")]
        [SerializeField] private string waitingroomScene = "WaitingRoom";

        public async void OnAutoJoinClick()
        {
            if (autoJoinButton) autoJoinButton.interactable = false;
            SetStatus("Đang ghép…");

            try
            {
                // RANDOM và nhận response
                string randomResp = await SendReqAsync("RANDOM\n");

                var (randOk, matchId, randMsg) = HandleRandomResponse(randomResp);
                if (!randOk)
                {
                    SetStatus(randMsg);
                    RestoreButton();
                    return;
                }

                // Lưu ID và PlayerPrefs
                idroom = matchId;
                PlayerPrefs.SetString("MatchId", matchId);
                PlayerPrefs.Save();

                SetStatus($"Đã chọn phòng {matchId}. Đang gửi JOIN…");

                // JOIN|id và nhận response
                string joinResp = await SendReqAsync($"JOIN|{matchId}\n");

                var (joinOk, joinMsg) = HandleJoinResponse(joinResp);
                if (!joinOk)
                {
                    SetStatus(joinMsg);
                    RestoreButton();
                    return;
                }

                // Thành công: chuyển scene
                SetStatus($"Gia nhập thành công! ID: {matchId}. Đang chuyển…");
                SceneManager.LoadScene(waitingroomScene, LoadSceneMode.Single);
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


        private async Task<string> SendReqAsync(string request)
        {
            // Bảo đảm request luôn kết thúc bằng newline đúng protocol
            if (!request.EndsWith("\n")) request += "\n";

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
                    // Ghi
                    byte[] toSend = Encoding.UTF8.GetBytes(request);
                    await stream.WriteAsync(toSend, 0, toSend.Length);
                    await stream.FlushAsync();

                    // Đọc với timeout
                    byte[] buffer = new byte[1024];
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

        private (bool ok, string id, string message) HandleRandomResponse(string resp)
        {
            if (string.IsNullOrWhiteSpace(resp))
                return (false, "", "Phản hồi rỗng từ server.");

            // Bỏ \r\n cuối, tách theo '|'
            resp = resp.TrimEnd('\r', '\n').Trim();
            var parts = resp.Split('|');

            if (parts.Length < 2)
                return (false, "", "Phản hồi không hợp lệ: " + resp);

            var head = parts[0].Trim();
            var payload = parts[1].Trim();

            if (!string.Equals(head, "RANDOM", StringComparison.OrdinalIgnoreCase))
                return (false, "", "Phản hồi không hợp lệ (không phải RANDOM): " + resp);

            if (string.Equals(payload, "fail", StringComparison.OrdinalIgnoreCase))
                return (false, "", "Không tìm được phòng để ghép.");

            if (!Regex.IsMatch(payload, @"^\d{6}$"))
                return (false, "", "ID phòng nhận về không hợp lệ: " + payload);

            // Hợp lệ: trả về id
            return (true, payload, "");
        }

        private (bool ok, string message) HandleJoinResponse(string resp)
        {
            if (string.IsNullOrWhiteSpace(resp))
                return (false, "Phản hồi rỗng từ server.");

            resp = resp.TrimEnd('\r', '\n').Trim();
            var parts = resp.Split('|');

            if (parts.Length < 2)
                return (false, "Phản hồi JOIN không hợp lệ: " + resp);

            var head = parts[0].Trim();
            var payload = parts[1].Trim();

            if (!string.Equals(head, "JOIN", StringComparison.OrdinalIgnoreCase))
                return (false, "Phản hồi JOIN không hợp lệ (HEAD khác JOIN): " + resp);

            if (string.Equals(payload, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                return (true, "");

            if (string.Equals(payload, "FAILED", StringComparison.OrdinalIgnoreCase))
                return (false, "Gia nhập thất bại (JOIN|FAILED).");

            return (false, "Phản hồi JOIN không hợp lệ: " + resp);
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
