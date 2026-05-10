using UnityEngine;
using System.Collections.Generic;

public class FogOfWarManager : MonoBehaviour
{
    public Sprite fogTileSprite;

    [Range(0f, 1f)]
    public float fogAlpha = 0.82f;

    public Color fogColor = new Color(0.05f, 0.05f, 0.08f, 1f);
    private List<GameObject> fogTilePool = new List<GameObject>();

    private bool[,] visible = new bool[8, 8];

    public void RefreshFog()
    {
        if (Game.currentMinigame != Game.Minigame.FogOfWar)
        {
            ShowAllPieces();
            HideAllFogTiles();
            return;
        }

        ComputeVisibility();
        ApplyFogVisuals();
    }

    public bool IsVisible(int x, int y)
    {
        if (Game.currentMinigame != Game.Minigame.FogOfWar) return true;
        if (x < 0 || x > 7 || y < 0 || y > 7) return false;
        return visible[x, y];
    }

    private void ComputeVisibility()
    {
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
                visible[x, y] = false;

        Game game = GetComponent<Game>();

        string humanColour = Game.white ? "white" : "black";

        GameObject[] humanPieces = humanColour == "white"
            ? game.playerWhite
            : game.playerBlack;

        foreach (GameObject piece in humanPieces)
        {
            if (piece == null) continue;

            Chessman cm = piece.GetComponent<Chessman>();
            int px = cm.GetXBoard();
            int py = cm.GetYBoard();

            visible[px, py] = true;

            AddPieceVision(game, piece, cm, px, py, humanColour);
        }
    }

    private void AddPieceVision(Game game, GameObject piece, Chessman cm, int px, int py, string colour)
    {
        switch (piece.name)
        {
            case "white_queen":
            case "black_queen":
                LineVision(game, colour, px, py, 1, 0);
                LineVision(game, colour, px, py, 0, 1);
                LineVision(game, colour, px, py, 1, 1);
                LineVision(game, colour, px, py, -1, 0);
                LineVision(game, colour, px, py, 0, -1);
                LineVision(game, colour, px, py, -1, -1);
                LineVision(game, colour, px, py, -1, 1);
                LineVision(game, colour, px, py, 1, -1);
                break;

            case "white_rook":
            case "black_rook":
                LineVision(game, colour, px, py, 1, 0);
                LineVision(game, colour, px, py, 0, 1);
                LineVision(game, colour, px, py, -1, 0);
                LineVision(game, colour, px, py, 0, -1);
                break;

            case "white_bishop":
            case "black_bishop":
                LineVision(game, colour, px, py, 1, 1);
                LineVision(game, colour, px, py, -1, -1);
                LineVision(game, colour, px, py, -1, 1);
                LineVision(game, colour, px, py, 1, -1);
                break;

            case "white_knight":
            case "black_knight":
                PointVision(game, colour, px + 1, py + 2);
                PointVision(game, colour, px - 1, py + 2);
                PointVision(game, colour, px + 2, py + 1);
                PointVision(game, colour, px - 2, py + 1);
                PointVision(game, colour, px - 2, py - 1);
                PointVision(game, colour, px - 1, py - 2);
                PointVision(game, colour, px + 1, py - 2);
                PointVision(game, colour, px + 2, py - 1);
                break;

            case "white_king":
            case "black_king":
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        if (dx != 0 || dy != 0)
                            PointVision(game, colour, px + dx, py + dy);
                break;

            case "white_pawn":
                PointVision(game, colour, px - 1, py + 1);
                PointVision(game, colour, px + 1, py + 1);
                if (game.PositionOnBoard(px, py + 1) && game.GetPosition(px, py + 1) == null)
                {
                    visible[px, py + 1] = true;
                    if (py == 1 && game.GetPosition(px, py + 2) == null)
                        visible[px, py + 2] = true;
                }
                break;

            case "black_pawn":
                PointVision(game, colour, px - 1, py - 1);
                PointVision(game, colour, px + 1, py - 1);
                if (game.PositionOnBoard(px, py - 1) && game.GetPosition(px, py - 1) == null)
                {
                    visible[px, py - 1] = true;
                    if (py == 6 && game.GetPosition(px, py - 2) == null)
                        visible[px, py - 2] = true;
                }
                break;
        }
    }

    private void LineVision(Game game, string colour, int px, int py, int dx, int dy)
    {
        int x = px + dx, y = py + dy;
        while (game.PositionOnBoard(x, y))
        {
            visible[x, y] = true;
            GameObject occupant = game.GetPosition(x, y);
            if (occupant != null) break; 
            x += dx;
            y += dy;
        }
    }

    private void PointVision(Game game, string colour, int x, int y)
    {
        if (!game.PositionOnBoard(x, y)) return;
        visible[x, y] = true;
    }

    private void ApplyFogVisuals()
    {
        Game game = GetComponent<Game>();
        string humanColour = Game.white ? "white" : "black";
        string enemyColour = Game.white ? "black" : "white";

        for (int i = 0; i < fogTilePool.Count; i++)
            fogTilePool[i].SetActive(false);

        int tileIndex = 0;

        float boardSize = (2048f * Game.multiplier) / 100f;
        float squareSize = boardSize / 8f;
        float halfBoard = (boardSize / 2f) - (squareSize / 2f);
        int r = Game.white ? 1 : -1;

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                bool canSee = visible[x, y];

                GameObject occupant = game.GetPosition(x, y);
                if (occupant != null)
                {
                    Chessman ocm = occupant.GetComponent<Chessman>();
                    if (ocm != null)
                    {
                        bool isEnemy = ocm.player == enemyColour;
                        occupant.GetComponent<SpriteRenderer>().enabled = !isEnemy || canSee;
                    }
                }

                if (!canSee)
                {
                    float wx = (x * squareSize) - halfBoard;
                    float wy = (y * squareSize) - halfBoard;

                    GameObject tile = GetOrCreateFogTile(tileIndex);
                    tile.transform.position = new Vector3(wx * r, wy * r, 79.5f);
                    tile.transform.localScale = new Vector3(
                        squareSize / 5f,
                        squareSize / 5f,
                        1f);
                    tile.SetActive(true);
                    tileIndex++;
                }
            }
        }
    }

    private GameObject GetOrCreateFogTile(int index)
    {
        if (index < fogTilePool.Count)
            return fogTilePool[index];
        GameObject tile = new GameObject("FogTile");
        tile.tag = "FogTile";
        SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
        sr.sprite = fogTileSprite;
        sr.color = new Color(fogColor.r, fogColor.g, fogColor.b, fogAlpha);
        sr.sortingOrder = 40;
        fogTilePool.Add(tile);
        return tile;
    }

    private void HideAllFogTiles()
    {
        foreach (GameObject t in fogTilePool)
            if (t != null) t.SetActive(false);
    }

    private void ShowAllPieces()
    {
        Game game = GetComponent<Game>();
        foreach (GameObject p in game.playerWhite)
            if (p != null) p.GetComponent<SpriteRenderer>().enabled = true;
        foreach (GameObject p in game.playerBlack)
            if (p != null) p.GetComponent<SpriteRenderer>().enabled = true;
    }
}