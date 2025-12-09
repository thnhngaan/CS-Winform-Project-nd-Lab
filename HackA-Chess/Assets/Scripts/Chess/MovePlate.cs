using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading.Tasks;
using Assets.Scripts;

public class MovePlate : MonoBehaviour // Hàm hiện bước đi
{
    private Game gameController;

    private GameObject reference;

    int matrixX;
    int matrixY;

    public bool attack = false;

    public bool isCastling = false;
    public int rookFromX, rookFromY;
    public int rookToX, rookToY;

    void Start()
    {
        GameObject controllerObj = GameObject.FindGameObjectWithTag("GameController");
        if (controllerObj != null)
        {
            gameController = controllerObj.GetComponent<Game>();
        }
        else
        {
            Debug.LogError("Không tìm thấy GameObject với tag 'GameController'!");
        }

        if (attack)
        {
            GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0f, 1f);
        }
    }

    void OnMouseUp()
    {
        if (gameController == null)
        {
            Debug.LogError("gameController null trong MovePlate! Kiểm tra tag GameController.");
            return;
        }

        Chessman cm = reference.GetComponent<Chessman>();
        if (cm == null) return;

        // Không phải lượt mình thì bỏ
        if (!gameController.IsMyTurn() || gameController.GetCurrentPlayer() != cm.GetPlayer())
        {
            return;
        }

        // ===== 1. LƯU TỌA ĐỘ CŨ TRƯỚC KHI DI CHUYỂN =====
        int fromX = cm.GetXBoard();
        int fromY = cm.GetYBoard();

        bool gameEnded = false;

        // ===== 2. XỬ LÝ ATTACK (ĂN QUÂN) =====
        if (attack)
        {
            GameObject cp = gameController.GetPosition(matrixX, matrixY);

            if (cp != null)
            {
                if (cp.name == "chess_white_king")
                {
                    // đen ăn vua trắng -> đen thắng, notify server
                    gameController.Winner("black", true);
                    gameEnded = true;
                }
                else if (cp.name == "chess_black_king")
                {
                    // trắng ăn vua đen -> trắng thắng, notify server
                    gameController.Winner("white", true);
                    gameEnded = true;
                }

                Destroy(cp);
            }
        }

        // ===== 3. NHẬP THÀNH NẾU CÓ =====
        if (isCastling)
        {
            GameObject rook = gameController.GetPosition(rookFromX, rookFromY);
            if (rook != null)
            {
                gameController.SetPositionEmpty(rookFromX, rookFromY);

                Chessman rookCm = rook.GetComponent<Chessman>();
                rookCm.SetXBoard(rookToX);
                rookCm.SetYBoard(rookToY);
                rookCm.SetCoords();
                rookCm.hasMoved = true;

                gameController.SetPosition(rook);
            }
        }

        // ===== 4. CẬP NHẬT QUÂN CỜ CỦA MÌNH =====
        gameController.SetPositionEmpty(fromX, fromY);
        cm.SetXBoard(matrixX);
        cm.SetYBoard(matrixY);
        cm.SetCoords();
        cm.hasMoved = true;

        gameController.SetPosition(reference);
        TryPromote(cm);

        if (!gameEnded)
        {
            gameController.NextTurn();
        }

        cm.DestroyMovePlates();

        // ===== 5. GỬI NƯỚC ĐI LÊN SERVER (from = tọa độ CŨ) =====
        _ = SendMoveAsync(fromX, fromY, matrixX, matrixY);
    }

    void TryPromote(Chessman cm)
    {
        SpriteRenderer sr = cm.GetComponent<SpriteRenderer>();

        if (cm.name == "chess_white_pawn" && cm.GetYBoard() == 7)
        {
            cm.name = "chess_white_queen";
            sr.sprite = cm.chess_queen_white;
        }

        if (cm.name == "chess_black_pawn" && cm.GetYBoard() == 0)
        {
            cm.name = "chess_black_queen";
            sr.sprite = cm.chess_queen_black;
        }
    }

    public void SetCoords(int x, int y)
    {
        matrixX = x;
        matrixY = y;
    }

    public void SetReference(GameObject obj)
    {
        reference = obj;
    }

    public GameObject GetReference()
    {
        return reference;
    }

    private async Task SendMoveAsync(int fromX, int fromY, int toX, int toY)
    {
        try
        {
            if (NetworkClient.Instance == null || !NetworkClient.Instance.IsConnected)
                return;

            string roomId = GameSession.RoomId ?? "000000";
            string msg = $"MOVE|{roomId}|{fromX}|{fromY}|{toX}|{toY}";
            await NetworkClient.Instance.SendAsync(msg);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Send MOVE error: " + ex.Message);
        }
    }

    private async Task SendGameOverAsync(string winnerColor)
    {
        try
        {
            if (NetworkClient.Instance == null || !NetworkClient.Instance.IsConnected)
                return;

            string roomId = GameSession.RoomId ?? "000000";
            string msg = $"GAME_OVER|{roomId}|{winnerColor}";
            await NetworkClient.Instance.SendAsync(msg);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Send GAME_OVER error: " + ex.Message);
        }
    }
}
