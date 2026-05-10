using System.Collections;
using UnityEngine;

public class MoveThePlate : MonoBehaviour
{
    public GameObject controller;
    public GameObject lastMovePlate;
    public GameObject movePlate;
    public LegalMovesManager legalMovesManager;

    GameObject reference = null;
    public GameObject test;

    int matrixX;      // destination column
    int matrixY;      // destination row
    public int IX;    // origin column
    public int IY;    // origin row
    // I have no idea what the X and Y axis should be
    // On one hand, it could be the Unity position
    // Or, it could be the representation in the matrix

    public bool attack = false;
    public bool enpass = false;
    public bool PwnTQn = false;
    public bool enPassant = false;
    public bool castling = false;
    public bool checkl = false;

    public GameObject piece;
    public GameObject DMPawn;
    public GameObject Rook;

    public int xr, yr, tyr, txr;

    public AudioSource source;
    public AudioClip move, moveattack, check, enpassant;

    public float a, b;

    public const float moveDuration = 0.19f;
    public static bool IsAnimating = false;

    public void Start()
    {
        if (this.CompareTag("GameController")) return;
        if (attack)
        {
            controller = GameObject.FindGameObjectWithTag("GameController");
            GameObject chp = controller.GetComponent<Game>().GetPosition(matrixX, matrixY);
            if (chp != null)
            {
                ColorUtility.TryParseHtmlString("#BF2529", out Color myColor);
                gameObject.GetComponent<SpriteRenderer>().color = myColor;
            }
            else if (enPassant && chp == null)
            {
                ColorUtility.TryParseHtmlString("#FFB000", out Color mycol);
                gameObject.GetComponent<SpriteRenderer>().color = mycol;
            }
        }
    }
    public void PlaySound(AudioClip Clip)
    {
        GameObject surce = GameObject.FindGameObjectWithTag("source");
        source.enabled = true;
        if (surce.GetComponent<AudioSource>().clip != enpassant || !surce.GetComponent<AudioSource>().isPlaying)
        {
            surce.GetComponent<AudioSource>().clip = Clip;
            surce.GetComponent<AudioSource>().Play();
        }
    }

    public void OnMouseUp()
    {
        if (IsAnimating) return;

        StartCoroutine(MakeMoveAnimated(true));
    }

    public IEnumerator BotMoveCaller()
    {
        while (IsAnimating)
            yield return null;

        if (!legalMovesManager.GetComponent<Game>().IsGameOver())
        {
            controller = GameObject.FindGameObjectWithTag("GameController");
            controller.GetComponent<ChessBot>().BotTurn();
        }
    }
    public IEnumerator MakeMoveAnimated(bool isPlayer)
    {
        IsAnimating = true;
        controller = GameObject.FindGameObjectWithTag("GameController");

        Vector3 pieceStartPos = piece.transform.position;
        Vector3 rookStartPos = castling && Rook != null ? Rook.transform.position : Vector3.zero;

        float boardSize = (2048f * Game.multiplier) / 100f;
        float squareSize = boardSize / 8f;
        float halfBoard = (boardSize / 2f) - (squareSize / 2f);
        int r = Game.white ? 1 : -1;

        float destX = ((matrixX * squareSize) - halfBoard) * r;
        float destY = ((matrixY * squareSize) - halfBoard) * r;
        Vector3 pieceDestPos = new Vector3(destX, destY, 80f);

        Vector3 rookDestPos = Vector3.zero;
        if (castling && Rook != null)
        {
            float rdx = ((txr * squareSize) - halfBoard) * r;
            float rdy = ((tyr * squareSize) - halfBoard) * r;
            rookDestPos = new Vector3(rdx, rdy, 80f);
        }
        piece.GetComponent<SpriteRenderer>().sortingOrder = 2200;
        HandleCaptureLogic();

        controller.GetComponent<Game>().SetEmptyPosition(IX, IY);

        piece.GetComponent<Chessman>().SetXBoard(matrixX);
        piece.GetComponent<Chessman>().SetYBoard(matrixY);

        if (piece.name == "white_pawn" || piece.name == "black_pawn")
        {
            controller.GetComponent<Game>().MadeMoves = 0;
            controller.GetComponent<Game>().FullMoves = 0;
        }
        else
        {
            controller.GetComponent<Game>().MadeMoves++;
            controller.GetComponent<Game>().FullMoves =
            controller.GetComponent<Game>().MadeMoves / 2;
        }

        controller.GetComponent<Game>().SetPosition(piece);
        if (Game.currentMinigame != Game.Minigame.Duck)
            controller.GetComponent<Game>().NextTurn();
        piece.GetComponent<Chessman>().MakeMovePlatesInvisible();

        if (PwnTQn)
        {
            piece.GetComponent<Chessman>().PawnToQueen(piece);
            PwnTQn = false;
        }

        if (castling && Rook != null)
        {
            controller.GetComponent<Game>().SetEmptyPosition(xr, yr);
            Rook.GetComponent<Chessman>().SetXBoard(txr);
            Rook.GetComponent<Chessman>().SetYBoard(tyr);
            controller.GetComponent<Game>().SetPosition(Rook);
            if (Rook.name == "white_rook")
                Rook.GetComponent<Chessman>().wCastling = false;
            else
                Rook.GetComponent<Chessman>().bCastling = false;
            Rook.GetComponent<Chessman>().castling = false;
            castling = false;
        }

        controller.GetComponent<Game>().RecordTheMove(piece, IX, IY, piece.GetComponent<Chessman>().GetXBoard(), piece.GetComponent<Chessman>().GetYBoard(), attack, castling);

        float elapsed = 0f;
        if (Game.currentMinigame != Game.Minigame.FogOfWar)
        {
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);

                float tEased = t * t * (3f - 2f * t);

                piece.transform.position = Vector3.LerpUnclamped(pieceStartPos, pieceDestPos, tEased);

                if (rookStartPos != Vector3.zero && Rook != null)
                    Rook.transform.position = Vector3.LerpUnclamped(rookStartPos, rookDestPos, tEased);
                yield return null;
            }
        }
        piece.transform.position = pieceDestPos;
        piece.GetComponent<SpriteRenderer>().sortingOrder = 2197;
        if (Rook != null && rookDestPos != Vector3.zero)
            Rook.transform.position = rookDestPos;

        PostMoveLogic(squareSize, halfBoard, r);

        FogOfWarManager fog = controller.GetComponent<FogOfWarManager>();
        if (fog != null) fog.RefreshFog();

        DuckChessManager duck = controller.GetComponent<DuckChessManager>();
        if (duck != null && Game.currentMinigame == Game.Minigame.Duck)
        {
            if (isPlayer)
                duck.OnPieceMoved();
            else
                duck.OnBotPieceMoved();
        }

        piece.GetComponent<Chessman>().DestroyMovePlates();

        IsAnimating = false;
        if (isPlayer && Game.currentMinigame != Game.Minigame.Duck)
        {
            StartCoroutine(BotMoveCaller());
        }
    }
    private void HandleCaptureLogic()
    {
        if (!attack) return;

        GameObject chp = controller.GetComponent<Game>().GetPosition(matrixX, matrixY);

        if (enPassant)
        {
            chp = DMPawn;
            enabled = false;
            enpass = true;
        }

        if (chp == null) 
            return; 

        if (controller.GetComponent<LegalMovesManager>().PlayerColour(chp) == 1)
        {
            for (int i = 0; i < controller.GetComponent<Game>().playerWhite.Length; i++)
            {
                if (controller.GetComponent<Game>().playerWhite[i] == chp)
                {
                    controller.GetComponent<Game>().playerWhite[i] = null;
                    break;
                }
            }
        }
        else
        {
            for (int i = 0; i < controller.GetComponent<Game>().playerBlack.Length; i++)
            {
                if (controller.GetComponent<Game>().playerBlack[i] == chp)
                {
                    controller.GetComponent<Game>().playerBlack[i] = null;
                    break;
                }
            }
        }

        Destroy(chp);
        controller.GetComponent<Game>().MadeMoves = 0;
        controller.GetComponent<Game>().FullMoves = 0;
        controller.GetComponent<Game>().positions[matrixX, matrixY] = piece;
    }

    private void PostMoveLogic(float squareSize, float halfBoard, int r)
    {
        legalMovesManager = controller.GetComponent<LegalMovesManager>();

        if (legalMovesManager.IsKingInCheck())
        {
            a = controller.GetComponent<LegalMovesManager>().Xkposition;
            b = controller.GetComponent<LegalMovesManager>().Ykposition;
            a = (a * squareSize) - halfBoard;
            b = (b * squareSize) - halfBoard;

            if (legalMovesManager.IsCheckmate())
            {
                controller.GetComponent<Game>().Winner();
                legalMovesManager.SetGameOver();
            }
            checkl = true;
            PlaySound(check);
        }
        else if (legalMovesManager.IsStalemate() || controller.GetComponent<Game>().GetFiftyMoveRule())
        {
            legalMovesManager.SetGameOver();
            controller.GetComponent<Game>().Draw();
        }

        float x = (matrixX * squareSize) - halfBoard;
        float y = (matrixY * squareSize) - halfBoard;
        float X = (IX * squareSize) - halfBoard;
        float Y = (IY * squareSize) - halfBoard;

        GameObject[] lastMovePlat = GameObject.FindGameObjectsWithTag("lastMovePlate");
        for (int i = 0; i < lastMovePlat.Length; i++)
            Destroy(lastMovePlat[i]);

        GameObject LMP = Instantiate(lastMovePlate,
            new Vector3(x * r, y * r, 80.5f), Quaternion.identity);
        LMP.transform.localScale = new Vector2(
            LMP.transform.localScale.x * Game.multiplier / 2.4f,
            LMP.transform.localScale.y * Game.multiplier / 2.4f);

        GameObject mp = Instantiate(lastMovePlate,
            new Vector3(X * r, Y * r, 80.5f), Quaternion.identity);
        mp.transform.localScale = new Vector2(
            mp.transform.localScale.x * Game.multiplier / 2.4f,
            mp.transform.localScale.y * Game.multiplier / 2.4f);
        mp.GetComponent<SpriteRenderer>().color =
            Game.ToColorTransparentCM(Game.matchTheme, 220);

        if (checkl)
        {
            GameObject kng = Instantiate(lastMovePlate,
                new Vector3(a * r, b * r, 81), Quaternion.identity);
            kng.GetComponent<SpriteRenderer>().color = new Color(1.0f, 0.4f, 0.0f, 1.0f);
            kng.transform.localScale = new Vector2(
                kng.transform.localScale.x * Game.multiplier / 2.4f,
                kng.transform.localScale.y * Game.multiplier / 2.4f);
            checkl = false;
        }

        if (enpass)
        {
            PlaySound(enpassant);
            enpass = false;
        }
        else if (attack)
        {
            LMP.GetComponent<SpriteRenderer>().color = new Color(0.7f, 0.4f, 0.3f, 1.0f);
            PlaySound(moveattack);
            attack = false;
        }
        else
        {
            PlaySound(move);
            LMP.GetComponent<SpriteRenderer>().color = Game.ToColorTransparentLM(Game.matchTheme, 200);
        }
    }

    public void RecieveMove(int a, int b, int X, int Y)
    {
        controller = GameObject.FindGameObjectWithTag("GameController");

        attack = controller.GetComponent<Game>().positions[a, b] != null;

        float boardSize = (2048f * Game.multiplier) / 100f;
        float squareSize = boardSize / 8f;
        float halfBoard = (boardSize / 2f) - (squareSize / 2f);
        matrixX = a; matrixY = b;
        IX = X; IY = Y;

        piece = controller.GetComponent<Game>().GetPosition(X, Y);
        reference = piece;

        if (piece.name == "white_pawn" || piece.name == "black_pawn")
        {
            PwnTQn = (matrixY == 7 || matrixY == 0);

            if (piece.name == "white_pawn" && Y == 4 && b == 5 &&
                controller.GetComponent<Game>().positions[a, b] == null)
            {
                DMPawn = controller.GetComponent<Game>().positions[a, Y];
                attack = true;
                enPassant = true;
            }
            else if (piece.name == "black_pawn" && Y == 3 && b == 2 &&
                     controller.GetComponent<Game>().positions[a, b] == null)
            {
                DMPawn = controller.GetComponent<Game>().positions[a, Y];
                attack = true;
                enPassant = true;
            }
        }
        else if (piece.name == "white_king" || piece.name == "black_king")
        {
            if (piece.name == "white_king" && X == 4 && Y == 0)
            {
                if (a == 2 && b == 0)
                {
                    Rook = controller.GetComponent<Game>().GetPosition(0, 0);
                    castling = true; xr = 0; yr = 0; txr = 3; tyr = 0;
                }
                else if (a == 6 && b == 0)
                {
                    Rook = controller.GetComponent<Game>().GetPosition(7, 0);
                    castling = true; xr = 7; yr = 0; txr = 5; tyr = 0;
                }
            }
            else if (piece.name == "black_king" && X == 4 && Y == 7)
            {
                if (a == 2 && b == 7)
                {
                    Rook = controller.GetComponent<Game>().GetPosition(0, 7);
                    castling = true; xr = 0; yr = 7; txr = 3; tyr = 7;
                }
                else if (a == 6 && b == 7)
                {
                    Rook = controller.GetComponent<Game>().GetPosition(7, 7);
                    castling = true; xr = 7; yr = 7; txr = 5; tyr = 7;
                }
            }
        }

        StartCoroutine(MakeMoveAnimated(false));
    }

    public void SetCoords(int x, int y)
    {
        matrixX = x;
        matrixY = y;
    }

    public void SetReference(GameObject obj) { reference = obj; }
    public GameObject GetReference() { return reference; }
}