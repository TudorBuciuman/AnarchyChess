using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using System;
public class Game : MonoBehaviour
{
    [SerializeField]
    public PieceSetDefinition[] pieceSets = new PieceSetDefinition[3];
    public GameObject OtherUI, OtherUIb;
    public int PieceSet = -1;

    public GameObject chesspiece;
    public Chessman csman;
    public string MyTurn = "a";
    public GameObject EPassant = null;
    public GameObject[,] positions = new GameObject[8, 8];
    public GameObject[] playerBlack = new GameObject[50];
    public GameObject[] playerWhite = new GameObject[50];
    public string currentPlayer = "white";
    public static bool white = true;
    public bool gameOver = false;
    public string winner;
    public int FullMoves = 0;
    public int MadeMoves = 0;
    public string Fen;
    public bool EnPassant = true;
    public bool WCastling = false;
    public bool BCastling = false;
    public bool wkCastling = false;
    public bool wqCastling = false;
    public bool bkCastling = false;
    public bool bqCastling = false;
    public List<Move> MoveHistory = new List<Move>();

    public float MyTime = 300;
    public float OppTime = 300;
    public Text MyTimeT;
    public Text OppTimeT;
    public Text MyName;
    public Text OppName;
    public Text WinnerT;
    public string OppNamet;
    public string MyNamet;

    public Text YouWonText;
    public Image dark;
    public Texture2D horsecursor;

    public Sprite[] tableSprites = new Sprite[10];
    public Color c;
    public static Colors matchTheme = Colors.wood1;
    public static Minigame currentMinigame;
    public static bool ShowMovesTracker = true;


    string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    public static float multiplier = 2.4f;
    public void Awake()
    {
#if UNITY_STANDALONE_WIN
        multiplier = 4.5f;
#endif
        c = GameObject.Find("tabla_sah").GetComponent<SpriteRenderer>().color;
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate =60;
        if (CompareTag("GameController"))
            StartTheGame();
    }
    public void StartTheGame()
    {
        white = UnityEngine.Random.Range(0, 2) == 1;
        if (!white)
        {
            Destroy(OtherUI);
            OtherUIb.SetActive(true);
        }
        else
        {
            Destroy(OtherUIb);
        }
        PieceSetManager();
        Theme();
        if (UnityEngine.Random.Range(1, 10)==2)
        Cursor.SetCursor(horsecursor,Vector2.zero,CursorMode.Auto);
        ChoseMinigame();
        
        for (int i = 0; i < playerBlack.Length; i++)
        {
            if(playerBlack[i])
            SetPosition(playerBlack[i]);
        }
        for (int i = 0; i < playerWhite.Length; i++)
        {
            if (playerWhite[i])
            SetPosition(playerWhite[i]);
        }
        if (currentMinigame == Minigame.FogOfWar)
        {
            FogOfWarManager fog = GameObject.FindGameObjectWithTag("GameController").GetComponent<FogOfWarManager>();
            if (fog != null)
                fog.RefreshFog();
        }
        if (currentMinigame == Minigame.Duck)
        {
            DuckChessManager duck = GetComponent<DuckChessManager>();
            if (duck != null)
                duck.SpawnDuck();
        }
        if (!white)
        {
            GameObject.FindGameObjectWithTag("GameController").GetComponent<ChessBot>().BotTurn();
        }
        FindFirstObjectByType<MinigameAnnouncer>().ShowMinigame(GetMinigameName(currentMinigame));
        FindFirstObjectByType<MoveTrackerUI>().Resetter(ShowMovesTracker);
    }
    public void ChoseMinigame()
    {
        int random = UnityEngine.Random.Range(0, 10);
        if (random ==3 || random==4)
        {
            currentMinigame = Minigame.NineHundredSixty;
            Chess960();
        }
        else if (random == 7)
        {
            fen = "rnbqkbnr/pppppppp/8/1PP2PP1/PPPPPPPP/PPPPPPPP/PPPPPPPP/PPPPPPPP w kq - 0 1";
            currentMinigame = Minigame.Horde;
            LoadThePositionFromFen(fen);
        }
        else if (random == 6)
        {
            currentMinigame = Minigame.FogOfWar;
            LoadThePositionFromFen(fen);
        }
        else if(random == 8)
        {
            currentMinigame = Minigame.Atomic;
            LoadThePositionFromFen(fen);
        }
        else if (random == 5)
        {
            currentMinigame = Minigame.Duck;
            LoadThePositionFromFen(fen);
        }
        else
        {
            currentMinigame = Minigame.Classic;
            LoadThePositionFromFen(fen);
        }
    }
    public void Update()
    {
        if (Input.GetKeyUp(KeyCode.K))
            Theme();
        if (Input.GetKeyUp(KeyCode.L))
            RebuildPieces();
    }
    public enum Colors
    {
        normal,
        dark,
        white,
        bright,
        darkbrown,
        brightwood,
        anarchy,
        gray,
        blue,
        JIK,
        wood1,
        wood2,
        valentines,
        olive,
        metal,
        /*horsey,
        dublin,
        fascism,
        lichess_anarchy,
        turbografx,
        war,
        darkfantasy,*/

    }
    public enum Minigame
    {
        Classic,
        NineHundredSixty,
        Horde,
        FogOfWar,
        Anarchy,
        Atomic,
        AnarchyBot,
        Duck,
        DiceChess,
    }
    public static string GetMinigameName(Minigame game)
    {
        switch (game)
        {
            case Minigame.Horde:
                return "Horde";
            case Minigame.NineHundredSixty:
                return "Chess 960";
            case Minigame.Classic:
                return "Classic";
            case Minigame.Atomic:
                return "Atomic";
            case Minigame.Anarchy:
                return "Anarchy";
            case Minigame.DiceChess:
                return "Dice chess";
            case Minigame.Duck:
                return "Duck chess";
        }
        return game.ToString();
    }
    public static Color ToColor(Colors c)
    {
        switch (c)
        {
            case Colors.normal:
                return new Color32(0x44, 0x14, 0x02, 255); 

            case Colors.dark:
                return new Color32(0x10, 0x14, 0x19, 255);

            case Colors.white:
                return new Color32(0xF5, 0xEB, 0xE0, 255);

            case Colors.bright:
                return new Color32(0xE7, 0xD0, 0x9E, 255); 

            case Colors.darkbrown:
                return new Color32(0x25, 0x16, 0x05, 255); 

            case Colors.brightwood:
                return new Color32(0xD4, 0xAA, 0x7D, 255); 

            case Colors.gray:
                return new Color32(0x1C, 0x1C, 0x21, 255); 

            case Colors.blue:
                return new Color32(0x22, 0x80, 0xBF, 255); 

            case Colors.JIK:
                return new Color32(0x23, 0x2E, 0xD1, 255);

            case Colors.valentines:
                return new Color32(0xFF, 0x9C, 0xE4, 255);

            case Colors.olive:
                return new Color32(0xC0, 0xC1, 0x8F, 255);

            case Colors.metal:
                return Color.gray;

            case Colors.wood2:
                return new Color32(0x44, 0x24, 0x00, 255);

            default:
                return new Color32(0x49, 0x00, 0x00, 255);
        }
    }
    public static Color ToColorTransparent(Colors c, byte t)
    {
        switch (c)
        {
            case Colors.normal:
                return new Color32(0x78, 0x47, 0x1A, t);

            case Colors.dark:
                return new Color32(0x10, 0x14, 0x19, t);

            case Colors.white:
                return new Color32(0x80, 0x80, 0x80, t);

            case Colors.bright:
                return new Color32(0xE7, 0xD0, 0x9E, t);

            case Colors.darkbrown:
                return new Color32(0x25, 0x16, 0x05, t);

            case Colors.brightwood:
                return new Color32(0xD4, 0xAA, 0x7D, t);

            case Colors.gray:
                return new Color32(0x1C, 0x1C, 0x21, t);

            case Colors.blue:
                return new Color32(0x22, 0x80, 0xBF, t);

            case Colors.JIK:
                return new Color32(0x23, 0x2E, 0xD1, t);

            default:
                return new Color32(0x78, 0x47, 0x1A, t);
        }
    }
    public static Color ToColorTransparentLM(Colors c, byte t)
    {
        switch (c)
        {
            case Colors.normal:
                return new Color(0.8f, 0.6f, 0.3f, 1.0f);

            case Colors.dark:
                return new Color32(0x12, 0x14, 0x19, t);

            case Colors.white:
                return new Color32(0x66, 0x66, 0x66, t);

            case Colors.bright:
                return new Color32(0xE9, 0xD0, 0x9E, t);

            case Colors.darkbrown:
                return new Color32(0x45, 0x16, 0x05, t);

            case Colors.brightwood:
                return new Color32(0xD4, 0xAA, 0x7D, t);

            case Colors.gray:
                return new Color32(0x1C, 0x1C, 0x21, t);

            case Colors.blue:
                return new Color32(0x22, 0x80, 0xBF, t);

            case Colors.JIK:
                return new Color32(0x23, 0x2E, 0xD1, t);

            default:
                return new Color(0.8f, 0.6f, 0.3f, 1.0f);
        }
    }
    public static Color ToColorTransparentCM(Colors c, byte t)
    {
        switch (c)
        {
            case Colors.normal:
                return new Color(0.3f, 0.3f, 0.3f, 1.0f);

            case Colors.dark:
                return new Color32(0x12, 0x14, 0x19, t);

            case Colors.white:
                return new Color32(0x66, 0x66, 0x66, t);

            case Colors.bright:
                return new Color32(0xE9, 0xD0, 0x9E, t);

            case Colors.darkbrown:
                return new Color32(0x45, 0x16, 0x05, t);

            case Colors.brightwood:
                return new Color32(0xD4, 0xAA, 0x7D, t);

            case Colors.gray:
                return new Color32(0x1C, 0x1C, 0x21, t);

            case Colors.blue:
                return new Color32(0x22, 0x80, 0xBF, t);

            case Colors.JIK:
                return new Color32(0x23, 0x2E, 0xD1, t);

            default:
                return new Color(0.3f, 0.3f, 0.3f, 1.0f);
        }
    }
    public static Color ToColorBoardEdge(Colors c)
    {
        switch (c)
        {
            case Colors.metal:
                return new Color32(0x3A, 0x3A, 0x3A, 255);

            default:
                return Color.black;
        }
    }
    public static Color ToColorBright(Colors c)
    {
        switch (c)
        {
            case Colors.normal:
                return new Color32(0xAB, 0x87, 0x51, 255);

            case Colors.dark:
                return new Color32(0x9D, 0x9D, 0x9D, 255);

            case Colors.white:
                return Color.white;

            case Colors.bright:
                return new Color32(0xAB, 0x87, 0x51, 255);

            case Colors.darkbrown:
                return new Color32(0xAB, 0x87, 0x51, 255);

            case Colors.brightwood:
                return new Color32(0x9D, 0x9D, 0x9D, 255);

            case Colors.gray:
                return new Color32(0x9D, 0x9D, 0x9D, 255);

            case Colors.blue:
                return new Color32(0x9D, 0x9D, 0x9D, 255);

            case Colors.JIK:
                return new Color32(0x9D, 0x9D, 0x9D, 255);

            case Colors.valentines:
                return new Color32(0xFD, 0xFD, 0xFD, 255);

            case Colors.olive:
                return new Color32(0xFD, 0xFD, 0xFD, 255);

            case Colors.metal:
                return new Color32(0xDB, 0xDB, 0xDB, 255);

            default:
                return new Color32(0x9D, 0x9D, 0x9D, 255);
        }
    }
    public static Color ToColorDark(Colors c)
    {
        switch (c)
        {
            case Colors.normal:
                return new Color32(0x66, 0x43, 0x24, 255);

            case Colors.dark:
                return Color.black;

            case Colors.white:
                return Color.black;

            case Colors.bright:
                return Color.black;

            case Colors.darkbrown:
                return new Color32(0x25, 0x16, 0x05, 255);

            case Colors.brightwood:
                return Color.black;

            case Colors.gray:
                return Color.black;

            case Colors.blue:
                return Color.black;

            case Colors.JIK:
                return Color.black;

            case Colors.valentines:
                return Color.black;

            case Colors.olive:
                return Color.black;
            case Colors.metal:
                return Color.black;

            default:
                return Color.black;
        }
    }
    public void PieceSetManager()
    {
        int y = UnityEngine.Random.Range(0, pieceSets.Length);
        while (y == PieceSet)
            y = UnityEngine.Random.Range(0, pieceSets.Length);
        PieceSet = y;
    }
    public void RebuildPieces()
    {
        PieceSetManager();
        GameObject[] objects = GameObject.FindGameObjectsWithTag("Player");
        foreach (var item in objects)
        {
            PieceSpriteLoader(item);
        }
    }
    public Colors GetTheColor()
    {
        Colors[] values = (Colors[])Enum.GetValues(typeof(Colors));
        Colors randomColor = values[UnityEngine.Random.Range(0, values.Length)];
        while (randomColor == matchTheme)
            randomColor = values[UnityEngine.Random.Range(0, values.Length)];
        if (randomColor == Colors.white && (DateTime.Now.Hour >= 17 || DateTime.Now.Hour <= 8))
            randomColor = GetTheColor();
        return randomColor;
    }
    public void Theme()
    {
        GameObject table = GameObject.Find("tabla_sah");
        table.transform.localScale = new Vector2(multiplier, multiplier);
        if (multiplier == 2.4f)
        {
            GameObject.Find("Other").transform.localScale = new Vector2(2.4f / 4.5f, 2.4f / 4.5f);
            GameObject.Find("Board_edge").transform.localScale *= new Vector2(2.4f / 4.5f, 2.4f / 4.5f);
        }
        Colors randomColor = GetTheColor();
        //PieceSetManager();
        matchTheme = randomColor;
        GameObject.FindFirstObjectByType<Camera>().backgroundColor = ToColor(randomColor);
        switch (randomColor)
        {
            case Colors.anarchy:
                {
                    table.GetComponent<SpriteRenderer>().sprite = tableSprites[1];
                    table.GetComponent<SpriteRenderer>().color = Color.white;
                    GameObject.Find("Board_edge").GetComponent<SpriteRenderer>().color = ToColorBoardEdge(randomColor);
                    GameObject.Find("aceg").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("1357").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("bdfh").GetComponent<Text>().color = ToColorDark(randomColor);
                    GameObject.Find("2468").GetComponent<Text>().color = ToColorDark(randomColor);
                    break;
                }
            case Colors.JIK:
                {
                    table.GetComponent<SpriteRenderer>().sprite = tableSprites[2];
                    table.GetComponent<SpriteRenderer>().color = Color.white;
                    GameObject.Find("Board_edge").GetComponent<SpriteRenderer>().color = ToColorBoardEdge(randomColor);
                    GameObject.Find("aceg").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("1357").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("bdfh").GetComponent<Text>().color = ToColorDark(randomColor);
                    GameObject.Find("2468").GetComponent<Text>().color = ToColorDark(randomColor);
                    break;
                }
            case Colors.brightwood:
                {
                    table.GetComponent<SpriteRenderer>().sprite = tableSprites[0];
                    table.GetComponent<SpriteRenderer>().color = Color.white;
                    GameObject.Find("Board_edge").GetComponent<SpriteRenderer>().color = ToColorBoardEdge(randomColor);
                    GameObject.Find("aceg").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("1357").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("bdfh").GetComponent<Text>().color = ToColorDark(randomColor);
                    GameObject.Find("2468").GetComponent<Text>().color = ToColorDark(randomColor);
                    break;
                }
            case Colors.darkbrown:
                {
                    table.GetComponent<SpriteRenderer>().sprite = tableSprites[3];
                    table.GetComponent<SpriteRenderer>().color = Color.white;
                    GameObject.Find("Board_edge").GetComponent<SpriteRenderer>().color = ToColorBoardEdge(randomColor);
                    GameObject.Find("aceg").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("1357").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("bdfh").GetComponent<Text>().color = ToColorDark(randomColor);
                    GameObject.Find("2468").GetComponent<Text>().color = ToColorDark(randomColor);
                    break;
                }
            case Colors.bright:
                {
                    table.GetComponent<SpriteRenderer>().sprite = tableSprites[0];
                    table.GetComponent<SpriteRenderer>().color = Color.white;
                    GameObject.Find("Board_edge").GetComponent<SpriteRenderer>().color = ToColorBoardEdge(randomColor);
                    GameObject.Find("aceg").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("1357").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("bdfh").GetComponent<Text>().color = ToColorDark(randomColor);
                    GameObject.Find("2468").GetComponent<Text>().color = ToColorDark(randomColor);
                    break;
                }
            case Colors.white:
                {
                    table.GetComponent<SpriteRenderer>().sprite = tableSprites[UnityEngine.Random.Range(0,3)];
                    table.GetComponent<SpriteRenderer>().color = Color.white;
                    GameObject.Find("Board_edge").GetComponent<SpriteRenderer>().color = ToColorBoardEdge(randomColor);
                    GameObject.Find("aceg").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("1357").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("bdfh").GetComponent<Text>().color = ToColorDark(randomColor);
                    GameObject.Find("2468").GetComponent<Text>().color = ToColorDark(randomColor);
                    break;
                }
            case Colors.blue:
                {
                    table.GetComponent<SpriteRenderer>().sprite = tableSprites[2];
                    table.GetComponent<SpriteRenderer>().color = Color.white;
                    GameObject.Find("Board_edge").GetComponent<SpriteRenderer>().color = ToColorBoardEdge(randomColor);
                    GameObject.Find("aceg").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("1357").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("bdfh").GetComponent<Text>().color = ToColorDark(randomColor);
                    GameObject.Find("2468").GetComponent<Text>().color = ToColorDark(randomColor);
                    break;
                }
            case Colors.gray:
                {
                    table.GetComponent<SpriteRenderer>().sprite = tableSprites[0];
                    table.GetComponent<SpriteRenderer>().color = Color.gray;
                    GameObject.Find("Board_edge").GetComponent<SpriteRenderer>().color = ToColorBoardEdge(randomColor);
                    GameObject.Find("aceg").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("1357").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("bdfh").GetComponent<Text>().color = ToColorDark(randomColor);
                    GameObject.Find("2468").GetComponent<Text>().color = ToColorDark(randomColor);
                    break;
                }
            case Colors.dark:
                {
                    table.GetComponent<SpriteRenderer>().sprite = tableSprites[8];
                    table.GetComponent<SpriteRenderer>().color = Color.gray;
                    GameObject.Find("Board_edge").GetComponent<SpriteRenderer>().color = ToColorBoardEdge(randomColor);
                    GameObject.Find("aceg").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("1357").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("bdfh").GetComponent<Text>().color = ToColorDark(randomColor);
                    GameObject.Find("2468").GetComponent<Text>().color = ToColorDark(randomColor);
                    break;
                }
            case Colors.valentines:
                {
                    table.GetComponent<SpriteRenderer>().sprite = tableSprites[4];
                    table.GetComponent<SpriteRenderer>().color = Color.white;
                    GameObject.Find("Board_edge").GetComponent<SpriteRenderer>().color = ToColorBoardEdge(randomColor);
                    GameObject.Find("aceg").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("1357").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("bdfh").GetComponent<Text>().color = ToColorDark(randomColor);
                    GameObject.Find("2468").GetComponent<Text>().color = ToColorDark(randomColor);
                    break;
                }

            case Colors.metal:
                {
                    table.GetComponent<SpriteRenderer>().sprite = tableSprites[5];
                    table.GetComponent<SpriteRenderer>().color = Color.white;
                    GameObject.Find("Board_edge").GetComponent<SpriteRenderer>().color = ToColorBoardEdge(randomColor);
                    GameObject.Find("aceg").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("1357").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("bdfh").GetComponent<Text>().color = ToColorDark(randomColor);
                    GameObject.Find("2468").GetComponent<Text>().color = ToColorDark(randomColor);
                    break;
                }

            case Colors.olive:
                {
                    table.GetComponent<SpriteRenderer>().sprite = tableSprites[6];
                    table.GetComponent<SpriteRenderer>().color = Color.white;
                    GameObject.Find("Board_edge").GetComponent<SpriteRenderer>().color = ToColorBoardEdge(randomColor);
                    GameObject.Find("aceg").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("1357").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("bdfh").GetComponent<Text>().color = ToColorDark(randomColor);
                    GameObject.Find("2468").GetComponent<Text>().color = ToColorDark(randomColor);
                    break;
                }

            case Colors.wood2:
                {
                    table.GetComponent<SpriteRenderer>().sprite = tableSprites[7];
                    table.GetComponent<SpriteRenderer>().color = Color.white;
                    GameObject.Find("Board_edge").GetComponent<SpriteRenderer>().color = ToColorBoardEdge(randomColor);
                    GameObject.Find("aceg").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("1357").GetComponent<Text>().color = ToColorBright(randomColor);
                    GameObject.Find("bdfh").GetComponent<Text>().color = ToColorDark(randomColor);
                    GameObject.Find("2468").GetComponent<Text>().color = ToColorDark(randomColor);
                    break;
                }

            default:
                table.GetComponent<SpriteRenderer>().color = c;
                table.GetComponent<SpriteRenderer>().sprite = tableSprites[0];
                GameObject.Find("Board_edge").GetComponent<SpriteRenderer>().color = ToColorBoardEdge(randomColor);
                GameObject.Find("aceg").GetComponent<Text>().color = ToColorBright(randomColor);
                GameObject.Find("1357").GetComponent<Text>().color = ToColorBright(randomColor);
                GameObject.Find("bdfh").GetComponent<Text>().color = ToColorDark(randomColor);
                GameObject.Find("2468").GetComponent<Text>().color = ToColorDark(randomColor);
                break;
        }
        GameObject[] lastMovePlat = GameObject.FindGameObjectsWithTag("lastMovePlate");
        for (int i = 0; i < lastMovePlat.Length; i++)
        {
            if (i == 0) { 
                lastMovePlat[i].GetComponent<SpriteRenderer>().color = ToColorTransparentLM(randomColor,220);
            }
            else
            {
                lastMovePlat[i].GetComponent<SpriteRenderer>().color = ToColorTransparentCM(randomColor, 200);
            }
        }
        GameObject[] movePlates = GameObject.FindGameObjectsWithTag("MovePlate");
        foreach(GameObject mv in movePlates)
        {
            if (!mv.GetComponent<MoveThePlate>().attack)
            {
                switch (randomColor)
                {
                    case Colors.anarchy:
                        {
                            mv.GetComponent<SpriteRenderer>().color = ToColorTransparent(randomColor, 204);
                            break;
                        }
                    case Colors.JIK:
                        {
                            mv.GetComponent<SpriteRenderer>().color = ToColorTransparent(randomColor, 204);
                            break;
                        }
                    case Colors.brightwood:
                        {
                            mv.GetComponent<SpriteRenderer>().color = ToColorTransparent(randomColor, 204);
                            break;
                        }
                    case Colors.bright:
                        {
                            mv.GetComponent<SpriteRenderer>().color = ToColorTransparent(randomColor, 204);
                            break;
                        }
                    case Colors.white:
                        {
                            mv.GetComponent<SpriteRenderer>().color = ToColorTransparent(randomColor, 204);
                            break;
                        }
                    case Colors.blue:
                        {
                            mv.GetComponent<SpriteRenderer>().color = ToColorTransparent(randomColor, 204);
                            break;
                        }
                    case Colors.gray:
                        {
                            mv.GetComponent<SpriteRenderer>().color = ToColorTransparent(randomColor, 204);
                            break;
                        }
                    case Colors.dark:
                        {
                            mv.GetComponent<SpriteRenderer>().color = ToColorTransparent(randomColor, 204);
                            break;
                        }
                    default:
                        mv.GetComponent<SpriteRenderer>().color = ToColorTransparent(randomColor, 204);
                        break;
                }
            }
        }
    }

    GameObject Create(string name, int x, int y)
    {
        GameObject obj = Instantiate(chesspiece, new Vector3(0, 0, 80), Quaternion.identity);
        obj.transform.localScale = new Vector2(obj.transform.localScale.x * Game.multiplier / 2.4f, obj.transform.localScale.y * Game.multiplier / 2.4f);
        Chessman cm = obj.GetComponent<Chessman>();
        cm.name = name;
        cm.SetYBoard(y);
        cm.SetXBoard(x);
        cm.Activate();
        PieceSpriteLoader(obj);
        return obj;
    }
    public void ReturnQueenSprite(GameObject obj)
    {
        switch (obj.name)
        {
            case "white_queen": obj.GetComponent<SpriteRenderer>().sprite = pieceSets[PieceSet].white_queen; break;
            case "black_queen": obj.GetComponent<SpriteRenderer>().sprite = pieceSets[PieceSet].black_queen; break;
        }
    }
    public void PieceSpriteLoader(GameObject obj)
    {
        switch (obj.name)
        {
            case "black_king": obj.GetComponent<SpriteRenderer>().sprite = pieceSets[PieceSet].black_king; break;
            case "black_queen": obj.GetComponent<SpriteRenderer>().sprite = pieceSets[PieceSet].black_queen; break;
            case "black_knight": obj.GetComponent<SpriteRenderer>().sprite = pieceSets[PieceSet].black_knight; break;
            case "black_rook": obj.GetComponent<SpriteRenderer>().sprite = pieceSets[PieceSet].black_rook; break;
            case "black_pawn":  obj.GetComponent<SpriteRenderer>().sprite = pieceSets[PieceSet].black_pawn; break;
            case "black_bishop": obj.GetComponent<SpriteRenderer>().sprite = pieceSets[PieceSet].black_bishop; break;

            case "white_king": obj.GetComponent<SpriteRenderer>().sprite = pieceSets[PieceSet].white_king; break;
            case "white_queen": obj.GetComponent<SpriteRenderer>().sprite = pieceSets[PieceSet].white_queen; break;
            case "white_knight": obj.GetComponent<SpriteRenderer>().sprite = pieceSets[PieceSet].white_knight; break;
            case "white_rook": obj.GetComponent<SpriteRenderer>().sprite = pieceSets[PieceSet].white_rook; break;
            case "white_pawn": obj.GetComponent<SpriteRenderer>().sprite = pieceSets[PieceSet].white_pawn; break;
            case "white_bishop": obj.GetComponent<SpriteRenderer>().sprite = pieceSets[PieceSet].white_bishop; break;
        }
    }

    public void SetPosition(GameObject obj)
    {
        Chessman cm = obj.GetComponent<Chessman>();
        positions[cm.GetXBoard(), cm.GetYBoard()] = obj;
    }

    public void SetEmptyPosition(int x, int y)
    {
        positions[x, y] = null;
    }

    public GameObject GetPosition(int x, int y)
    {
        return positions[x, y];
    }

    public bool PositionOnBoard(int x, int y)
    {
        if (x < 0 || y < 0 || x > 7 || y > 7) return false;
        return true;
    }

    public bool IsGameOver()
    {
        return gameOver;
    }

    public string GetCurrentPlayer()
    {
        return currentPlayer;
    }

    public void NextTurn()
    {
        if (currentPlayer == "white")
        {
            currentPlayer = "black";
        }
        else
        {
            currentPlayer = "white";
        }
    }

    public void Winner()
    {
        gameOver = true;
        NextTurn();
        if (PlayerPrefs.GetInt("darkMode") == 1)
        {
            if (currentPlayer != (white? "white" : "black"))
            {
                GameObject a= Instantiate(Resources.Load<GameObject>("GameOver"));
                AudioManager.PlayGameOver();
            }
            else if (currentPlayer == (white ? "white" : "black"))
            {
                StartCoroutine(FadeAlphaImg(dark, 10));
                StartCoroutine(FadeAlpha(YouWonText, 8));
            }
        }
        else if (UI.win)
        {
            StartCoroutine(FadeAlphaImg(dark, 5));
            StartCoroutine(FadeAlpha(YouWonText, 3));
        }
        else
        {
            WinnerT.gameObject.SetActive(true);
            winner = currentPlayer;
            WinnerT.text = currentPlayer + " has won";
        }
        PlayerPrefs.SetString("LastScene", "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
    }

    public IEnumerator FadeAlpha(Text image, float time)
    {
        Color color = image.color;
        float startAlpha = 0f;
        float endAlpha = 1f; 
        float elapsed = 0f;
        color.a = startAlpha;
        image.color = color;

        while (elapsed < time){
            elapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / time);
            color.a = newAlpha;
            image.color = color;
            yield return null;
        }

        color.a = endAlpha;
        image.color = color;
    }
    public IEnumerator FadeAlphaImg(Image image, float time)
    {
        Color color = image.color;
        float startAlpha = 0f;
        float endAlpha = 140/255f;
        float elapsed = 0f;
        color.a = startAlpha;
        image.color = color;

        while (elapsed < time){
            elapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / time);
            color.a = newAlpha;
            image.color = color;
            yield return null;
        }

        color.a = endAlpha;
        image.color = color;
    }
    public void WinnerText(string player)
    {
        WinnerT.text = player + " has won";
        WinnerT.gameObject.SetActive(true);
        gameOver = true;
    }

    public void Draw()
    {
        WinnerT.text = "Draw";
        WinnerT.gameObject.SetActive(true);
        gameOver = true;
    }
    public string FormatTime(float time)
    {
        int min = Mathf.FloorToInt(time / 60);
        int sec = Mathf.FloorToInt(time % 60);
        return string.Format("{0:00}:{1:00}", min, sec);
    }

    [System.Serializable]
    public class Move
    {
        public GameObject piece;
        public int fromX;
        public int fromY;
        public int toX;
        public int toY;

        public Move(GameObject piece, int fromX, int fromY, int toX, int toY)
        {
            this.piece = piece;
            this.fromX = fromX;
            this.fromY = fromY;
            this.toX = toX;
            this.toY = toY;
        }
    }

    public void RecordTheMove(GameObject piece, int fromX, int fromY, int toX, int toY, bool attack, bool castling)
    {
        Move move = new Move(piece, fromX, fromY, toX, toY);
        Chessman chessman = piece.GetComponent<Chessman>();
        MoveHistory.Add(move);
        string castleSize = null;
        if ((move.piece.name == "white_rook" && fromX == 0 && fromY == 0) || (toX == 0 && toY == 0))
        {
            chessman.wqCastling = false;
        }
        else if ((move.piece.name == "white_rook" && fromX == 7 && fromY == 0) || (toX == 7 && toY == 0))
        {
            chessman.wkCastling = false;
        }
        else if ((move.piece.name == "black_rook" && fromX == 0 && fromY == 7) || (toX == 0 && toY == 7))
        {
            chessman.bqCastling = false;
        }
        else if ((move.piece.name == "black_rook" && fromX == 7 && fromY == 7) || (toX == 7 && toY == 7))
        {
            chessman.bkCastling = false;
        }
        else if (move.piece.name == "black_king")
        {
            chessman.bCastling = false;
        }
        else if (move.piece.name == "white_king")
        {
            WCastling = false;
        }
        if (castling)
        {
            if (toX == 6)
            {
                castleSize="queenside";
            }
            else
            {
                castleSize = "kingside";
            }
        }
        if(ShowMovesTracker)
        FindFirstObjectByType<MoveTrackerUI>()?.RecordMove(
        piece, fromX, fromY, toX, toY,
        isCapture: attack,
        isCheck: false,
        isCheckmate: false,
        castleSide: null,
        promotionPiece: null
        );
    }

    public Move GetTheLastMove()
    {
        if (MoveHistory.Count > 0)
        {
            return MoveHistory[MoveHistory.Count - 1];
        }
        return null;
    }

    public List<GameObject> GetEnemyPieces()
    {
        List<GameObject> enemyPieces = new List<GameObject>();


        if (currentPlayer == "white")
        {
            foreach (GameObject piece in playerBlack)
            {
                if (piece != null)
                {
                    enemyPieces.Add(piece);
                }
            }
        }
        else
        {
            foreach (GameObject piece in playerWhite)
            {
                if (piece != null)
                {
                    enemyPieces.Add(piece);
                }
            }
        }
        return enemyPieces;
    }
    public List<GameObject> GetPlayerPieces()
    {
        int r = 0;
        List<GameObject> playerPieces = new List<GameObject>();
        if (currentPlayer == "white")
        {
            foreach (GameObject piece in playerWhite)
            {
                if (piece != null)
                {
                    r++;
                    playerPieces.Add(piece);
                }
            }
        }
        else
        {
            foreach (GameObject piece in playerBlack)
            {
                if (piece != null)
                {
                    playerPieces.Add(piece);
                }
            }
        }
        return playerPieces;
    }
    public void Chess960()
    {
        int w = 0, b = 0;
        GameObject controller = GameObject.FindGameObjectWithTag("GameController");
        controller.GetComponent<Game>().WCastling = false;
        controller.GetComponent<Game>().BCastling = false;
        int[] v = new int[8];

        for (int i=0; i<8; i++)
        playerWhite[w++] = Create("white_pawn", i, 1);
        for (int i = 0; i < 8; i++)
        playerBlack[b++] = Create("black_pawn", i, 6);
        int b1 = UnityEngine.Random.Range(1, 5),b2=UnityEngine.Random.Range(1,5);
        playerWhite[w++] = Create("white_bishop", b1 * 2 - 1, 0);
        playerWhite[w++] = Create("white_bishop", b2 * 2 - 2, 0);
        playerBlack[b++] = Create("black_bishop", b1 * 2 - 1, 7);
        playerBlack[b++] = Create("black_bishop", b2 * 2 - 2, 7);
        v[b1*2-1] = 1;
        v[b2*2-2] = 1;
        int q = SpecialRandomToInt(v);
        v[q] = 1;
        playerWhite[w++] = Create("white_queen", q, 0);
        playerBlack[b++] = Create("black_queen", q, 7);
        int k1 = SpecialRandomToInt(v);
        v[k1] = 1;
        int k2 = SpecialRandomToInt(v);
        v[k2] = 1;
        playerWhite[w++] = Create("white_knight", k1, 0);
        playerWhite[w++] = Create("white_knight", k2, 0);
        playerBlack[b++] = Create("black_knight", k1, 7);
        playerBlack[b++] = Create("black_knight", k2, 7);
        int r1=-1, k=-1, r2=0;
        for(int i=0; i<8; i++)
        {
            if (v[i] == 0)
            {
                if (r1 == -1)
                {
                    r1 = i;
                }
                else if (k == -1)
                {
                    k = i;
                }
                else
                {
                    r2 = i;
                    break;
                }
            }
        }
        playerWhite[w++] = Create("white_rook", r1, 0);
        playerWhite[w++] = Create("white_king", k, 0);
        playerWhite[w++] = Create("white_rook", r2, 0);
        playerBlack[b++] = Create("black_rook", r1, 7);
        playerBlack[b++] = Create("black_king", k, 7);
        playerBlack[b++] = Create("black_rook", r2, 7);
    }
    public int SpecialRandomToInt(int[] v)
    {
        int t = 0;
        for (int i = 0; i < 8; i++)
            if (v[i]==0)
            t++;
        int y = UnityEngine.Random.Range(1, t + 1);
        for(int i=0; i<8; i++)
        {
            if (v[i] == 0)
            {
                y--;
                if (y == 0)
                    return i;
            }
        }
        return 0;
    }
    public void LoadThePositionFromFen(string fen)
    {
            GameObject controller = GameObject.FindGameObjectWithTag("GameController");
            if (PlayerPrefs.GetString("Continue") == "yes" && SceneManager.GetActiveScene().name != "Multiplayer lobby")
            {
                fen = PlayerPrefs.GetString("LastScene");
            Debug.Log(fen);
            }
            int y = fen.Length;
            int u = 0;
            int w = 0, b = 0;
            for (int i = 0; i <= 63; i++)
            {
                if (char.IsDigit(fen[u]))
                {
                    i += (int)(char.GetNumericValue(fen[u])) - 1;
                    u++;

                }
                else if ((int)(fen[u]) == '/')
                {
                    u++;
                    i--;
                }
                else if ((int)(fen[u]) == 'K')
                {
                    playerWhite[w++] = Create("white_king", (i % 8), (7 - (i / 8)));
                    u++;
                }
                else if ((int)(fen[u]) == 'Q')
                {
                    playerWhite[w++] = Create("white_queen", (i % 8), (7 - (i / 8)));
                    u++;
                }
                else if ((int)(fen[u]) == 'R')
                {
                    playerWhite[w++] = Create("white_rook", (i % 8), (7 - (i / 8)));
                    u++;
                }
                else if ((int)(fen[u]) == 'B')
                {
                    playerWhite[w++] = Create("white_bishop", (i % 8), (7 - (i / 8)));
                    u++;
                }
                else if ((int)(fen[u]) == 'N')
                {
                    playerWhite[w++] = Create("white_knight", (i % 8), (7 - (i / 8)));
                    u++;
                }
                else if ((int)(fen[u]) == 'P')
                {
                    playerWhite[w++] = Create("white_pawn", (i % 8), (7 - (i / 8)));
                    u++;
                }

                else if ((int)(fen[u]) == 'k')
                {
                    playerBlack[b++] = Create("black_king", (i % 8), (7 - (i / 8)));
                    u++;
                }
                else if ((int)(fen[u]) == 'q')
                {
                    playerBlack[b++] = Create("black_queen", (i % 8), (7 - (i / 8)));
                    u++;
                }
                else if ((int)(fen[u]) == 'r')
                {
                    playerBlack[b++] = Create("black_rook", (i % 8), (7 - (i / 8)));
                    u++;
                }
                else if ((int)(fen[u]) == 'b')
                {
                    playerBlack[b++] = Create("black_bishop", (i % 8), (7 - (i / 8)));
                    u++;
                }
                else if ((int)(fen[u]) == 'n')
                {
                    playerBlack[b++] = Create("black_knight", (i % 8), (7 - (i / 8)));
                    u++;
                }
                else if ((int)(fen[u]) == 'p')
                {
                    playerBlack[b++] = Create("black_pawn", (i % 8), (7 - (i / 8)));
                    u++;
                }

            }
            if (u < y)
            {
                if (fen[u] == ' ')
                    u++;

                if ((int)(fen[u]) == 'b')
                {
                    currentPlayer = "black";
                }
                u += 2;
                int ctl = 0;
                while (u < y)
                {
                    if (ctl == 0 && (int)(fen[u]) == '-')
                    {
                        ctl = 1;
                        if ((int)(fen[u]) == '-')
                        {
                            controller.GetComponent<Game>().WCastling = false;
                            controller.GetComponent<Game>().BCastling = false;
                            controller.GetComponent<Game>().BCastling = false;
                            controller.GetComponent<Game>().WCastling = false;
                        }
                        u++;
                    }
                    else if ((int)(fen[u]) == 'Q')
                    {
                    ctl = 1;
                        controller.GetComponent<Game>().WCastling = true;
                        controller.GetComponent<Game>().WCastling = true;
                        controller.GetComponent<Game>().wqCastling = true;
                        controller.GetComponent<Game>().wqCastling = true;
                    }
                    else if ((int)(fen[u]) == 'K')
                    {
                    ctl = 1;
                        controller.GetComponent<Game>().WCastling = true;
                        controller.GetComponent<Game>().WCastling = true;
                        controller.GetComponent<Game>().wkCastling = true;
                        controller.GetComponent<Game>().wkCastling = true;

                    }
                    else if ((int)(fen[u]) == 'q')
                    {
                    ctl = 1;
                        controller.GetComponent<Game>().BCastling = true;
                        controller.GetComponent<Game>().BCastling = true;
                        controller.GetComponent<Game>().bqCastling = true;
                        controller.GetComponent<Game>().bqCastling = true;
                    }
                    else if ((int)(fen[u]) == 'k')
                    {
                    ctl = 1;
                        controller.GetComponent<Game>().BCastling = true;
                        controller.GetComponent<Game>().BCastling = true;
                        controller.GetComponent<Game>().bkCastling = true;
                        controller.GetComponent<Game>().bkCastling = true;
                    }
                    else if ((int)(fen[u]) == ' ' && (int)(fen[u + 1]) == '-')
                    {

                        controller.GetComponent<Game>().EnPassant = false;
                    }
                    else if ((int)(fen[u]) == ' ')
                    {
                        if (EnPassant)
                        {
                            u++;
                            int p = (int)(fen[u]) - (int)('a');
                            u++;
                            int q = (int)(fen[u]) - (int)('1');
                            EPassant = positions[p, q];

                        }

                        u++;
                        MadeMoves = 0;
                        while (u < y && (int)(fen[u]) != ' ')
                        {
                            MadeMoves = (int)(char.GetNumericValue(fen[u])) + MadeMoves * 10;
                            u++;
                        }
                        u++;
                        while (u < y && (int)(fen[u]) != ' ')
                        {
                            FullMoves = (int)(char.GetNumericValue(fen[u])) + FullMoves * 10;
                            u++;
                        }
                    }


                    u++;
                }
            }
            else
            {
                controller.GetComponent<Game>().WCastling = true;
                controller.GetComponent<Game>().BCastling = true;
                controller.GetComponent<Game>().wkCastling = true;
                controller.GetComponent<Game>().wqCastling = true;
                controller.GetComponent<Game>().bkCastling = true;
                controller.GetComponent<Game>().bqCastling = true;
            }
        
    }
    public void SaveFenBoard()
    {
        if (!IsGameOver())
        {
            char[] TheFen = new char[128];
            int u = 0, emptySquareCount = 0;

            for (int j = 7; j >= 0; j--)
            {
                for (int i = 0; i <= 7; i++)
                {
                    if (positions[i, j] != null)
                    {
                        if (emptySquareCount > 0)
                        {
                            TheFen[u++] = (char)('0' + emptySquareCount);
                            emptySquareCount = 0;
                        }

                        switch (positions[i, j].name)
                        {
                            case "white_king":
                                TheFen[u++] = 'K';
                                break;
                            case "white_queen":
                                TheFen[u++] = 'Q';
                                break;
                            case "white_pawn":
                                TheFen[u++] = 'P';
                                break;
                            case "white_knight":
                                TheFen[u++] = 'N';
                                break;
                            case "white_rook":
                                TheFen[u++] = 'R';
                                break;
                            case "white_bishop":
                                TheFen[u++] = 'B';
                                break;
                            case "black_king":
                                TheFen[u++] = 'k';
                                break;
                            case "black_queen":
                                TheFen[u++] = 'q';
                                break;
                            case "black_pawn":
                                TheFen[u++] = 'p';
                                break;
                            case "black_knight":
                                TheFen[u++] = 'n';
                                break;
                            case "black_rook":
                                TheFen[u++] = 'r';
                                break;
                            case "black_bishop":
                                TheFen[u++] = 'b';
                                break;
                        }
                    }
                    else
                    {
                        emptySquareCount++;
                    }
                }

                if (emptySquareCount > 0)
                {
                    TheFen[u++] = (char)('0' + emptySquareCount);
                    emptySquareCount = 0;
                }

                if (j != 0)
                {
                    TheFen[u++] = '/';
                }

            }
            TheFen[u++] = ' ';
            TheFen[u++] = (currentPlayer == "white") ? 'w' : 'b';
            string Fen = new string(TheFen);
            PlayerPrefs.SetString("LastScene", Fen);
        }
        else
            PlayerPrefs.SetString("LastScene", "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
    }
    public string GetMatchFen()
    {
        char[] TheFen = new char[128];
        int u = 0, emptySquareCount = 0;

        for (int j = 7; j >= 0; j--)
        {
            for (int i = 0; i <= 7; i++)
            {
                if (positions[i, j] != null)
                {
                    if (emptySquareCount > 0)
                    {
                        TheFen[u++] = (char)('0' + emptySquareCount);
                        emptySquareCount = 0;
                    }

                    switch (positions[i, j].name)
                    {
                        case "white_king":
                            TheFen[u++] = 'K';
                            break;
                        case "white_queen":
                            TheFen[u++] = 'Q';
                            break;
                        case "white_pawn":
                            TheFen[u++] = 'P';
                            break;
                        case "white_knight":
                            TheFen[u++] = 'N';
                            break;
                        case "white_rook":
                            TheFen[u++] = 'R';
                            break;
                        case "white_bishop":
                            TheFen[u++] = 'B';
                            break;
                        case "black_king":
                            TheFen[u++] = 'k';
                            break;
                        case "black_queen":
                            TheFen[u++] = 'q';
                            break;
                        case "black_pawn":
                            TheFen[u++] = 'p';
                            break;
                        case "black_knight":
                            TheFen[u++] = 'n';
                            break;
                        case "black_rook":
                            TheFen[u++] = 'r';
                            break;
                        case "black_bishop":
                            TheFen[u++] = 'b';
                            break;
                    }
                }
                else
                {
                    emptySquareCount++;
                }
            }

            if (emptySquareCount > 0)
            {
                TheFen[u++] = (char)('0' + emptySquareCount);
                emptySquareCount = 0;
            }

            if (j != 0)
            {
                TheFen[u++] = '/';
            }

        }
        TheFen[u++] = ' ';
        TheFen[u++] = (currentPlayer == "white") ? 'w' : 'b';
        return new string(TheFen);
    }
    public bool GetFiftyMoveRule()
    {
        return (FullMoves == 50);
    }

    public void SetPosition(GameObject piece, int x, int y) 
    { 
        positions[x, y] = piece; 
    }
}