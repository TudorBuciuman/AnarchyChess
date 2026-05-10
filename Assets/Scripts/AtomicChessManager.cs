using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AtomicChessManager : MonoBehaviour
{
    public AudioClip explosionClip;
    public GameObject explosionVFXPrefab;
    public float vfxLifetime = 0.6f;

    private float BoardSize => (2048f * Game.multiplier) / 100f;
    private float SquareSize => BoardSize / 8f;
    private float HalfBoard => (BoardSize / 2f) - (SquareSize / 2f);
    private int Orientation => Game.white ? 1 : -1;

    public bool IsKingCapture(GameObject piece, int tx, int ty)
    {
        if (Game.currentMinigame != Game.Minigame.Atomic) return false;
        if (piece.name != "white_king" && piece.name != "black_king") return false;

        Game game = GetComponent<Game>();
        return game.PositionOnBoard(tx, ty) && game.GetPosition(tx, ty) != null;
    }
    public bool ProcessCapture(GameObject attackingPiece, int tx, int ty)
    {
        if (Game.currentMinigame != Game.Minigame.Atomic) return false;

        Game game = GetComponent<Game>();
        string attackerColour = attackingPiece.GetComponent<Chessman>().player;
        string enemyColour = attackerColour == "white" ? "black" : "white";

        bool enemyKingDead = false;
        bool ownKingDead = false;

        HashSet<GameObject> toDestroy = new HashSet<GameObject>();

        GameObject capturedPiece = game.GetPosition(tx, ty);
        if (capturedPiece != null)
            toDestroy.Add(capturedPiece);

        toDestroy.Add(attackingPiece);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue; 
                int nx = tx + dx;
                int ny = ty + dy;
                if (!game.PositionOnBoard(nx, ny)) continue;

                GameObject neighbour = game.GetPosition(nx, ny);
                if (neighbour == null) continue;

                Chessman ncm = neighbour.GetComponent<Chessman>();
                if (ncm == null) continue; 

                if (neighbour.name == "white_pawn" || neighbour.name == "black_pawn")
                    continue;

                toDestroy.Add(neighbour);
            }
        }

        foreach (GameObject dying in toDestroy)
        {
            if (dying == null) continue;
            if (dying.name == "white_king" || dying.name == "black_king")
            {
                Chessman dcm = dying.GetComponent<Chessman>();
                if (dcm.player == enemyColour) enemyKingDead = true;
                if (dcm.player == attackerColour) ownKingDead = true;
            }
        }

        SpawnExplosionVFX(tx, ty);

        PlayExplosionSound();

        foreach (GameObject dying in toDestroy)
        {
            if (dying == null) continue;

            Chessman dcm = dying.GetComponent<Chessman>();
            if (dcm != null)
            {
                int dx = dcm.GetXBoard();
                int dy2 = dcm.GetYBoard();

                RemoveFromPlayerArray(game, dying, dcm.player);

                game.SetEmptyPosition(dx, dy2);
            }

            Destroy(dying);
        }

        game.MadeMoves = 0;
        game.FullMoves = 0;

        return enemyKingDead || ownKingDead;
    }

    public bool WouldBlastOwnKing(GameObject attackingPiece, int tx, int ty)
    {
        if (Game.currentMinigame != Game.Minigame.Atomic) return false;

        Game game = GetComponent<Game>();
        string attackerColour = attackingPiece.GetComponent<Chessman>().player;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = tx + dx;
                int ny = ty + dy;
                if (!game.PositionOnBoard(nx, ny)) continue;

                GameObject neighbour = game.GetPosition(nx, ny);
                if (neighbour == null) continue;

                if (neighbour == attackingPiece) continue;

                string kingName = attackerColour == "white" ? "white_king" : "black_king";
                if (neighbour.name == kingName)
                    return true;
            }
        }
        return false;
    }

    private void PlayExplosionSound()
    {
        if (explosionClip == null) return;

        GameObject sourceObj = GameObject.Find("Sound_effects");
        if (sourceObj == null) return;

        AudioSource audioSource = sourceObj.GetComponent<AudioSource>();
        if (audioSource == null) return;

        audioSource.clip = explosionClip;
        audioSource.Play();
    }

    private void SpawnExplosionVFX(int tx, int ty)
    {
        if (explosionVFXPrefab == null) return;

        float wx = (tx * SquareSize) - HalfBoard;
        float wy = (ty * SquareSize) - HalfBoard;

        GameObject vfx = Instantiate(
            explosionVFXPrefab,
            new Vector3(wx * Orientation, wy * Orientation, 78f),
            Quaternion.identity);

        float scale = Game.multiplier / 2.4f;
        vfx.transform.localScale = new Vector3(scale, scale, 1f);

        Destroy(vfx, vfxLifetime);
    }

    private void RemoveFromPlayerArray(Game game, GameObject piece, string colour)
    {
        if (colour == "white")
        {
            for (int i = 0; i < game.playerWhite.Length; i++)
            {
                if (game.playerWhite[i] == piece)
                {
                    game.playerWhite[i] = null;
                    return;
                }
            }
        }
        else
        {
            for (int i = 0; i < game.playerBlack.Length; i++)
            {
                if (game.playerBlack[i] == piece)
                {
                    game.playerBlack[i] = null;
                    return;
                }
            }
        }
    }
}