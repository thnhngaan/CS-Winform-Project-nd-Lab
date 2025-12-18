using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading.Tasks;
using Assets.Scripts;
using UnityEngine.UI;

public class WaitingRoomListener : MonoBehaviour
{
    [Header("Scene khi vào chơi")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("UI trạng thái (optional)")]
    [SerializeField] private TMP_Text statusText;

    [Header("GameObject")]
    [SerializeField] private Button Ready;
    [SerializeField] private Button Cancel;
    [SerializeField] private Button Kick;
    [SerializeField] private Button Back;
    [SerializeField] private TMP_Text player_host;
    [SerializeField] private TMP_Text player_client;
    [SerializeField] private Image avt_host;
    [SerializeField] private Image avt_client;
    [SerializeField] private TMP_Text idroom;

    private bool _listening = false;
    private bool _listenStarted = false;
    private bool _readySent = false;

    private void Awake()
    {
        if (Ready != null)
        {
            Ready.onClick.RemoveListener(Button_Ready_Click); // đỡ xóa listener của script khác
            Ready.onClick.AddListener(Button_Ready_Click);
        }
    }

    private async void Start()
    {
        if (statusText != null)
            statusText.text = "Đang chờ đủ người vào phòng...";

        if (idroom != null)
            idroom.text = GameSession.RoomId;
        string msg = $"GETINFO|{UserSession.CurrentUsername}";
        await NetworkClient.Instance.SendAsync(msg);
        string resp = await NetworkClient.Instance.WaitForPrefixAsync("GETINFO|", 5000);
        string[] parts = resp.Split('|');
        if (parts.Length < 7 || parts[0] != "GETINFO")
        {
            Debug.LogError($"GetInfo: Gói tin sai format. parts.Length = {parts.Length}, parts[0] = {parts[0]}");
            return;
        }
        string fullNameStr = parts[1];
        int elo = int.Parse(parts[2]);
        string avatarPath = parts[6];


    }

    private async void OnEnable()
    {
        _listening = true;
        _listenStarted = false;
        _readySent = false;

        if (Ready != null) Ready.interactable = true;
        await ListenLoop();
    }

    private void OnDisable()
    {
        _listening = false;
    }

    private async void Button_Ready_Click()
    {
        if (_readySent) return;
        _readySent = true;

        if (Ready != null) Ready.interactable = false;

        Debug.Log($"[WaitingRoomListener] ReadyClick - IsConnected={NetworkClient.Instance.IsConnected}, RoomId='{GameSession.RoomId}'");

        if (!NetworkClient.Instance.IsConnected)
        {
            Debug.LogWarning("[WaitingRoomListener] Chưa kết nối server.");
            return;
        }

        if (string.IsNullOrEmpty(GameSession.RoomId))
        {
            Debug.LogWarning("[WaitingRoomListener] RoomId rỗng.");
            return;
        }

        string msg = $"READY|{GameSession.RoomId}";
        await NetworkClient.Instance.SendAsync(msg);
        Debug.Log("[WaitingRoomListener] Sent: " + msg);

        if (!_listenStarted)
        {
            _listenStarted = true;
        
        }
    }

    private async Task ListenLoop()
    {
        while (_listening && NetworkClient.Instance.IsConnected)
        {
            string msg = await NetworkClient.Instance.WaitForPrefixAsync("GAME_START|", 5000);
            if (string.IsNullOrEmpty(msg))
            {
                Debug.LogWarning("[WaitingRoomListener] Timeout/không có dữ liệu.");
                continue;
            }

            msg = msg.Trim();
            HandleServerMessage(msg);

            if (msg.StartsWith("GAME_START|", StringComparison.Ordinal))
                break;
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

            case "GET_INFO":

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