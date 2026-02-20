using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class LegalMovesManager : MonoBehaviour
{
    public Game legalMovesManage;
    public Game game;
    public bool IsGameOver = false;
    public GameObject lostpiece;
    public GameObject CheckPiece;
    public int Xkposition;
    public int Ykposition;

    public void SetGameOver()
    {
        IsGameOver = true;
    }

    public bool GetGameOver()
    {
        return IsGameOver;
    }

    public bool IsLegal(GameObject piece, int x, int y, int a, int b)
    {
        if (piece == null) return false;

        int t = 0;
        legalMovesManage = GetComponent<Game>();
        List<GameObject> enemyPieces = legalMovesManage.GetEnemyPieces();

        GameObject targetPiece = GetPosition(a, b);
        GameObject sourcePiece = GetPosition(x, y);

        if (sourcePiece != piece) return false;

        if (targetPiece != null && PlayerColour(targetPiece) != PlayerColour(piece))
        {
            t = 1;
            lostpiece = targetPiece;
            legalMovesManage.positions[a, b] = piece;
            legalMovesManage.positions[x, y] = null;
        }
        else if (targetPiece == null)
        {
            legalMovesManage.positions[a, b] = piece;
            legalMovesManage.positions[x, y] = null;
        }
        else
        {
            return false;
        }

        foreach (GameObject enemyPiece in enemyPieces)
        {
            if (enemyPiece != null && enemyPiece != lostpiece)
            {
                if (ThreatensKing(enemyPiece))
                {
                    if (t == 1)
                    {
                        legalMovesManage.positions[a, b] = lostpiece;
                        lostpiece = null;
                    }
                    else
                    {
                        legalMovesManage.positions[a, b] = null;
                    }
                    legalMovesManage.positions[x, y] = piece;
                    return false;
                }
            }
        }

        if (t == 1)
        {
            legalMovesManage.positions[a, b] = lostpiece;
        }
        else
        {
            legalMovesManage.positions[a, b] = null;
        }
        legalMovesManage.positions[x, y] = piece;
        lostpiece = null;
        return true;
    }

    public bool IsCheckmate()
    {
        return IsStalemate();
    }

    public bool IsStalemate()
    {
        legalMovesManage = GetComponent<Game>();
        List<GameObject> playerPieces = legalMovesManage.GetPlayerPieces();

        foreach (GameObject playerPiece in playerPieces)
        {
            if (playerPiece != null)
            {
                if (MakeMove(playerPiece))
                {
                    return false;
                }
            }
        }
        return true;
    }

    public bool IsKingInCheck()
    {
        legalMovesManage = GetComponent<Game>();
        List<GameObject> enemyPieces = legalMovesManage.GetEnemyPieces();

        foreach (GameObject enemyPiece in enemyPieces)
        {
            if (enemyPiece != null)
            {
                if (ThreatensKing(enemyPiece))
                {
                    SetCheckPieceFast(enemyPiece);
                    return true;
                }
            }
        }
        return false;
    }

    public bool ThreatensKing(GameObject piece)
    {
        if (piece == null || piece.GetComponent<Chessman>() == null) return false;

        int a = piece.GetComponent<Chessman>().GetXBoard();
        int b = piece.GetComponent<Chessman>().GetYBoard();

        switch (piece.name)
        {
            case "black_king":
            case "white_king":
                return (GoTo(a, b + 1) || GoTo(a - 1, b + 1) || GoTo(a, b - 1) ||
                        GoTo(a - 1, b - 1) || GoTo(a + 1, b + 1) || GoTo(a + 1, b) ||
                        GoTo(a + 1, b - 1) || GoTo(a - 1, b));

            case "black_queen":
            case "white_queen":
                return (LineMovePlate(1, 0, a, b) || LineMovePlate(0, 1, a, b) ||
                        LineMovePlate(1, 1, a, b) || LineMovePlate(-1, 0, a, b) ||
                        LineMovePlate(0, -1, a, b) || LineMovePlate(-1, -1, a, b) ||
                        LineMovePlate(-1, 1, a, b) || LineMovePlate(1, -1, a, b));

            case "black_bishop":
            case "white_bishop":
                return (LineMovePlate(1, 1, a, b) || LineMovePlate(-1, -1, a, b) ||
                        LineMovePlate(-1, 1, a, b) || LineMovePlate(1, -1, a, b));

            case "black_pawn":
                return (GoTo(a - 1, b - 1) || GoTo(a + 1, b - 1));

            case "white_pawn":
                return (GoTo(a - 1, b + 1) || GoTo(a + 1, b + 1));

            case "black_knight":
            case "white_knight":
                return (GoTo(a + 1, b + 2) || GoTo(a - 1, b + 2) || GoTo(a + 1, b - 2) ||
                        GoTo(a + 2, b - 1) || GoTo(a - 1, b - 2) || GoTo(a + 2, b + 1) ||
                        GoTo(a - 2, b + 1) || GoTo(a - 2, b - 1));

            case "black_rook":
            case "white_rook":
                return (LineMovePlate(1, 0, a, b) || LineMovePlate(0, 1, a, b) ||
                        LineMovePlate(-1, 0, a, b) || LineMovePlate(0, -1, a, b));
        }
        return false;
    }

    public bool LineMovePlate(int xI, int yI, int a, int b)
    {
        if (game == null) game = GetComponent<Game>();

        int x = a + xI;
        int y = b + yI;
        string currentPlayer = game.currentPlayer;

        while (PositionOnBoard(x, y) && GetPosition(x, y) == null)
        {
            x += xI;
            y += yI;
        }

        if (!PositionOnBoard(x, y)) return false;

        GameObject targetPiece = GetPosition(x, y);
        if (targetPiece == null) return false;

        Chessman chessman = targetPiece.GetComponent<Chessman>();
        if (chessman == null) return false;

        if (chessman.player != currentPlayer) return false;

        if (currentPlayer == "black" && targetPiece.name == "black_king")
        {
            Xkposition = x;
            Ykposition = y;
            return true;
        }
        else if (currentPlayer == "white" && targetPiece.name == "white_king")
        {
            Xkposition = x;
            Ykposition = y;
            return true;
        }

        return false;
    }

    public bool GoTo(int x, int y)
    {
        if (!PositionOnBoard(x, y)) return false;

        GameObject piece = GetPosition(x, y);
        if (piece == null) return false;

        if (game == null) game = GetComponent<Game>();

        string currentPlayer = game.currentPlayer;

        if (currentPlayer == "black" && piece.name == "black_king")
        {
            Xkposition = x;
            Ykposition = y;
            return true;
        }
        else if (currentPlayer == "white" && piece.name == "white_king")
        {
            Xkposition = x;
            Ykposition = y;
            return true;
        }

        return false;
    }

    public bool PositionOnBoard(int x, int y)
    {
        return x >= 0 && x <= 7 && y >= 0 && y <= 7;
    }

    public GameObject GetPosition(int x, int y)
    {
        if (game == null) game = GetComponent<Game>();

        if (!PositionOnBoard(x, y)) return null;

        return game.positions[x, y];
    }

    public void SetCheckPieceFast(GameObject piece)
    {
        CheckPiece = piece;
    }

    public GameObject GetCheckPieceFast()
    {
        return CheckPiece;
    }

    public bool MakeMove(GameObject piece)
    {
        if (piece == null || piece.GetComponent<Chessman>() == null) return false;

        int a = piece.GetComponent<Chessman>().xBoard;
        int b = piece.GetComponent<Chessman>().yBoard;

        switch (piece.name)
        {
            case "black_king":
            case "white_king":
                return (TemporaryUpdate(piece, a, b, a, b - 1) ||
                        TemporaryUpdate(piece, a, b, a, b + 1) ||
                        TemporaryUpdate(piece, a, b, a - 1, b - 1) ||
                        TemporaryUpdate(piece, a, b, a - 1, b) ||
                        TemporaryUpdate(piece, a, b, a - 1, b + 1) ||
                        TemporaryUpdate(piece, a, b, a + 1, b - 1) ||
                        TemporaryUpdate(piece, a, b, a + 1, b) ||
                        TemporaryUpdate(piece, a, b, a + 1, b + 1));

            case "black_queen":
            case "white_queen":
                return (LMovePlate(piece, 1, 0, a, b) || LMovePlate(piece, 0, 1, a, b) ||
                        LMovePlate(piece, 1, 1, a, b) || LMovePlate(piece, -1, 0, a, b) ||
                        LMovePlate(piece, 0, -1, a, b) || LMovePlate(piece, -1, -1, a, b) ||
                        LMovePlate(piece, -1, 1, a, b) || LMovePlate(piece, 1, -1, a, b));

            case "black_bishop":
            case "white_bishop":
                return (LMovePlate(piece, 1, 1, a, b) || LMovePlate(piece, -1, -1, a, b) ||
                        LMovePlate(piece, -1, 1, a, b) || LMovePlate(piece, 1, -1, a, b));

            case "black_pawn":
                return ((TemporaryUpdate(piece, a, b, a - 1, b - 1) &&
                         GetPosition(a - 1, b - 1) != null) ||
                        (TemporaryUpdate(piece, a, b, a + 1, b - 1) &&
                         GetPosition(a + 1, b - 1) != null) ||
                        (GetPosition(a, b - 1) == null && 
                        TemporaryUpdate(piece, a, b, a, b - 1) )||
                        (b == 6 && GetPosition(a, b - 1) == null &&
                         GetPosition(a, b - 2) == null  &&
                         TemporaryUpdate(piece, a, b, a, b - 2)));

            case "white_pawn":
                return ((TemporaryUpdate(piece, a, b, a - 1, b + 1) &&
                         GetPosition(a - 1, b + 1) != null) ||
                        (TemporaryUpdate(piece, a, b, a + 1, b + 1) &&
                         GetPosition(a + 1, b + 1) != null) ||
                        (GetPosition(a, b + 1) == null &&
                        TemporaryUpdate(piece, a, b, a, b + 1)) ||
                        (b == 1 && GetPosition(a, b + 1) == null &&
                        GetPosition(a, b + 2) == null &&
                         TemporaryUpdate(piece, a, b, a, b + 2)));

            case "black_knight":
            case "white_knight":
                return (TemporaryUpdate(piece, a, b, a + 1, b + 2) ||
                        TemporaryUpdate(piece, a, b, a - 1, b + 2) ||
                        TemporaryUpdate(piece, a, b, a + 1, b - 2) ||
                        TemporaryUpdate(piece, a, b, a + 2, b - 1) ||
                        TemporaryUpdate(piece, a, b, a - 1, b - 2) ||
                        TemporaryUpdate(piece, a, b, a + 2, b + 1) ||
                        TemporaryUpdate(piece, a, b, a - 2, b + 1) ||
                        TemporaryUpdate(piece, a, b, a - 2, b - 1));

            case "black_rook":
            case "white_rook":
                return (LMovePlate(piece, 1, 0, a, b) || LMovePlate(piece, 0, 1, a, b) ||
                        LMovePlate(piece, -1, 0, a, b) || LMovePlate(piece, 0, -1, a, b));

            default:
                return false;
        }
    }

    public bool LMovePlate(GameObject piece, int a, int b, int x, int y)
    {
        if (piece == null) return false;

        int q = x;
        int r = y;

        while (PositionOnBoard(q + a, r + b) && GetPosition(q + a, r + b) == null)
        {
            q += a;
            r += b;

            if (PositionOnBoard(q, r) && TemporaryUpdate(piece, x, y, q, r))
            {
                return true;
            }
        }

        if (PositionOnBoard(q + a, r + b))
        {
            GameObject targetPiece = GetPosition(q + a, r + b);
            if (targetPiece != null && PlayerColour(targetPiece) != PlayerColour(piece))
            {
                q += a;
                r += b;
                if (TemporaryUpdate(piece, x, y, q, r))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool TemporaryUpdate(GameObject piece, int x, int y, int a, int b)
    {
        if (piece == null || legalMovesManage == null) return false;

        int t = 0;
        List<GameObject> enemyPieces = legalMovesManage.GetEnemyPieces();

        GameObject targetPiece = GetPosition(a, b);
        GameObject sourcePiece = GetPosition(x, y);

        if (!PositionOnBoard(a, b)) return false;

        if (targetPiece != null && PlayerColour(targetPiece) != PlayerColour(piece))
        {
            t = 1;
            lostpiece = targetPiece;
            legalMovesManage.positions[a, b] = piece;
            legalMovesManage.positions[x, y] = null;
        }
        else if (targetPiece == null)
        {
            t = 2;
            legalMovesManage.positions[a, b] = piece;
            legalMovesManage.positions[x, y] = null;
        }
        else
        {
            return false;
        }

        if (t == 1 || t == 2)
        {
            foreach (GameObject ePiece in enemyPieces)
            {
                if (ePiece != null && (t == 2 || ePiece != lostpiece))
                {
                    if (ThreatensKing(ePiece))
                    {
                        legalMovesManage.positions[x, y] = piece;
                        if (t == 1)
                        {
                            legalMovesManage.positions[a, b] = lostpiece;
                            lostpiece = null;
                        }
                        else
                        {
                            legalMovesManage.positions[a, b] = null;
                        }
                        return false;
                    }
                }
            }

            legalMovesManage.positions[x, y] = piece;
            if (t == 1)
            {
                legalMovesManage.positions[a, b] = lostpiece;
                lostpiece = null;
            }
            else
            {
                legalMovesManage.positions[a, b] = null;
            }
            return true;
        }

        return false;
    }

    public int PlayerColour(GameObject piece)
    {
        if (piece == null) return -1;

        switch (piece.name)
        {
            case "white_king":
            case "white_queen":
            case "white_knight":
            case "white_bishop":
            case "white_rook":
            case "white_pawn":
                return 1;
            default:
                return 0;
        }
    }
}