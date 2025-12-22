
using System;
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
        [Header("UI")]
        [SerializeField] public Button autoJoinButton;

        [Header("Next Scene")]
        [SerializeField] private string waitingroomScene = "WaitingRoom";

        [Header("Timeout (ms)")]
        public int requestTimeoutMs = 5000; // chờ phản hồi mỗi lệnh

        private void SetStatus(string msg) => Debug.Log(msg);
        private void RestoreButton() { if (autoJoinButton) autoJoinButton.interactable = true; }

        // --- ENTRY từ Button ---
        public async void OnAutoJoinClick()
        {
            if (autoJoinButton) autoJoinButton.interactable = false;
            SetStatus("Đang ghép ngẫu nhiên…");

            try
            {
                // Kiểm tra trạng thái kết nối đã có sẵn
                if (!IsConnected())
                {
                    SetStatus("Chưa có kết nối TCP tới server (NetworkClient chưa sẵn sàng).");
                    RestoreButton();
                    return;
                }

                // RANDOM
                var (randOk, matchId, randMsg) = await DoRandomAsync();
                if (!randOk)
                {
                    SetStatus(randMsg);
                    RestoreButton();
                    return;
                }

                // JOIN
                SetStatus($"Đã chọn phòng {matchId}. Đang JOIN…");
                var joinOk = await DoJoinAsync(matchId);
                if (!joinOk)
                {
                    SetStatus("JOIN thất bại.");
                    RestoreButton();
                    return;
                }

                PlayerPrefs.SetString("MatchId", matchId);
                PlayerPrefs.Save();
                SetStatus($"Gia nhập thành công! ID: {matchId}. Chuyển scene…");
                SceneManager.LoadScene(waitingroomScene, LoadSceneMode.Single);
            }
            catch (TimeoutException tex)
            {
                SetStatus("Hết thời gian chờ: " + tex.Message);
                RestoreButton();
            }
            catch (Exception ex)
            {
                SetStatus("Lỗi: " + ex.Message);
                RestoreButton();
            }
        }

        // Sử dụng NetworkClient.Instance đã kết nối
        private bool IsConnected()
        {
            return NetworkClient.Instance != null && NetworkClient.Instance.IsConnected;
        }

        private async Task<(bool ok, string id, string message)> DoRandomAsync()
        {
            await NetworkClient.Instance.SendAsync("RANDOM\n");

            // Chờ phản hồi bắt đầu bằng "RANDOM|"
            string resp = await NetworkClient.Instance.WaitForPrefixAsync("RANDOM|", requestTimeoutMs);
            if (resp == null)
                throw new TimeoutException($"Không nhận phản hồi RANDOM trong {requestTimeoutMs}ms");

            resp = resp.Trim();         
            var parts = resp.Split('|');       
            if (parts.Length < 2)
                return (false, "", "Phản hồi RANDOM không hợp lệ: " + resp);

            var head = parts[0].Trim();
            var payload = parts[1].Trim();

            if (!string.Equals(head, "RANDOM", StringComparison.OrdinalIgnoreCase))
                return (false, "", "Phản hồi không hợp lệ (không phải RANDOM): " + resp);

            if (string.Equals(payload, "fail", StringComparison.OrdinalIgnoreCase))
                return (false, "", "Không tìm được phòng để ghép.");

            // ID 6 chữ số
            if (!Regex.IsMatch(payload, @"^\d{6}$"))
                return (false, "", "ID phòng nhận về không hợp lệ: " + payload);

            return (true, payload, "");
        }

        private async Task<bool> DoJoinAsync(string matchId)
        {
            await NetworkClient.Instance.SendAsync($"JOIN|{matchId}\n");

            // Chờ phản hồi bắt đầu bằng "JOIN|"
            string resp = await NetworkClient.Instance.WaitForPrefixAsync("JOIN|", requestTimeoutMs);
            if (resp == null)
                throw new TimeoutException($"Không nhận phản hồi JOIN trong {requestTimeoutMs}ms");

            resp = resp.Trim();        
            var parts = resp.Split('|');
            if (parts.Length < 2) return false;

            var head = parts[0].Trim();
            var payload = parts[1].Trim();

            if (!string.Equals(head, "JOIN", StringComparison.OrdinalIgnoreCase))
                return false;

            return string.Equals(payload, "SUCCESS", StringComparison.OrdinalIgnoreCase);
        }
    }
}

