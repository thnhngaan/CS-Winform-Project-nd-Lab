using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Assets.Scripts;

public class Game : MonoBehaviour // Hàm quản lí bàn cờ 
{
    public GameObject chesspiece;
    private string myColor = "white"; // màu của mình (server gửi)
    [SerializeField] private TMP_Text infoText;

    private GameObject[,] positions = new GameObject[8, 8]; // Tạo mảng vị trí bàn cờ
    private GameObject[] playerBlack = new GameObject[16];
    private GameObject[] playerWhite = new GameObject[16];

    private string currentPlayer = "white"; // Cho trắng đi trước
    private bool gameOver = false; // đặt cờ cho scene GameOver

    void Start()
    {

        Time.timeScale = 1f;
        string sessionColor = Assets.Scripts.GameSession.MyColor;
        if (!string.IsNullOrEmpty(sessionColor))
        {
            myColor = sessionColor.ToLower();   // "white" hoặc "black"
        }
        playerWhite = new GameObject[]
        { Create("chess_white_rook", 0, 0),Create("chess_white_knight", 1, 0),
        Create("chess_white_bishop", 2, 0), Create("chess_white_queen", 3, 0), Create("chess_white_king", 4, 0),
        Create("chess_white_bishop", 5, 0), Create("chess_white_knight", 6, 0), Create("chess_white_rook", 7, 0),
        Create("chess_white_pawn", 0, 1), Create("chess_white_pawn", 1, 1), Create("chess_white_pawn", 2, 1),
        Create("chess_white_pawn", 3, 1), Create("chess_white_pawn", 4, 1), Create("chess_white_pawn", 5, 1),
        Create("chess_white_pawn", 6, 1), Create("chess_white_pawn", 7, 1) };

        playerBlack = new GameObject[] { Create("chess_black_rook", 0, 7), Create("chess_black_knight",1,7),
                                     Create("chess_black_bishop",2,7), Create("chess_black_queen",3,7), Create("chess_black_king",4,7),
                                     Create("chess_black_bishop",5,7), Create("chess_black_knight",6,7), Create("chess_black_rook",7,7),
                                     Create("chess_black_pawn", 0, 6), Create("chess_black_pawn", 1, 6), Create("chess_black_pawn", 2, 6),
                                     Create("chess_black_pawn", 3, 6), Create("chess_black_pawn", 4, 6), Create("chess_black_pawn", 5, 6),
                                     Create("chess_black_pawn", 6, 6), Create("chess_black_pawn", 7, 6) };

        for (int i = 0; i < playerBlack.Length; i++)
        {
            SetPosition(playerBlack[i]);
            SetPosition(playerWhite[i]);
        }
    }

    public GameObject Create(string name, int x, int y)
    {
        GameObject obj = Instantiate(chesspiece, new Vector3(0, 0, -1), Quaternion.identity);
        Chessman cm = obj.GetComponent<Chessman>();
        cm.name = name;
        cm.SetXBoard(x);
        cm.SetYBoard(y);
        cm.Activate();
        return obj;
    }
    public void SetPosition(GameObject obj)
    {
        Chessman cm = obj.GetComponent<Chessman>();
        positions[cm.GetXBoard(), cm.GetYBoard()] = obj;
    }

    public void SetPositionEmpty(int x, int y)
    {
        positions[x, y] = null;
    }

    public GameObject GetPosition(int x, int y)
    {
        return positions[x, y];
    }

    public bool PositionOnBoard(int x, int y)
    {
        if (x < 0 || y < 0 || x >= positions.GetLength(0) || y >= positions.GetLength(1)) return false;
        return true;
    }

    public string GetCurrentPlayer()
    {
        return currentPlayer;
    }
    public bool IsMyTurn()
    {
        return !gameOver && currentPlayer == myColor;
    }
    public bool IsGameOver()
    {
        return gameOver;
    }

    public void NextTurn()
    {
        if (currentPlayer == "white")
            currentPlayer = "black";
        else
            currentPlayer = "white";

        // OPTIONAL: cập nhật text debug nếu có
        if (infoText != null)
        {
            infoText.text =
                $"You are: {myColor}\n" +
                $"Turn: {currentPlayer}\n" +
                $"Room: {Assets.Scripts.GameSession.RoomId}\n" +
                $"Opp: {Assets.Scripts.GameSession.OpponentName}";
        }

    }
    public void ApplyNetworkMove(int fromX, int fromY, int toX, int toY)
    {
        if (gameOver) return;

        GameObject piece = GetPosition(fromX, fromY);
        if (piece == null)
        {
            Debug.LogWarning($"[ApplyNetworkMove] Không có quân ở ({fromX},{fromY})");
            return;
        }

        Chessman cm = piece.GetComponent<Chessman>();
        GameObject target = GetPosition(toX, toY);

        // Nếu ô đích có quân đối thủ → ăn quân
        if (target != null)
        {
            Chessman targetCm = target.GetComponent<Chessman>();
            if (targetCm.name.Contains("king"))
            {
                // ăn vua → mình thắng
                Winner(cm.player); // không notify server, vì bên kia đã gửi rồi
            }
            Destroy(target);
        }

        // Cập nhật ma trận
        SetPositionEmpty(fromX, fromY);
        cm.SetXBoard(toX);
        cm.SetYBoard(toY);
        cm.SetCoords();
        SetPosition(piece);

        // (chưa xử lý castling / phong cấp, tạm bỏ qua bản đầu)
        NextTurn();
    }
    public void Winner(string playerWinner, bool notifyServer)
    {
        if (gameOver) return;

        gameOver = true;
        currentPlayer = "none";

        if (notifyServer && NetworkClient.Instance != null && NetworkClient.Instance.IsConnected)
        {
            try
            {
                string roomId = GameSession.RoomId;
                string msg = $"GAME_OVER|{roomId}|{playerWinner}";
                _ = NetworkClient.Instance.SendAsync(msg); // fire-and-forget
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Send GAME_OVER error: " + ex.Message);
            }
        }

        gameOverUI.ShowGameOver(playerWinner);
    }
    // overload cũ để code cũ vẫn xài được
    public void Winner(string playerWinner)
    {
        Winner(playerWinner, false);
    }

    public GameOverScreen  gameOverUI;

}
