using UnityEngine;

public class Chessman : MonoBehaviour
{
    public GameObject controller;
    public GameObject movePlate;

    private int xBoard = -1;
    private int yBoard = -1;

    private string player;
    public bool hasMoved = false;

    public Sprite chess_bishop_black, chess_king_black, chess_knight_black, chess_rook_black, chess_pawn_black, chess_queen_black;
    public Sprite chess_bishop_white, chess_king_white, chess_knight_white, chess_rook_white, chess_pawn_white, chess_queen_white;

    public void Activate()
    {
        controller = GameObject.FindGameObjectWithTag("GameController");

        SetCoords();

        switch (this.name)
        {
            case "chess_black_queen": this.GetComponent<SpriteRenderer>().sprite = chess_queen_black; player = "black"; break;
            case "chess_black_bishop": this.GetComponent<SpriteRenderer>().sprite = chess_bishop_black; player = "black"; break;
            case "chess_black_king": this.GetComponent<SpriteRenderer>().sprite = chess_king_black; player = "black"; break;
            case "chess_black_rook": this.GetComponent<SpriteRenderer>().sprite = chess_rook_black; player = "black"; break;
            case "chess_black_pawn": this.GetComponent<SpriteRenderer>().sprite = chess_pawn_black; player = "black"; break;
            case "chess_black_knight": this.GetComponent<SpriteRenderer>().sprite = chess_knight_black; player = "black"; break;


            case "chess_white_queen": this.GetComponent<SpriteRenderer>().sprite = chess_queen_white; player = "white"; break;
            case "chess_white_bishop": this.GetComponent<SpriteRenderer>().sprite = chess_bishop_white; player = "white"; break;
            case "chess_white_king": this.GetComponent<SpriteRenderer>().sprite = chess_king_white; player = "white"; break;
            case "chess_white_rook": this.GetComponent<SpriteRenderer>().sprite = chess_rook_white; player = "white"; break;
            case "chess_white_pawn": this.GetComponent<SpriteRenderer>().sprite = chess_pawn_white; player = "white"; break;
            case "chess_white_knight": this.GetComponent<SpriteRenderer>().sprite = chess_knight_white; player = "white"; break;


        }

    }

    public void SetCoords()
    {
        float x = xBoard;
        float y = yBoard;

        x *= 1.25f;
        y *= 1.25f;

        x += -4.35f;
        y += -4.35f;

        this.transform.position = new Vector3(x, y, -1.0f);

    }

    public int GetXBoard() { return xBoard; }

    public int GetYBoard() { return yBoard; }

    public void SetXBoard(int x) { xBoard = x; }

    public void SetYBoard(int y) { yBoard = y; }

    private void OnMouseUp()
    {
        if (!controller.GetComponent<Game>().IsGameOver() && controller.GetComponent<Game>().GetCurrentPlayer() == player)
        {
            DestroyMovePlates();

            InitiateMovePlates();
        }
    }

    public void DestroyMovePlates()
    {
        GameObject[] movePlates = GameObject.FindGameObjectsWithTag("MovePlate");
        for (int i = 0; i < movePlates.Length; i++)
        {
            Destroy(movePlates[i]);
        }
    }

    public void InitiateMovePlates()
    {
        switch (this.name)
        {
            case "chess_black_queen":
            case "chess_white_queen":
                LineMovePlate(1, 0);
                LineMovePlate(0, 1);
                LineMovePlate(1, 1);
                LineMovePlate(-1, 0);
                LineMovePlate(0, -1);
                LineMovePlate(-1, -1);
                LineMovePlate(-1, 1);
                LineMovePlate(1, -1);
                break;
            case "chess_black_knight":
            case "chess_white_knight":
                LMovePlate();
                break;
            case "chess_black_bishop":
            case "chess_white_bishop":
                LineMovePlate(1, 1);
                LineMovePlate(1, -1);
                LineMovePlate(-1, 1);
                LineMovePlate(-1, -1);
                break;
            case "chess_black_king":
            case "chess_white_king":
                SurroundMovePlate();
                CastlingMovePlate();
                break;
            case "chess_black_rook":
            case "chess_white_rook":
                LineMovePlate(1, 0);
                LineMovePlate(0, 1);
                LineMovePlate(-1, 0);
                LineMovePlate(0, -1);
                break;
            case "chess_black_pawn":
                PawnMovePlate(xBoard, yBoard - 1);
                break;
            case "chess_white_pawn":
                PawnMovePlate(xBoard, yBoard + 1);
                break;
        }
    }

    public void LineMovePlate(int xIncrement, int yIncrement)
    {
        Game sc = controller.GetComponent<Game>();

        int x = xBoard + xIncrement;
        int y = yBoard + yIncrement;

        while (sc.PositionOnBoard(x, y) && sc.GetPosition(x, y) == null)
        {
            MovePlateSpawn(x, y);
            x += xIncrement;
            y += yIncrement;
        }

        if (sc.PositionOnBoard(x, y) && sc.GetPosition(x, y).GetComponent<Chessman>().player != player)
        {
            MovePlateAttackSpawn(x, y);
        }
    }

    public void LMovePlate()
    {
        PointMovePlate(xBoard + 1, yBoard + 2);
        PointMovePlate(xBoard - 1, yBoard + 2);
        PointMovePlate(xBoard + 2, yBoard + 1);
        PointMovePlate(xBoard + 2, yBoard - 1);
        PointMovePlate(xBoard + 1, yBoard - 2);
        PointMovePlate(xBoard - 1, yBoard - 2);
        PointMovePlate(xBoard - 2, yBoard + 1);
        PointMovePlate(xBoard - 2, yBoard - 1);
    }

    public void SurroundMovePlate()
    {
        PointMovePlate(xBoard, yBoard + 1);
        PointMovePlate(xBoard, yBoard - 1);
        PointMovePlate(xBoard - 1, yBoard + 0);
        PointMovePlate(xBoard - 1, yBoard - 1);
        PointMovePlate(xBoard - 1, yBoard + 1);
        PointMovePlate(xBoard + 1, yBoard + 0);
        PointMovePlate(xBoard + 1, yBoard - 1);
        PointMovePlate(xBoard + 1, yBoard + 1);
    }

    public void PointMovePlate(int x, int y)
    {
        Game sc = controller.GetComponent<Game>();
        if (sc.PositionOnBoard(x, y))
        {
            GameObject cp = sc.GetPosition(x, y);

            if (cp == null)
            {
                MovePlateSpawn(x, y);
            }
            else if (cp.GetComponent<Chessman>().player != player)
            {
                MovePlateAttackSpawn(x, y);
            }
        }
    }

    public void PawnMovePlate(int x, int y)
    {
        Game sc = controller.GetComponent<Game>();

        int dir = (player == "white") ? 1 : -1;

        // Đi 1 ô thẳng
        if (sc.PositionOnBoard(x, y) && sc.GetPosition(x, y) == null)
        {
            MovePlateSpawn(x, y);

            // Nếu vẫn ở hàng xuất phát thì cho đi 2 ô
            int startRank = (player == "white") ? 1 : 6;
            if (yBoard == startRank)
            {
                int y2 = y + dir;
                if (sc.PositionOnBoard(x, y2) && sc.GetPosition(x, y2) == null)
                {
                    MovePlateSpawn(x, y2);
                }
            }
        }
        int attackY = yBoard + dir;

        if (sc.PositionOnBoard(xBoard + 1, attackY) &&
            sc.GetPosition(xBoard + 1, attackY) != null &&
            sc.GetPosition(xBoard + 1, attackY).GetComponent<Chessman>().player != player)
        {
            MovePlateAttackSpawn(xBoard + 1, attackY);
        }

        if (sc.PositionOnBoard(xBoard - 1, attackY) &&
            sc.GetPosition(xBoard - 1, attackY) != null &&
            sc.GetPosition(xBoard - 1, attackY).GetComponent<Chessman>().player != player)
        {
            MovePlateAttackSpawn(xBoard - 1, attackY);
        }
    }

    public void MovePlateSpawn(int matrixX, int matrixY)
    {
        //Get the board value in order to convert to xy coords
        float x = matrixX;
        float y = matrixY;

        //Adjust by variable offset
        x *= 1.25f;
        y *= 1.25f;

        x += -4.35f;
        y += -4.35f;

        //Set actual unity values
        GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);

        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
    }

    public void MovePlateAttackSpawn(int matrixX, int matrixY)
    {
        //Get the board value in order to convert to xy coords
        float x = matrixX;
        float y = matrixY;

        //Adjust by variable offset
        x *= 1.25f;
        y *= 1.25f;

        x += -4.35f;
        y += -4.35f;

        //Set actual unity values
        GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);

        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.attack = true;
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
    }

    public void MovePlateCastlingSpawn(int matrixX, int matrixY,
                                   int rookFromX, int rookFromY,
                                   int rookToX, int rookToY)
    {
        float x = matrixX;
        float y = matrixY;

        x *= 1.25f;
        y *= 1.25f;

        x += -4.35f;
        y += -4.35f;

        GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);

        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);

        mpScript.isCastling = true;
        mpScript.rookFromX = rookFromX;
        mpScript.rookFromY = rookFromY;
        mpScript.rookToX = rookToX;
        mpScript.rookToY = rookToY;

    }

    public void CastlingMovePlate()
    {
        Game sc = controller.GetComponent<Game>();

        // Vua trắng ở e1 (4,0)
        if (player == "white" && xBoard == 4 && yBoard == 0)
        {
            Chessman kingCm = GetComponent<Chessman>();

            // Rook bên phải (h1: 7,0)
            GameObject rookRight = sc.GetPosition(7, 0);
            if (rookRight != null)
            {
                Chessman rookCm = rookRight.GetComponent<Chessman>();
                if (!kingCm.hasMoved && !rookCm.hasMoved &&
                    sc.GetPosition(5, 0) == null &&
                    sc.GetPosition(6, 0) == null)
                {
                    MovePlateCastlingSpawn(6, 0, 7, 0, 5, 0);
                }
            }

            // Rook bên trái (a1: 0,0)
            GameObject rookLeft = sc.GetPosition(0, 0);
            if (rookLeft != null)
            {
                Chessman rookCm = rookLeft.GetComponent<Chessman>();
                if (!kingCm.hasMoved && !rookCm.hasMoved &&
                    sc.GetPosition(1, 0) == null &&
                    sc.GetPosition(2, 0) == null &&
                    sc.GetPosition(3, 0) == null)
                {
                    MovePlateCastlingSpawn(2, 0, 0, 0, 3, 0);
                }
            }
        }

        // Vua đen ở e8 (4,7)
        if (player == "black" && xBoard == 4 && yBoard == 7)
        {
            Chessman kingCm = GetComponent<Chessman>();

            // Rook phải (h8: 7,7)
            GameObject rookRight = sc.GetPosition(7, 7);
            if (rookRight != null)
            {
                Chessman rookCm = rookRight.GetComponent<Chessman>();
                if (!kingCm.hasMoved && !rookCm.hasMoved &&
                    sc.GetPosition(5, 7) == null &&
                    sc.GetPosition(6, 7) == null)
                {
                    MovePlateCastlingSpawn(6, 7, 7, 7, 5, 7);
                }
            }

            // Rook trái (a8: 0,7)
            GameObject rookLeft = sc.GetPosition(0, 7);
            if (rookLeft != null)
            {
                Chessman rookCm = rookLeft.GetComponent<Chessman>();
                if (!kingCm.hasMoved && !rookCm.hasMoved &&
                    sc.GetPosition(1, 7) == null &&
                    sc.GetPosition(2, 7) == null &&
                    sc.GetPosition(3, 7) == null)
                {
                    MovePlateCastlingSpawn(2, 7, 0, 7, 3, 7);
                }
            }
        }
    }
}
