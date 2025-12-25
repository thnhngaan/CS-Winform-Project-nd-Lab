using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameAI : MonoBehaviour
{
    public GameObject chesspiece;
    public GameObject movePlatePrefab;
    private GameObject[,] positions = new GameObject[8, 8];
    private GameObject[] playerBlack = new GameObject[16];
    private GameObject[] playerWhite = new GameObject[16];

    public string currentPlayer = "white";
    private bool gameOver = false;
    public GameOverAI gameOverUI;

    public enum GameMode {PvAI_Easy, PvAI_Normal, PvAI_Hard }
    public GameMode gameMode = GameMode.PvAI_Easy;

    private AIPlayer aiPlayer;

    void Start()
    {
        //them ham khoi tao
        string mode = PlayerPrefs.GetString("GameMode", "PvAI_Easy");

        switch (mode)
        {
            case "PvAI_Easy":
                gameMode = GameMode.PvAI_Easy;
                break;
            case "PvAI_Normal":
                gameMode = GameMode.PvAI_Normal;
                break;
            case "PvAI_Hard":
                gameMode = GameMode.PvAI_Hard;
                break;
            default:
                gameMode = GameMode.PvAI_Easy;
                break;
        }
        Time.timeScale = 1f;

        aiPlayer = GetComponent<AIPlayer>();
        aiPlayer.SetGame(this);
        playerWhite = new GameObject[]
        {
            Create("chess_white_rook", 0, 0), Create("chess_white_knight", 1, 0),
            Create("chess_white_bishop", 2, 0), Create("chess_white_queen", 3, 0), Create("chess_white_king", 4, 0),
            Create("chess_white_bishop", 5, 0), Create("chess_white_knight", 6, 0), Create("chess_white_rook", 7, 0),
            Create("chess_white_pawn", 0, 1), Create("chess_white_pawn", 1, 1), Create("chess_white_pawn", 2, 1),
            Create("chess_white_pawn", 3, 1), Create("chess_white_pawn", 4, 1), Create("chess_white_pawn", 5, 1),
            Create("chess_white_pawn", 6, 1), Create("chess_white_pawn", 7, 1)
        };

        playerBlack = new GameObject[]
        {
            Create("chess_black_rook", 0, 7), Create("chess_black_knight", 1, 7),
            Create("chess_black_bishop", 2, 7), Create("chess_black_queen", 3, 7), Create("chess_black_king", 4, 7),
            Create("chess_black_bishop", 5, 7), Create("chess_black_knight", 6, 7), Create("chess_black_rook", 7, 7),
            Create("chess_black_pawn", 0, 6), Create("chess_black_pawn", 1, 6), Create("chess_black_pawn", 2, 6),
            Create("chess_black_pawn", 3, 6), Create("chess_black_pawn", 4, 6), Create("chess_black_pawn", 5, 6),
            Create("chess_black_pawn", 6, 6), Create("chess_black_pawn", 7, 6)
        };

        for (int i = 0; i < playerBlack.Length; i++)
        {
            SetPosition(playerBlack[i]);
            SetPosition(playerWhite[i]);
        }
    }

    public GameObject Create(string name, int x, int y)
    {
        GameObject obj = Instantiate(chesspiece, new Vector3(0, 0, -1), Quaternion.identity);
        ChessmanAI cm = obj.GetComponent<ChessmanAI>();
        cm.name = name;
        cm.SetXBoard(x);
        cm.SetYBoard(y);
        cm.Activate();
        return obj;
    }

    public void SetPosition(GameObject obj)
    {
        ChessmanAI cm = obj.GetComponent<ChessmanAI>();
        positions[cm.GetXBoard(), cm.GetYBoard()] = obj;
    }
    public void SetPosition(int x, int y, GameObject obj)
    {
        positions[x, y] = obj;
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
        return x >= 0 && y >= 0 && x < 8 && y < 8;
    }

    public string GetCurrentPlayer()
    {
        return currentPlayer;
    }

    public bool IsGameOver()
    {
        return gameOver;
    }

    public void NextTurn()
    {
        if (currentPlayer == "white")
        {
            currentPlayer = "black";
            StartCoroutine(InvokeAI());
        }
        else
        {
            currentPlayer = "white";
        }
    }

    IEnumerator InvokeAI()
    {
        yield return new WaitForSeconds(0.5f);
        aiPlayer.MakeAIMove(gameMode);
    }

    public void Winner(string playerWinner)
    {
        gameOver = true;
        currentPlayer = "none";
        gameOverUI.ShowGameOver(playerWinner);
    }
}
