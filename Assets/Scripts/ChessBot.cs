using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class ChessBot : MonoBehaviour
{
    public GameObject controller;
    public GameObject target;
    public LegalMovesManager LegalMovesManager;
    public Game game;

    private List<int> availableMoves;
    private bool moveFound = false;

    public void BotTurn()
    {
        game = GetComponent<Game>();

        if (!game.GetComponent<Game>().IsGameOver())
        {
            MakeBotMove();
        }
    }
    private void MakeBotMove()
    {
        List<GameObject> botPieces = game.GetPlayerPieces();
        if (botPieces == null || botPieces.Count == 0) return;

        botPieces = botPieces.Where(p => p != null && p.GetComponent<Chessman>() != null).ToList();

        if (botPieces.Count == 0) return;

        List<GameObject> enemyPieces = game.GetEnemyPieces();
        int a = 0;
        int b = 0;
        for(int i=0; i<botPieces.Count; i++)
        {
            a += GetValue(botPieces[i]);
        }
        for(int i=0; i<enemyPieces.Count; i++)
        {
            b += GetValue(enemyPieces[i]);
        }
        System.Random rng = new System.Random();
        int maxAttempts = botPieces.Count * 20;
        int attempts = 0;

        moveFound = false;

        while (!moveFound && attempts < maxAttempts)
        {
            attempts++;

            int randomIndex = rng.Next(0, botPieces.Count);
            GameObject selectedPiece = botPieces[randomIndex];

            if (selectedPiece == null || selectedPiece.GetComponent<Chessman>() == null)
            {
                botPieces.RemoveAt(randomIndex);
                if (botPieces.Count == 0) break;
                continue;
            }

            GenerateMoveBitmap(selectedPiece);

            if (availableMoves != null && availableMoves.Count > 0)
            {
                TryExecuteMove(selectedPiece);
            }
        }

        if (!moveFound)
        {
            Debug.LogWarning("Bot couldn't find a legal move!");
        }
    }
    public int GetValue(GameObject piece)
    {
        switch (piece.name)
        {
            case "black_king": return 0;
            case "black_queen": return 9;
            case "black_knight": return 3;
            case "black_rook": return 5; 
            case "black_pawn": return 1;
            case "black_bishop": return 3;

            case "white_king": return 0;
            case "white_queen": return 9;
            case "white_knight": return 3;
            case "white_rook": return 5;
            case "white_pawn": return 1;
            case "white_bishop": return 3;
        }
        return 0;
    }
    private void GenerateMoveBitmap(GameObject piece)
    {
        availableMoves = new List<int>();

        if (piece == null || piece.GetComponent<Chessman>() == null) return;

        int x = piece.GetComponent<Chessman>().GetXBoard();
        int y = piece.GetComponent<Chessman>().GetYBoard();

        PossibleMoves(x, y, piece);
    }

    private void TryExecuteMove(GameObject piece)
    {
        if (availableMoves == null || availableMoves.Count == 0) return;

        System.Random rng = new System.Random();

        foreach (int moveIndex in availableMoves.OrderBy(x => rng.Next()))
        {
            if (moveFound) break;

            int targetX = moveIndex % 8;
            int targetY = moveIndex / 8;

            Chessman chessman = piece.GetComponent<Chessman>();
            if (chessman == null) break;

            int origX = chessman.GetXBoard();
            int origY = chessman.GetYBoard();

            if (IsOkeyDokey(piece, origX, origY, targetX, targetY))
            {
                ExecuteMove(piece, targetX, targetY, origX, origY);
                break;
            }
        }
    }

    private void ExecuteMove(GameObject piece, int targetX, int targetY, int origX, int origY)
    {
        Game gm = controller.GetComponent<Game>();
        GameObject targetPiece = gm.GetPosition(targetX, targetY);
        bool isAttack = targetPiece != null;

        if (isAttack)
        {
            MovePlateAttackSpawn(piece, targetX, targetY, origX, origY);
        }
        else
        {
            MovePlateSpawn(piece, targetX, targetY, origX, origY);
        }
    }

    private bool IsOkeyDokey(GameObject piece, int fromX, int fromY, int toX, int toY)
    {
        if (LegalMovesManager == null)
        {
            LegalMovesManager = controller.GetComponent<LegalMovesManager>();
        }

        if (LegalMovesManager == null) return false;

        return LegalMovesManager.IsLegal(piece, fromX, fromY, toX, toY);
    }

    public void PossibleMoves(int a, int b, GameObject piece)
    {
        if (piece == null) return;

        switch (piece.name)
        {
            case "black_queen":
            case "white_queen":
                LineMovePlate(piece, a, b, 1, 0);
                LineMovePlate(piece, a, b, 0, 1);
                LineMovePlate(piece, a, b, 1, 1);
                LineMovePlate(piece, a, b, -1, 0);
                LineMovePlate(piece, a, b, 0, -1);
                LineMovePlate(piece, a, b, -1, -1);
                LineMovePlate(piece, a, b, -1, 1);
                LineMovePlate(piece, a, b, 1, -1);
                break;
            case "black_knight":
            case "white_knight":
                LMovePlate(a, b, piece);
                break;
            case "black_bishop":
            case "white_bishop":
                LineMovePlate(piece, a, b, 1, 1);
                LineMovePlate(piece, a, b, -1, -1);
                LineMovePlate(piece, a, b, -1, 1);
                LineMovePlate(piece, a, b, 1, -1);
                break;
            case "black_king":
            case "white_king":
                SurroundMovePlate(a, b, piece);
                break;
            case "black_rook":
            case "white_rook":
                LineMovePlate(piece, a, b, 1, 0);
                LineMovePlate(piece, a, b, 0, 1);
                LineMovePlate(piece, a, b, -1, 0);
                LineMovePlate(piece, a, b, 0, -1);
                break;
            case "black_pawn":
                BPawnMovePlate(a, b, piece);
                break;
            case "white_pawn":
                WPawnMovePlate(a, b, piece);
                break;
        }
    }

    private void LineMovePlate(GameObject piece, int a, int b, int xDir, int yDir)
    {
        if (moveFound) return;

        int x = a + xDir;
        int y = b + yDir;

        while (PositionOnBoard(x, y) && GetPosition(x, y) == null)
        {
            AddMoveToBitmap(x, y);
            x += xDir;
            y += yDir;
        }

        if (PositionOnBoard(x, y))
        {
            GameObject targetPiece = GetPosition(x, y);
            if (targetPiece != null && targetPiece.GetComponent<Chessman>() != null)
            {
                if (PlayerColour(targetPiece) != PlayerColour(piece))
                {
                    AddMoveToBitmap(x, y);
                }
            }
        }
    }

    private void LMovePlate(int a, int b, GameObject piece)
    {
        if (moveFound) return;

        int[][] knightMoves = new int[][]
        {
            new int[] {1, 2}, new int[] {-1, 2},
            new int[] {2, 1}, new int[] {-2, 1},
            new int[] {-2, -1}, new int[] {-1, -2},
            new int[] {1, -2}, new int[] {2, -1}
        };

        foreach (var move in knightMoves)
        {
            int x = a + move[0];
            int y = b + move[1];
            PointMovePlate(x, y, a, b, piece);
        }
    }

    private void SurroundMovePlate(int a, int b, GameObject piece)
    {
        if (moveFound) return;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                PointMovePlate(a + dx, b + dy, a, b, piece);
            }
        }
    }

    private void WPawnMovePlate(int x, int y, GameObject piece)
    {
        if (moveFound) return;

        Game gm = controller.GetComponent<Game>();

        if (gm.PositionOnBoard(x, y + 1) && gm.GetPosition(x, y + 1) == null)
        {
            AddMoveToBitmap(x, y + 1);

            if (y == 1 && gm.GetPosition(x, y + 2) == null)
            {
                AddMoveToBitmap(x, y + 2);
            }
        }

        CheckPawnCapture(x + 1, y + 1, piece, gm);
        CheckPawnCapture(x - 1, y + 1, piece, gm);

        if (y == 4)
        {
            CheckEnPassant(x + 1, y, x + 1, y + 1, piece, gm);
            CheckEnPassant(x - 1, y, x - 1, y + 1, piece, gm);
        }
    }

    private void BPawnMovePlate(int x, int y, GameObject piece)
    {
        if (moveFound) return;

        Game gm = controller.GetComponent<Game>();

        if (gm.PositionOnBoard(x, y - 1) && gm.GetPosition(x, y - 1) == null)
        {
            AddMoveToBitmap(x, y - 1);

            if (y == 6 && gm.GetPosition(x, y - 2) == null)
            {
                AddMoveToBitmap(x, y - 2);
            }
        }

        CheckPawnCapture(x + 1, y - 1, piece, gm);
        CheckPawnCapture(x - 1, y - 1, piece, gm);

        if (y == 3)
        {
            CheckEnPassant(x + 1, y, x + 1, y - 1, piece, gm);
            CheckEnPassant(x - 1, y, x - 1, y - 1, piece, gm);
        }
    }

    private void CheckPawnCapture(int x, int y, GameObject piece, Game gm)
    {
        if (gm.PositionOnBoard(x, y))
        {
            GameObject target = gm.GetPosition(x, y);
            if (target != null && target.GetComponent<Chessman>() != null)
            {
                if (PlayerColour(target) != PlayerColour(piece))
                {
                    AddMoveToBitmap(x, y);
                }
            }
        }
    }

    private void CheckEnPassant(int checkX, int checkY, int moveX, int moveY, GameObject piece, Game gm)
    {
        if (gm.PositionOnBoard(moveX, moveY) && GetEnPassant(checkX, checkY, piece))
        {
            AddMoveToBitmap(moveX, moveY);
        }
    }

    private void PointMovePlate(int x, int y, int a, int b, GameObject piece)
    {
        if (moveFound) return;

        Game gm = controller.GetComponent<Game>();

        if (!gm.PositionOnBoard(x, y)) return;

        GameObject targetPiece = gm.GetPosition(x, y);

        if (targetPiece == null)
        {
            AddMoveToBitmap(x, y);
        }
        else if (targetPiece.GetComponent<Chessman>() != null)
        {
            if (PlayerColour(targetPiece) != PlayerColour(piece))
            {
                AddMoveToBitmap(x, y);
            }
        }
    }

    private void AddMoveToBitmap(int x, int y)
    {
        int bitPosition = y * 8 + x;
        if (!availableMoves.Contains(bitPosition))
        {
            availableMoves.Add(bitPosition);
        }
    }

    public void MovePlateSpawn(GameObject piece, int matrixX, int matrixY, int i, int j)
    {
        if (moveFound) return;

        LegalMovesManager = controller.GetComponent<LegalMovesManager>();

        if (LegalMovesManager.IsLegal(piece, i, j, matrixX, matrixY))
        {
            MoveThePlate mpScript = controller.GetComponent<MoveThePlate>();
            mpScript.attack = false;
            mpScript.piece = piece;
            mpScript.enPassant = false;

            if (piece.name == "white_pawn" || piece.name == "black_pawn")
            {
                mpScript.PwnTQn = (matrixY == 7 || matrixY == 0);
            }

            mpScript.IX = i;
            mpScript.IY = j;
            mpScript.SetReference(game.GetPosition(i, j));
            mpScript.SetCoords(matrixX, matrixY);
            mpScript.MakeMove();
            moveFound = true;
        }
    }

    public void MovePlateAttackSpawn(GameObject piece, int matrixX, int matrixY, int i, int j)
    {
        if (moveFound) return;

        LegalMovesManager = controller.GetComponent<LegalMovesManager>();

        if (LegalMovesManager.IsLegal(piece, i, j, matrixX, matrixY))
        {
            MoveThePlate mpScript = controller.GetComponent<MoveThePlate>();
            mpScript.attack = true;
            mpScript.piece = piece;
            mpScript.enPassant = false;

            if (piece.name == "white_pawn" || piece.name == "black_pawn")
            {
                mpScript.PwnTQn = (matrixY == 7 || matrixY == 0);
            }

            mpScript.IX = i;
            mpScript.IY = j;
            mpScript.SetReference(game.GetPosition(i, j));
            mpScript.SetCoords(matrixX, matrixY);
            mpScript.MakeMove();

            moveFound = true;
        }
    }

    private bool PositionOnBoard(int x, int y)
    {
        return x >= 0 && x <= 7 && y >= 0 && y <= 7;
    }

    private GameObject GetPosition(int x, int y)
    {
        if (game == null) game = GetComponent<Game>();
        return game.positions[x, y];
    }

    private int PlayerColour(GameObject piece)
    {
        if (piece == null) return -1;

        string pieceName = piece.name.ToLower();
        return pieceName.StartsWith("white") ? 1 : 0;
    }

    private bool GetEnPassant(int tox, int toy, GameObject obj)
    {
        Game gm = controller.GetComponent<Game>();
        Game.Move lastMove = gm.GetTheLastMove();

        if (lastMove != null && lastMove.piece != null)
        {
            string movePieceName = lastMove.piece.name;

            if (!movePieceName.Contains("pawn")) return false;

            if (toy == 5 && !movePieceName.Contains("black")) return false;
            if (toy == 2 && !movePieceName.Contains("white")) return false;

            return (lastMove.toX == tox && lastMove.toY == toy);
        }

        if (gm.EnPassant)
        {
            GameObject targetObj = gm.positions[tox, toy];
            if (targetObj != null && gm.EPassant == targetObj)
            {
                return true;
            }
        }

        return false;
    }
}