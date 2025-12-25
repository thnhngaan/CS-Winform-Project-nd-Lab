using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WaitingRoomListener : MonoBehaviour
{
    [Header("Scene khi vào chơi")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("UI trạng thái")]
    [SerializeField] private TMP_Text statustext;

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

    private string HostUsername;
    private string ClientUsername;

    [SerializeField] private TMP_Text CountdownText;
    private int CountdownVersion = 0;
    [SerializeField] private TMP_Text VSText;
    [SerializeField] private TMP_Text StatusHost;
    [SerializeField] private TMP_Text StatusClient;

    [SerializeField] private string DefaultAvatarPath = "Avatar/icons8-avatar-50";

    private readonly HashSet<string> InfoRequested = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private bool ReadySent;

    private void Awake()
    {
        if (Ready != null)
        {
            Ready.onClick.RemoveListener(OnReadyClick);
            Ready.onClick.AddListener(OnReadyClick);
        }
        if (Cancel != null)
        {
            Cancel.onClick.RemoveListener(OnCancelClick);
            Cancel.onClick.AddListener(OnCancelClick);
        }
        if (Back != null)
        {
            Back.onClick.RemoveAllListeners();
            Back.onClick.AddListener(OnLeaveClick);
        }
        //các gói khác Kick, cancel,back
        SetReadyUI(false);
    }
    private void Start()
    {
        if (statustext != null) statustext.text = "Đang tìm đối thủ ...";
        if (idroom != null) idroom.text = GameSession.RoomId;

    }
    private async void OnEnable()
    {
        ReadySent = false;
        Subscribe();
        if (Ready != null) Ready.interactable = true;
        InfoRequested.Clear();

        if (NetworkClient.Instance != null && NetworkClient.Instance.IsConnected && !string.IsNullOrEmpty(GameSession.RoomId))
            await NetworkClient.Instance.SendAsync($"ROOM_INFO|{GameSession.RoomId}");
    }
    private void OnDisable()
    {
        StopCountdown();
        Unsubscribe();
        if (NetworkClient.Instance != null)
            NetworkClient.Instance.OnLine -= OnServerLine;
    }
    private void SetReadyUI(bool isReady)
    {
        if (Ready != null) Ready.gameObject.SetActive(!isReady);
        if (Cancel != null) Cancel.gameObject.SetActive(isReady);
    }
    private CancellationTokenSource demoCTS;
    private async void OnReadyClick()
    {
        if (ReadySent) return;
        ReadySent = true;
        SetReadyUI(true);

        if (NetworkClient.Instance == null || !NetworkClient.Instance.IsConnected)
        {
            if (statustext != null) statustext.text = "Mất kết nối server!";
            ReadySent = false;
            SetReadyUI(false);
            return;
        }

        await NetworkClient.Instance.SendAsync($"READY|{GameSession.RoomId}");
    }
    private async void OnLeaveClick()
    {
        if (NetworkClient.Instance == null || !NetworkClient.Instance.IsConnected) return;
        if (string.IsNullOrEmpty(GameSession.RoomId)) return;

        await NetworkClient.Instance.SendAsync($"OUTROOM|{GameSession.RoomId}");
        Unsubscribe();

        SceneManager.LoadScene("JoinRoom");
    }
    private async void OnCancelClick()
    {
        if (!ReadySent) return;
        ReadySent = false;

        SetReadyUI(false);

        if (NetworkClient.Instance != null && NetworkClient.Instance.IsConnected)
            await NetworkClient.Instance.SendAsync($"UNREADY|{GameSession.RoomId}");

        StopCountdown(); 
    }


    private async void RequestInfo(string username)
    {
        if (string.IsNullOrEmpty(username)) return;
        if (NetworkClient.Instance == null || !NetworkClient.Instance.IsConnected) return;

        if (!InfoRequested.Add(username)) return; 
        await NetworkClient.Instance.SendAsync($"GETINFO|{username}");
    }

    private void OnServerLine(string msg)
    {
        msg = msg?.Trim();
        if (string.IsNullOrEmpty(msg)) return;
        Debug.Log($"Đây là msg của bạn {msg}");
        var parts = msg.Split('|');
        if (parts.Length == 0) return;

        switch (parts[0])
        {
            case "ROOM_UPDATE":
                HandleRoomUpdate(parts);
                break;

            case "GETINFO":
                HandleGetInfo(parts);
                break;

            case "GAME_START":
                HandleGameStart(parts);
                StopCountdown();

                break;
            case "COUNTDOWN":
                ReadySent = true;
                SetReadyUI(true);      //cho cancel nếu muốn
                HandleCountdown(parts);
                break;
            case "COUNTDOWN_CANCEL":
                ReadySent = false;
                StopCountdown();
                SetReadyUI(false);
                break;
            case "READY_STATE":
                HandleReadyState(parts);
                break;
        }
    }
    private void HandleReadyState(string[] parts)
    {
        //READY_STATE|roomId|hostReady|clientReady|countdownRunning
        if (parts.Length < 5) return;
        if (!string.Equals(parts[1], GameSession.RoomId, StringComparison.OrdinalIgnoreCase)) return;

        bool hostReady = parts[2] == "1";
        bool clientReady = parts[3] == "1";
        bool running = parts[4] == "1";

        //Update ui
        if (StatusHost != null)
            StatusHost.text = hostReady ? "SẴN SÀNG" : "CHƯA SẴN SÀNG";

        if (StatusClient != null)
            StatusClient.text = clientReady ? "SẴN SÀNG" : "CHƯA SẴN SÀNG";
    }
    private void HandleCountdown(string[] parts)
    {
        // COUNTDOWN|roomId|10|startUnixMs
        if (parts.Length < 4) return;
        if (parts[1] != GameSession.RoomId) return;

        int seconds = int.Parse(parts[2]);
        long startUnixMs = long.Parse(parts[3]);

        StartCountdown(seconds, startUnixMs);
    }
    private void StartCountdown(int seconds, long startUnixMs)
    {
        StopCountdown();
        if (statustext) statustext.text = "GAME SẮP BẮT ĐẦU";
        CountdownVersion++;
        int ver =CountdownVersion;
        VSText.gameObject.SetActive(false);
        demoCTS = new CancellationTokenSource();
        _ = CountdownAsync(seconds, startUnixMs, demoCTS.Token);
    }

    private void StopCountdown()
    {
        demoCTS?.Cancel();
        demoCTS?.Dispose();
        if (VSText != null)
        {
            VSText.gameObject.SetActive(true);
        }
        demoCTS = null;

        statustext.text = "";
        if (CountdownText != null)
            CountdownText.gameObject.SetActive(false);
    }
    private async Task CountdownAsync(int seconds, long startUnixMs, CancellationToken ct)
    {
        if (CountdownText == null) return;
        CountdownText.gameObject.SetActive(true);
        try
        {
            while (true)
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                double elapsed = (now - startUnixMs) / 1000.0;
                int remain = Mathf.CeilToInt((float)(seconds - elapsed));

                if (remain <= 0) break;

                CountdownText.text = $"{remain}";
                await Task.Delay(100, ct); // update mượt

            }

          
        }
        catch (TaskCanceledException) { }
    }


    private void HandleRoomUpdate(string[] parts)
    {
        if (parts.Length < 4) return;
        string roomid = parts[1];
        if (!string.Equals(roomid, GameSession.RoomId, StringComparison.OrdinalIgnoreCase))
            return;
        string NewHost = parts[2]?.Trim() ?? "";
        string NewClient = parts[3]?.Trim() ?? "";

        string OldHost = HostUsername ?? "";
        string OldClient = ClientUsername ?? "";

        //client cũ lên host, client slot trống
        bool promoted = !string.IsNullOrEmpty(OldClient) && NewHost.Equals(OldClient, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(NewClient);

        HostUsername = NewHost;
        ClientUsername =NewClient;
        if (idroom) idroom.text = roomid;
        //setactive(true,false) theo sự tồn tại của host,client
        if (StatusHost) StatusHost.gameObject.SetActive(!string.IsNullOrEmpty(HostUsername));
        if (StatusClient) StatusClient.gameObject.SetActive(!string.IsNullOrEmpty(ClientUsername));

        if (promoted)
        {
            //chuyển UI slot client lên slot host
            MoveClientSlotToHostSlot();
            //OldHost rời nên remove để lần sau nếu quay lại -> GETINFO lại
            if (!string.IsNullOrEmpty(OldHost))
                InfoRequested.Remove(OldHost);
            //clear slot trống
            ClearClientSlot();
        }
        else
        {
            //nếu client thay đổi (rời/đổi user) thì remove để lần sau xin lại
            if (!string.IsNullOrEmpty(OldClient) && !OldClient.Equals(NewClient, StringComparison.OrdinalIgnoreCase))
            {
                InfoRequested.Remove(OldClient);
                ClearClientSlot();
            }
        
            //chỉ reset placeholder nếu muốn, rồi request info host mới
            if (!string.IsNullOrEmpty(OldHost) && !OldHost.Equals(NewHost, StringComparison.OrdinalIgnoreCase))
            {
                InfoRequested.Remove(OldHost);
                //set placeholder host trước khi info về
                SetHostPlaceholder();
            }
        }
        //xin info
        if (!string.IsNullOrEmpty(HostUsername)) RequestInfo(HostUsername);
        if (!string.IsNullOrEmpty(ClientUsername)) RequestInfo(ClientUsername);

        //enable/disable nút button theo đã đủ người hay chưa
        if (Ready != null) Ready.interactable = !string.IsNullOrEmpty(ClientUsername);
        if (StatusHost && !string.IsNullOrEmpty(HostUsername) && string.IsNullOrEmpty(StatusHost.text))
            StatusHost.text = "CHƯA SẴN SÀNG";
        if (StatusClient)
            StatusClient.text = string.IsNullOrEmpty(ClientUsername) ? "" : "CHƯA SẴN SÀNG";
    }
    private void MoveClientSlotToHostSlot()
    {
        if (player_host && player_client) player_host.text = player_client.text;
        if (avt_host && avt_client) avt_host.sprite = avt_client.sprite;

        if (StatusHost && StatusClient)
        {
            StatusHost.gameObject.SetActive(true);
            StatusHost.text = string.IsNullOrEmpty(StatusClient.text) ? "CHƯA SẴN SÀNG" : StatusClient.text;
        }
    }
    private void SetHostPlaceholder()
    {
        if (player_host) player_host.text = "";
        if (avt_host) avt_host.sprite = LoadAvatar("Avatar/icons8-wait-40");
        if (StatusHost)
        {
            StatusHost.gameObject.SetActive(!string.IsNullOrEmpty(HostUsername));
            StatusHost.text = "CHƯA SẴN SÀNG";
        }
    }
    private void ClearClientSlot()
    {
        if (player_client) player_client.text = "";
        if (avt_client) avt_client.sprite = LoadAvatar("Avatar/icons8-wait-40");

        if (StatusClient)
        {
            StatusClient.text = "";
            StatusClient.gameObject.SetActive(false);
        }
    }
    private void ClearHostSlot()
    {
        if (player_host) player_host.text = "";
        if (avt_host) avt_host.sprite = LoadAvatar("Avatar/icons8-wait-40");

        if (StatusHost)
        {
            StatusHost.text = "";
            StatusHost.gameObject.SetActive(false);
        }
    }


    private void HandleGetInfo(string[] parts)
    {
        if (parts.Length < 8) return;

        string username = parts[1];
        string fullname = parts[2];
        int elo = int.TryParse(parts[3], out var e) ? e : 0;
        Sprite avatar = LoadAvatar(parts[7]);

        bool isHost = !string.IsNullOrEmpty(HostUsername) && username.Equals(HostUsername, StringComparison.OrdinalIgnoreCase);
        bool isClient = !string.IsNullOrEmpty(ClientUsername) && username.Equals(ClientUsername, StringComparison.OrdinalIgnoreCase);
        if (isHost)
        {
            if (player_host) player_host.text = $"{fullname}  |  {elo}";
            if (avt_host) avt_host.sprite = avatar;
        }
        else if (isClient)
        {
            if (player_client) player_client.text = $"{fullname}  |  {elo}";
            if (avt_client) avt_client.sprite = avatar;
        }
    }

    private Sprite LoadAvatar(string path)
    {
        string p = string.IsNullOrEmpty(path) ? DefaultAvatarPath : path;
        var sp = Resources.Load<Sprite>(p);
        if (sp == null)
        {
            Debug.LogWarning("Không load được avatar: " + p + " -> fallback default");
            sp = Resources.Load<Sprite>(DefaultAvatarPath);
        }
        return sp;
    }

    private void HandleGameStart(string[] parts)
    { 
        // GAME_START|{color}|{roomId}|{opponentName}
        if (parts.Length < 4) return;
        GameSession.MyColor = parts[1];
        GameSession.RoomId = parts[2];
        GameSession.OpponentName = parts[3];
        Unsubscribe();
        StopCountdown();
        if (statustext != null) statustext.text = "BẮT ĐẦU";
        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }
    private void OnDestroy()
    {
        Unsubscribe();
        demoCTS?.Cancel();
    }
    private bool Subscribed;
    private void Subscribe()
    {
        if (Subscribed) return;
        if (NetworkClient.Instance == null) return;
        NetworkClient.Instance.OnLine -= OnServerLine; //tránh double-sub
        NetworkClient.Instance.OnLine += OnServerLine;
        Subscribed = true;
    }
    private void Unsubscribe()
    {
        if (!Subscribed) return;
        if (NetworkClient.Instance == null) return;

        NetworkClient.Instance.OnLine -= OnServerLine;
        Subscribed = false;
    }

}
