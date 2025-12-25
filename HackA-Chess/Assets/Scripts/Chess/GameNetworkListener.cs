using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Assets.Scripts;
using System.Collections;

public class GameNetworkListener : MonoBehaviour // Hàm lắng nghe msg từ các player
{
    [SerializeField] private Game game;
    [SerializeField] private TMP_Text statusText;
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
