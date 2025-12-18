using System.Collections.Generic;
using UnityEngine;

public class AIPlayer : MonoBehaviour
{
    private GameAI game;

    void Start()
    {
        game = GetComponent<GameAI>();
    }
    public void SetGame(GameAI game)
    {
        this.game = game;
    }
    public void MakeAIMove(GameAI.GameMode mode)
    {
        List<GameObject> pieces = GetMyPieces("black");
        List<MoveOption> allMoves = new List<MoveOption>();

        foreach (var piece in pieces)
        {
            ChessmanAI cm = piece.GetComponent<ChessmanAI>();
            List<Vector2Int> legalMoves = cm.GetLegalMoves();

            foreach (var move in legalMoves)
            {
                allMoves.Add(new MoveOption(piece, move));
            }
        }

        if (allMoves.Count == 0) return;

        MoveOption selectedMove = null;

        switch (mode)
        {
            case GameAI.GameMode.PvAI_Easy:
               // selectedMove = allMoves[Random.Range(0, allMoves.Count)];
                selectedMove = MinimaxRoot(2);
                break;
            case GameAI.GameMode.PvAI_Normal:
                selectedMove = ChooseBestCapture(allMoves);
                break;
            case GameAI.GameMode.PvAI_Hard:
                selectedMove = MinimaxRoot(3);
                break;
        }

        if (selectedMove != null)
        {
            ExecuteMove(selectedMove);
        }
    }

    List<GameObject> GetMyPieces(string player)
    {
        List<GameObject> list = new List<GameObject>();
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Chessman"))
        {
            ChessmanAI cm = obj.GetComponent<ChessmanAI>();
            if (cm != null && cm.player == player)
            {
                list.Add(obj);
            }
        }
        return list;
    }


    MoveOption ChooseBestCapture(List<MoveOption> moves)
    {
        foreach (var move in moves)
        {
            GameObject target = game.GetPosition(move.target.x, move.target.y);
            if (target != null)
            {
                ChessmanAI cm = target.GetComponent<ChessmanAI>();
                if (cm != null && cm.player == "white")
                    return move;
            }
        }
        return moves[Random.Range(0, moves.Count)];
    }

    void ExecuteMove(MoveOption move)
    {
        ChessmanAI cm = move.piece.GetComponent<ChessmanAI>();
        cm.AIMoveTo(move.target.x, move.target.y);
    }

    MoveOption MinimaxRoot(int depth)
{
    List<MoveOption> allMoves = GetAllLegalMoves("black");

    // Sắp xếp nước đi để Minimax thông minh hơn
    allMoves.Sort((a, b) =>
    {
        int va = a.captured != null ? GetPieceValue(a.captured.name) : 0;
        int vb = b.captured != null ? GetPieceValue(b.captured.name) : 0;
        return vb.CompareTo(va); // ưu tiên nước ăn mạnh trước
    });

    MoveOption bestMove = null;
    int bestValue = int.MinValue;
    int alpha = int.MinValue;
    int beta = int.MaxValue;

    foreach (MoveOption move in allMoves)
    {
        SimulateMove(move);
        int value = Minimax(depth - 1, alpha, beta, false);
        UndoMove(move);

        if (value > bestValue)
        {
            bestValue = value;
            bestMove = move;
        }

        alpha = Mathf.Max(alpha, bestValue);
        if (beta <= alpha) break; 
    }

    return bestMove;
}


    int Minimax(int depth, int alpha, int beta, bool isMaximizing)
    {
        if (depth == 0 || game.IsGameOver())
            return EvaluateBoard();

        List<MoveOption> moves = GetAllLegalMoves(isMaximizing ? "black" : "white");

        moves.Sort((a, b) =>
        {
            int va = a.captured != null ? GetPieceValue(a.captured.name) : 0;
            int vb = b.captured != null ? GetPieceValue(b.captured.name) : 0;
            return vb.CompareTo(va);
        });

        if (isMaximizing)
        {
            int bestValue = int.MinValue;

            foreach (MoveOption move in moves)
            {
                SimulateMove(move);
                int value = Minimax(depth - 1, alpha, beta, false);
                UndoMove(move);

                bestValue = Mathf.Max(bestValue, value);
                alpha = Mathf.Max(alpha, value);
                if (beta <= alpha) break; 
            }
            return bestValue;
        }
        else
        {
            int bestValue = int.MaxValue;

            foreach (MoveOption move in moves)
            {
                SimulateMove(move);
                int value = Minimax(depth - 1, alpha, beta, true);
                UndoMove(move);

                bestValue = Mathf.Min(bestValue, value);
                beta = Mathf.Min(beta, value);
                if (beta <= alpha) break; 
            }
            return bestValue;
        }
    }

    //danh gia ban co
    int EvaluateBoard()
    {
        int score = 0;
        int mobilityBlack = 0;
        int mobilityWhite = 0;

        foreach (GameObject piece in GameObject.FindGameObjectsWithTag("Chessman"))
        {
            ChessmanAI cm = piece.GetComponent<ChessmanAI>();
            int val = GetPieceValue(cm.name);

            // Material
            score += (cm.player == "black") ? val : -val;

            // Mobility
            int moves = cm.GetLegalMoves().Count;
            if (cm.player == "black") mobilityBlack += moves;
            else mobilityWhite += moves;

            int cx = Mathf.Abs(3 - cm.GetXBoard());
            int cy = Mathf.Abs(3 - cm.GetYBoard());
            int centerScore = 3 - (cx + cy); 
            if (cm.player == "black") score += centerScore;
            else score -= centerScore;
        }

        score += (mobilityBlack - mobilityWhite); 

        return score;
    }


    int GetPieceValue(string name)
    {
        if (name.Contains("pawn")) return 10;
        if (name.Contains("knight") || name.Contains("bishop")) return 30;
        if (name.Contains("rook")) return 50;
        if (name.Contains("queen")) return 90;
        if (name.Contains("king")) return 900;
        return 0;
    }

    void SimulateMove(MoveOption move)
    {
        move.captured = game.GetPosition(move.target.x, move.target.y);
        game.SetPosition(move.target.x, move.target.y, move.piece);
        game.SetPosition(move.pieceX, move.pieceY, null);

        ChessmanAI cm = move.piece.GetComponent<ChessmanAI>();
        cm.SetXBoard(move.target.x);
        cm.SetYBoard(move.target.y);
    }

    void UndoMove(MoveOption move)
    {
        game.SetPosition(move.pieceX, move.pieceY, move.piece);
        game.SetPosition(move.target.x, move.target.y, move.captured);

        ChessmanAI cm = move.piece.GetComponent<ChessmanAI>();
        cm.SetXBoard(move.pieceX);
        cm.SetYBoard(move.pieceY);
    }

    List<MoveOption> GetAllLegalMoves(string player)
    {
        List<MoveOption> list = new List<MoveOption>();
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Chessman"))
        {
            ChessmanAI cm = obj.GetComponent<ChessmanAI>();
            if (cm != null && cm.player == player)
            {
                List<Vector2Int> legal = cm.GetLegalMoves();
                foreach (var m in legal)
                    list.Add(new MoveOption(obj, m));
            }
        }
        return list;
    }


}

public class MoveOption
    {
        public GameObject piece;
        public Vector2Int target;
        public GameObject captured; 

        public int pieceX, pieceY;

        public MoveOption(GameObject piece, Vector2Int target)
        {
            this.piece = piece;
            this.target = target;
            this.captured = null;

            ChessmanAI cm = piece.GetComponent<ChessmanAI>();
            this.pieceX = cm.GetXBoard();
            this.pieceY = cm.GetYBoard();
        }
    }

