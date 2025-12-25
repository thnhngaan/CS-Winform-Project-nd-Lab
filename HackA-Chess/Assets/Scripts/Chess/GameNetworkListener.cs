using TMPro;
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
