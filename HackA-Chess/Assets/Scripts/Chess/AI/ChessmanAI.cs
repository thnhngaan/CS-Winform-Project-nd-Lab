using System.Collections.Generic;
using UnityEngine;

public class ChessmanAI : MonoBehaviour
{
    public GameObject controller;
    public GameObject movePlate;

    private int xBoard = -1;
    private int yBoard = -1;

    public string player;
    public bool hasMoved = false;


    public Sprite chess_bishop_black, chess_king_black, chess_knight_black, chess_rook_black, chess_pawn_black, chess_queen_black;
    public Sprite chess_bishop_white, chess_king_white, chess_knight_white, chess_rook_white, chess_pawn_white, chess_queen_white;

    public void Activate()
    {
        controller = GameObject.FindGameObjectWithTag("GameController");
        if (movePlate == null && controller.TryGetComponent(out GameAI gameAI))
        {
            movePlate = gameAI.movePlatePrefab;
        }
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
        x += -4.37f;
        y += -4.37f;
        this.transform.position = new Vector3(x, y, -1.0f);
    }

    public int GetXBoard() { return xBoard; }
    public int GetYBoard() { return yBoard; }
    public void SetXBoard(int x) { xBoard = x; }
    public void SetYBoard(int y) { yBoard = y; }

    public void OnMouseUp()
    {
        if (!controller.GetComponent<GameAI>().IsGameOver() && controller.GetComponent<GameAI>().GetCurrentPlayer() == player)
        {
            DestroyMovePlates();
            InitiateMovePlates();
        }
    }

    public void DestroyMovePlates()
    {
        GameObject[] movePlates = GameObject.FindGameObjectsWithTag("MovePlate");
        for (int i = 0; i < movePlates.Length; i++) Destroy(movePlates[i]);
    }

    public void InitiateMovePlates()
    {
        List<Vector2Int> moves = GetLegalMoves();
        foreach (var move in moves)
        {
            GameObject target = controller.GetComponent<GameAI>().GetPosition(move.x, move.y);
            if (target == null)
                MovePlateSpawn(move.x, move.y);
            else if (target.GetComponent<ChessmanAI>().player != player)
                MovePlateAttackSpawn(move.x, move.y);
        }
    }
    public void MovePlateSpawn(int x, int y)
    {
        float fx = x * 1.25f - 4.37f;
        float fy = y * 1.25f - 4.37f;

        GameObject mp = Instantiate(movePlate, new Vector3(fx, fy, -3.0f), Quaternion.identity);
        MovePlateAI mpScript = mp.GetComponent<MovePlateAI>();
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(x, y);
    }

    public void MovePlateAttackSpawn(int x, int y)
    {
        float fx = x * 1.25f - 4.37f;
        float fy = y * 1.25f - 4.37f;

        GameObject mp = Instantiate(movePlate, new Vector3(fx, fy, -3.0f), Quaternion.identity);
        MovePlateAI mpScript = mp.GetComponent<MovePlateAI>();
        if (mpScript != null)
        {
            mpScript.SetReference(gameObject);
            mpScript.SetCoords(x, y);
        }
        else
        {
            Debug.LogError("MovePlateAI script is missing from the MovePlate prefab!");
        }
        mpScript.attack = true;
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(x, y);
    }

    public List<Vector2Int> GetLegalMoves()
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        switch (this.name)
        {
            case "chess_black_queen":
            case "chess_white_queen":
                moves.AddRange(GetLineMoves(1, 0));
                moves.AddRange(GetLineMoves(0, 1));
                moves.AddRange(GetLineMoves(1, 1));
                moves.AddRange(GetLineMoves(-1, 0));
                moves.AddRange(GetLineMoves(0, -1));
                moves.AddRange(GetLineMoves(-1, -1));
                moves.AddRange(GetLineMoves(-1, 1));
                moves.AddRange(GetLineMoves(1, -1));
                break;
            case "chess_black_bishop":
            case "chess_white_bishop":
                moves.AddRange(GetLineMoves(1, 1));
                moves.AddRange(GetLineMoves(1, -1));
                moves.AddRange(GetLineMoves(-1, 1));
                moves.AddRange(GetLineMoves(-1, -1));
                break;
            case "chess_black_rook":
            case "chess_white_rook":
                moves.AddRange(GetLineMoves(1, 0));
                moves.AddRange(GetLineMoves(-1, 0));
                moves.AddRange(GetLineMoves(0, 1));
                moves.AddRange(GetLineMoves(0, -1));
                break;
            case "chess_black_knight":
            case "chess_white_knight":
                int[] dx = { 1, -1, 2, -2 };
                int[] dy = { 2, -2, 1, -1 };
                foreach (int x in dx)
                    foreach (int y in dy)
                        if (Mathf.Abs(x) != Mathf.Abs(y))
                            AddIfLegalMove(moves, xBoard + x, yBoard + y);
                break;
            case "chess_black_king":
            case "chess_white_king":
                for (int x = -1; x <= 1; x++)
                    for (int y = -1; y <= 1; y++)
                        if (x != 0 || y != 0)
                            AddIfLegalMove(moves, xBoard + x, yBoard + y);
                break;
            case "chess_white_pawn":
                AddPawnMoves(moves, 1);
                break;
            case "chess_black_pawn":
                AddPawnMoves(moves, -1);
                break;
        }

        return moves;
    }

    List<Vector2Int> GetLineMoves(int xInc, int yInc)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        GameAI sc = controller.GetComponent<GameAI>();
        int x = xBoard + xInc;
        int y = yBoard + yInc;
        while (sc.PositionOnBoard(x, y) && sc.GetPosition(x, y) == null)
        {
            result.Add(new Vector2Int(x, y));
            x += xInc;
            y += yInc;
        }
        if (sc.PositionOnBoard(x, y))
        {
            GameObject target = sc.GetPosition(x, y);
            if (target != null)
            {
                ChessmanAI cm = target.GetComponent<ChessmanAI>();
                if (cm != null && cm.player != player)
                {
                    result.Add(new Vector2Int(x, y));
                }
            }
        }
        return result;
    }

    void AddIfLegalMove(List<Vector2Int> list, int x, int y)
    {
        GameAI sc = controller.GetComponent<GameAI>();
        if (sc.PositionOnBoard(x, y))
        {
            GameObject target = sc.GetPosition(x, y);
            if (target == null || target.GetComponent<ChessmanAI>().player != player)
                list.Add(new Vector2Int(x, y));
        }
    }

    void AddPawnMoves(List<Vector2Int> list, int dir)
    {
        GameAI sc = controller.GetComponent<GameAI>();
        int startRank = (dir == 1) ? 1 : 6;

        if (sc.PositionOnBoard(xBoard, yBoard + dir) && sc.GetPosition(xBoard, yBoard + dir) == null)
        {
            list.Add(new Vector2Int(xBoard, yBoard + dir));
            if (yBoard == startRank && sc.GetPosition(xBoard, yBoard + 2 * dir) == null)
            {
                list.Add(new Vector2Int(xBoard, yBoard + 2 * dir));
            }
        }

        for (int dx = -1; dx <= 1; dx += 2)
        {
            int nx = xBoard + dx;
            int ny = yBoard + dir;
            if (sc.PositionOnBoard(nx, ny))
            {
                GameObject enemy = sc.GetPosition(nx, ny);
                /* if (enemy != null && enemy.GetComponent<Chessman>().player != player)
                 {
                     list.Add(new Vector2Int(nx, ny));
                 }*/
                if (enemy != null)
                {
                    ChessmanAI cm = enemy.GetComponent<ChessmanAI>();
                    if (cm != null && cm.player != player)
                    {
                        list.Add(new Vector2Int(nx, ny));
                    }
                }

            }
        }
    }

    public void AIMoveTo(int x, int y)
    {
        GameAI sc = controller.GetComponent<GameAI>();
        GameObject target = sc.GetPosition(x, y);
        if (target != null)
        {
            if (target.name == "chess_white_king")
            {
                sc.Winner("black");
                Destroy(target);
                return;
            }
            Destroy(target);
        }

        sc.SetPositionEmpty(GetXBoard(), GetYBoard());
        SetXBoard(x);
        SetYBoard(y);
        SetCoords();
        hasMoved = true;
        sc.SetPosition(gameObject);

        if (name == "chess_black_pawn" && y == 0)
        {
            name = "chess_black_queen";
            GetComponent<SpriteRenderer>().sprite = chess_queen_black;
        }

        sc.NextTurn();
    }
}
