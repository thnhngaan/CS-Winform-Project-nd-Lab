using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Assets.Scripts;

public class GameNetworkListener : MonoBehaviour // Hàm lắng nghe msg từ các player
{
    [SerializeField] private Game game;
    [SerializeField] private TMP_Text statusText;


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

    private void HandleGameOver(string[] parts) // hàm kết thúc game
    {
        // nghe msg theo định dạng GAME_OVER|roomId|winnerColor
        if (parts.Length < 3) return;

        string roomId = parts[1];
        if (roomId != GameSession.RoomId) return;

        string winnerColor = parts[2];
        if (!game.IsGameOver())
            game.Winner(winnerColor);  // chỉ hiển thị UI, không gửi ngược server
    }
}
