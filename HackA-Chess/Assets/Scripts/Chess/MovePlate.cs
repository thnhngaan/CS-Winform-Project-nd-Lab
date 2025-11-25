using UnityEngine;

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
        bool gameEnded = false;
        if (attack)
        {
            GameObject cp = gameController.GetPosition(matrixX, matrixY);

            if (cp != null)
            {
                if (cp.name == "chess_white_king")
                {
                    gameController.Winner("black");
                    gameEnded = true;
                }
                if (cp.name == "chess_black_king")
                {
                    gameController.Winner("white");
                    gameEnded = true;
                }


                Destroy(cp);
            }
        }
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

        gameController.SetPositionEmpty(cm.GetXBoard(), cm.GetYBoard());
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
}
