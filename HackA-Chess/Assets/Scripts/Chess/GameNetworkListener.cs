using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Assets.Scripts;

public class GameNetworkListener : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Game game;
    [SerializeField] private TMP_Text statusText;

    private bool _listening;

    private void Start()
    {
        if (game == null)
            game = FindObjectOfType<Game>();
    }

    private async void OnEnable()
    {
        _listening = true;
        await ListenLoop();
    }

    private void OnDisable()
    {
        _listening = false;
    }

    private async Task ListenLoop()
    {
        while (_listening && NetworkClient.Instance.IsConnected)
        {
            string msg = await NetworkClient.Instance.ReceiveOnceAsync();
            if (string.IsNullOrEmpty(msg))
            {
                Debug.LogWarning("[GameNet] mất kết nối hoặc không có dữ liệu");
                break;
            }

            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                HandleServerMessage(msg);
            });
        }
    }

    private void HandleServerMessage(string msg)
    {
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
        }
    }

    private void HandleOppMove(string[] parts)
    {
        // OPP_MOVE|roomId|fromX|fromY|toX|toY
        if (parts.Length < 6) return;

        string roomId = parts[1];
        if (roomId != GameSession.RoomId) return;

        int fromX = int.Parse(parts[2]);
        int fromY = int.Parse(parts[3]);
        int toX = int.Parse(parts[4]);
        int toY = int.Parse(parts[5]);

        if (game != null)
            game.ApplyNetworkMove(fromX, fromY, toX, toY);
    }

    private void HandleGameOver(string[] parts)
    {
        // GAME_OVER|roomId|winnerColor
        if (parts.Length < 3) return;

        string roomId = parts[1];
        if (roomId != GameSession.RoomId) return;

        string winnerColor = parts[2];
        if (!game.IsGameOver())
            game.Winner(winnerColor);   // chỉ hiển thị UI, không gửi ngược server
    }
}
