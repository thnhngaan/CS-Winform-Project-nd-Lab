using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading.Tasks;
using Assets.Scripts;

public class WaitingRoomListener : MonoBehaviour
{
    [Header("Scene khi vào chơi")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("UI trạng thái (optional)")]
    [SerializeField] private TMP_Text statusText;

    private bool _listening = false;

    private void Start()
    {
        if (statusText != null)
        {
            statusText.text = "Đang chờ đủ người vào phòng...";
        }
    }

    private async void OnEnable()
    {
        _listening = true;

        // Log để biết scene này có chạy hay không, và RoomId hiện tại là gì
        Debug.Log($"[WaitingRoomListener] OnEnable - IsConnected={NetworkClient.Instance.IsConnected}, RoomId='{GameSession.RoomId}'");

        if (NetworkClient.Instance.IsConnected)
        {
            if (string.IsNullOrEmpty(GameSession.RoomId))
            {
                Debug.LogWarning("[WaitingRoomListener] RoomId đang rỗng nên chưa gửi READY được.");
            }
            else
            {
                string msg = $"READY|{GameSession.RoomId}";
                await NetworkClient.Instance.SendAsync(msg);
                Debug.Log("[WaitingRoomListener] Sent: " + msg);
            }
        }

        await ListenLoop();
    }

    private void OnDisable()
    {
        _listening = false;
    }

    private async Task ListenLoop()
    {
        while (NetworkClient.Instance.IsConnected)
        {
            string msg = await NetworkClient.Instance.ReceiveOnceAsync();
            if (string.IsNullOrEmpty(msg))
            {
                Debug.LogWarning("[WaitingRoomListener] Không nhận được dữ liệu hoặc server đóng kết nối.");
                break;
            }

            // Kiểm tra trước xem đây có phải GAME_START không
            bool isGameStart = msg.StartsWith("GAME_START", StringComparison.Ordinal);

            // Đẩy xử lý về main thread
            HandleServerMessage(msg);

            // Nếu đã nhận GAME_START rồi thì NÓI TẠM BIỆT, không đọc thêm nữa.
            if (isGameStart)
            {
                Debug.Log("[WaitingRoomListener] Đã nhận GAME_START, dừng ListenLoop để GameScene nhận các gói tiếp theo.");
                break;
            }
        }
    }

    private void HandleServerMessage(string msg)
    {
        Debug.Log("[WaitingRoomListener] Received: " + msg);
        string[] parts = msg.Split('|');
        if (parts.Length == 0) return;

        switch (parts[0])
        {
            case "GAME_START":
                HandleGameStart(parts);
                break;

            // sau này có thể thêm case khác: CHAT, KICK, ...
            default:
                Debug.Log("[WaitingRoomListener] Msg không xử lý: " + msg);
                break;
        }
    }

    private void HandleGameStart(string[] parts)
    {
        // Format server gửi: GAME_START|{color}|{roomId}|{opponentName}
        if (parts.Length < 4) return;

        string myColor = parts[1];
        string roomId = parts[2];
        string opponentName = parts[3];

        GameSession.MyColor = myColor;
        GameSession.RoomId = roomId;
        GameSession.OpponentName = opponentName;

        Debug.Log($"[WaitingRoomListener] GAME_START: color={myColor}, room={roomId}, opp={opponentName}");

        if (statusText != null)
            statusText.text = "Bắt đầu ván cờ...";

        // Chuyển sang GameScene
        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }
}
