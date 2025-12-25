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
            myColor = sessionColor.ToLower();  // tạo session để phân biệt player "white" hoặc "black"
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

    public GameObject Create(string name, int x, int y) // hàm khởi tạo bàn cờ
    {
        GameObject obj = Instantiate(chesspiece, new Vector3(0, 0, -1), Quaternion.identity);
        Chessman cm = obj.GetComponent<Chessman>();
        cm.name = name;
        cm.SetXBoard(x);
        cm.SetYBoard(y);
        cm.Activate();
        return obj;
    }

    public void SetPosition(GameObject obj) // cập nhật vị trí quân cờ
    {
        Chessman cm = obj.GetComponent<Chessman>();
        positions[cm.GetXBoard(), cm.GetYBoard()] = obj;
    }

    public void SetPositionEmpty(int x, int y)
    {
        positions[x, y] = null;
    }

    public GameObject GetPosition(int x, int y) // lấy vị trí của quân cờ
    {
        return positions[x, y];
    }

    public bool PositionOnBoard(int x, int y) // kiểm tra vị trí của quân cờ
    {
        if (x < 0 || y < 0 || x >= positions.GetLength(0) || y >= positions.GetLength(1)) return false;
        return true;
    }

    public string GetCurrentPlayer() // Cập nhật người chơi hiện tại
    {
        return currentPlayer;
    }
    public bool IsMyTurn()
    {
        return !gameOver && currentPlayer == myColor;
    }
    public bool IsGameOver() // Kiểm tra GameOver
    {
        return gameOver;
    }

    public void NextTurn()
    {
        if (currentPlayer == "white")
            currentPlayer = "black";
        else
            currentPlayer = "white";

        if (infoText != null)
        {
            infoText.text =
                $"You are: {myColor}\n" +
                $"Turn: {currentPlayer}\n" +
                $"Room: {Assets.Scripts.GameSession.RoomId}\n" +
                $"Opp: {Assets.Scripts.GameSession.OpponentName}";
        }

    }

    public void ApplyNetworkMove(int fromX, int fromY, int toX, int toY) // hàm cập nhập nước cờ qua network
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

        //Castling 
        if (cm.name.Contains("king") && Mathf.Abs(toX - fromX) == 2)
        {
            int rookFromX, rookToX;
            int y = fromY; // vua ở hàng nào thì xe cũng hàng đó

            if (toX > fromX)
            {
                rookFromX = 7;
                rookToX = 5;
            }
            else
            {
                rookFromX = 0;
                rookToX = 3;
            }

            GameObject rook = GetPosition(rookFromX, y);
            if (rook != null)
            {
                Chessman rookCm = rook.GetComponent<Chessman>();

                // xóa vị trí cũ của xe trong ma trận
                SetPositionEmpty(rookFromX, y);

                // cập nhật vị trí mới cho xe
                rookCm.SetXBoard(rookToX);
                rookCm.SetYBoard(y);
                rookCm.SetCoords();
                rookCm.hasMoved = true;
                SetPosition(rook);
            }
            else
            {
                Debug.LogWarning("[ApplyNetworkMove] Không tìm thấy Xe để nhập thành!");
            }
        } 
    }

    public void Winner(string playerWinner, bool notifyServer) // hàm trả về người thắng
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
    public void ForceSetTurn(string color)
    {
        if (gameOver) return;
        currentPlayer = color;

        if (infoText != null)
        {
            infoText.text =
                $"You are: {myColor}\n" +
                $"Turn: {currentPlayer}\n" +
                $"Room: {GameSession.RoomId}\n" +
                $"Opp: {GameSession.OpponentName}";
        }
    }
}
