<<<<<<< HEAD
﻿using TMPro;
using UnityEngine;
using Assets.Scripts;

public class GameNetworkListener : MonoBehaviour
{
    [SerializeField] private Game game;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameOverScreen gameOverUI;

    private bool _handledGameOver = false;

    private void Awake()
    {
        if (game == null) game = FindObjectOfType<Game>();
        if (gameOverUI == null) gameOverUI = FindObjectOfType<GameOverScreen>();
    }

    private void OnEnable()
    {
        NetworkClient.Instance.OnLine -= HandleServerMessage;
        NetworkClient.Instance.OnLine += HandleServerMessage;
        _handledGameOver = false;
    }

    private void OnDisable()
    {
        if (NetworkClient.Instance != null)
            NetworkClient.Instance.OnLine -= HandleServerMessage;
    }

    private void HandleServerMessage(string msg)
    {
        Debug.Log("[GameNet] Received: " + msg);

        var parts = msg.Trim().Split('|');
        if (parts.Length == 0) return;

        switch (parts[0])
        {
            case "OPP_MOVE":
                HandleOppMove(parts);
                break;

            case "GAME_OVER":
                HandleGameOver(parts);
                break;

                // nếu bạn muốn xử lý GETINFO ngay tại scene game:
                // case "GETINFO":
                //     HandleGetInfo(parts);
                //     break;
        }
    }

    private void HandleOppMove(string[] parts)
    {
        if (parts.Length < 6) return;

        string roomId = parts[1].Trim();
        if (roomId != GameSession.RoomId) return;

        int fromX = int.Parse(parts[2]);
        int fromY = int.Parse(parts[3]);
        int toX = int.Parse(parts[4]);
        int toY = int.Parse(parts[5]);

        game?.ApplyNetworkMove(fromX, fromY, toX, toY);
    }

    private void HandleGameOver(string[] parts)
    {
        //GAME_OVER|roomId|WIN/LOSE|winnerColor
        if (parts.Length < 8) return;

        string roomId = parts[1].Trim();
        if (roomId != GameSession.RoomId) return;

        if (_handledGameOver) return; //tránh show 2 lần
        _handledGameOver = true;

        string result = parts[2].Trim();      //WIN / LOSE (có thể dùng để show text)
        string winnerColor = parts[3].Trim(); //white / black

        //show UI: chỉ cần winnerColor
        gameOverUI?.ShowGameOver(winnerColor);

        //optional: statusText
        if (statusText != null)
            statusText.text = (result == "WIN") ? "You win!" : "You lose!";

        //refresh info
        string u = UserSession.CurrentUsername;
        _ = NetworkClient.Instance.SendAsync($"GETINFO|{u}");
    }
}
=======
﻿using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Assets.Scripts;
using System.Collections;

public class GameNetworkListener : MonoBehaviour // Hàm lắng nghe msg từ các player
{
    [SerializeField] private Game game;
    [SerializeField] private TMP_Text statusText;
    private string MyColor => (GameSession.MyColor ?? "white").Trim().ToLower();
    [SerializeField] private float gameOverDelay = 1.5f;

    private Coroutine _gameOverCo;

    private void Awake() // bắt đầu lắng nghe khi bật scene Game
    {
        if (game == null)
            game = FindObjectOfType<Game>();
    }

    private async void OnEnable() // Lắng nghe
    {
        NetworkClient.Instance.OnLine -= HandleServerMessage;
        NetworkClient.Instance.OnLine += HandleServerMessage;
    }

    private void OnDisable() // Tắt lắng nghe
    {
        NetworkClient.Instance.OnLine -= HandleServerMessage;
    }


    private void HandleServerMessage(string msg)
    {
        // lắng nghe dữ liệu và lọc dữ liệu thành thành các case nhỏ
        Debug.Log("[GameNet] Received: " + msg);
        
        string[] parts = msg.Split('|');
        if (parts.Length == 0) return;

        switch (parts[0])
        {
            case "OPP_MOVE":
                HandleOppMove(parts);
                break;
            case "GAME_OVER":
                HandleGameOver(parts);
                break;
            case "TURN":
                HandleTurn(parts);
                break;
            case "TIME":
                HandleTime(parts);
                break;
            case "TIMEOUT":
                HandleTimeout(parts);
                break;
            case "RESIGNED":
                HandleResigned(parts);
                break;
        }
    }

    private void HandleOppMove(string[] parts) // hàm lắng nghe nước đi của đối thủ
    {
        // nghe msg theo định dạng OPP_MOVE|roomId|fromX|fromY|toX|toY
        if (parts.Length < 6) return;

        string roomId = parts[1];
        if (roomId != GameSession.RoomId) return;

        int fromX = int.Parse(parts[2]);
        int fromY = int.Parse(parts[3]);
        int toX = int.Parse(parts[4]);
        int toY = int.Parse(parts[5]);

        if (game != null)
            game.ApplyNetworkMove(fromX, fromY, toX, toY); // cập nhật scene game 
    }
    private bool _handledGameOver = false;
    private void HandleGameOver(string[] parts)
    {   
        // GAME_OVER|roomId|winnerColor
        if (parts.Length < 3) return;

        string roomId = parts[1].Trim();
        if (roomId != GameSession.RoomId) return;

        if (_handledGameOver) return;
        _handledGameOver = true;

        string winnerColor = parts[2].Trim().ToLower();

        // Delay show gameover để kịp đọc toast
        if (_gameOverCo != null) StopCoroutine(_gameOverCo);
        _gameOverCo = StartCoroutine(ShowGameOverDelayed(winnerColor));
    }

    [SerializeField] private GameOverScreen gameOverUI;
    private IEnumerator ShowGameOverDelayed(string winnerColor)
    {
        yield return new WaitForSecondsRealtime(gameOverDelay);
        gameOverUI?.ShowGameOver(winnerColor);
    }


    [SerializeField] private TMP_Text myTimerText;
    [SerializeField] private TMP_Text oppTimerText;
    private void HandleTurn(string[] parts)
    {
        // TURN|roomId|turnColor|seconds
        if (parts.Length < 4) return;
        if (parts[1].Trim() != GameSession.RoomId) return;

        string turnColor = parts[2].Trim().ToLower();
        if (!int.TryParse(parts[3].Trim(), out int seconds)) seconds = 30;

        string resetText = $"{seconds:0.0}s";

        bool myTurn = (turnColor == MyColor);

        if (myTurn)
        {
            if (myTimerText != null) myTimerText.text = resetText;
        }
        else
        {
            if (oppTimerText != null) oppTimerText.text = resetText;
        }
        // đồng thời set lượt để khóa/mở input
        game?.ForceSetTurn(turnColor);
    }

    private void HandleTime(string[] parts)
    {
        // TIME|roomId|turnColor|remainingMs
        if (parts.Length < 4) return;
        if (parts[1].Trim() != GameSession.RoomId) return;

        string turnColor = parts[2].Trim().ToLower();
        if (!int.TryParse(parts[3].Trim(), out int ms)) return;
        if (ms < 0) ms = 0;

        string t = $"{ms / 1000f:0.0}s";
        bool myTurn = (turnColor == MyColor);

        if (myTurn)
        {
            if (myTimerText != null) myTimerText.text = t;
        }
        else
        {
            if (oppTimerText != null) oppTimerText.text = t;
        }
    }

    private void HandleTimeout(string[] parts)
    {
        // TIMEOUT|roomId|loserColor|winnerColor
        if (parts.Length < 4) return;
        if (parts[1].Trim() != GameSession.RoomId) return;

        string winnerColor = parts[3].Trim().ToLower();
        game?.Winner(winnerColor, false);
    }

    [SerializeField] private StatusResignUI toast;
    private void HandleResigned(string[] parts)
    {
        if (parts.Length < 3) return;
        string roomId = parts[1].Trim();
        if (roomId != GameSession.RoomId) return;

        string who = parts[2].Trim();
        bool isMe = who.Equals(UserSession.CurrentUsername, System.StringComparison.OrdinalIgnoreCase);

        toast?.Show(isMe ? "Đầu hàng" : $"{who} đầu hàng.");
    }

}
>>>>>>> ngan
