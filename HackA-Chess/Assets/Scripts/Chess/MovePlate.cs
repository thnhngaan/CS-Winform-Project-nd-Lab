using UnityEngine;

public class MovePlate : MonoBehaviour
{
    private Game gameController;

    private GameObject reference;

    int matrixX;
    int matrixY;

    public bool attack = false;

    // Nhập thành
    public bool isCastling = false;
    public int rookFromX, rookFromY;
    public int rookToX, rookToY;

    void Start()
    {
        // Tự tìm GameController theo tag
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

        // Nếu đây là ô tấn công thì ăn quân
        if (attack)
        {
            GameObject cp = gameController.GetPosition(matrixX, matrixY);

            if (cp != null)
            {
                if (cp.name == "chess_white_king")
                    gameController.Winner("black");

                if (cp.name == "chess_black_king")
                    gameController.Winner("white");

                Destroy(cp);
            }
        }

        // Nếu là nhập thành -> di chuyển rook
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

        // Xóa vị trí cũ
        gameController.SetPositionEmpty(cm.GetXBoard(), cm.GetYBoard());

        // Cập nhật quân di chuyển
        cm.SetXBoard(matrixX);
        cm.SetYBoard(matrixY);
        cm.SetCoords();
        cm.hasMoved = true;

        // Ghi lại lên board
        gameController.SetPosition(reference);

        // Phong cấp nếu là tốt
        TryPromote(cm);

        // Đổi lượt
        gameController.NextTurn();

        // Xóa tất cả MovePlate
        cm.DestroyMovePlates();
    }

    void TryPromote(Chessman cm)
    {
        SpriteRenderer sr = cm.GetComponent<SpriteRenderer>();

        // Tốt trắng lên hàng 7
        if (cm.name == "chess_white_pawn" && cm.GetYBoard() == 7)
        {
            cm.name = "chess_white_queen";
            sr.sprite = cm.chess_queen_white;
        }

        // Tốt đen xuống hàng 0
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
}
