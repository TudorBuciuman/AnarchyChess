using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class DuckChessManager : MonoBehaviour
{
    public Sprite duckSprite;

    public GameObject movePlatePrefab;

    public Color duckPlateColor = new Color(1f, 0.85f, 0f, 0.75f); // golden yellow

    public AudioClip clip;

    public int DuckX { get; private set; } = -1;
    public int DuckY { get; private set; } = -1;

    public bool WaitingForDuckPlacement { get; private set; } = false;

    private GameObject duckObject;

    private List<GameObject> duckPlates = new List<GameObject>();

    private const string DUCK_PLATE_TAG = "DuckPlate";

    private const float DUCK_Z = 79f;
    private const float DUCK_PLATE_Z = 78f;

    public void SpawnDuck()
    {
        if (Game.currentMinigame != Game.Minigame.Duck) return;

        Game game = GetComponent<Game>();

        int x, y;
        int attempts = 0;
        do
        {
            x = UnityEngine.Random.Range(0, 8);
            y = UnityEngine.Random.Range(0, 8);
            attempts++;
        }
        while (game.GetPosition(x, y) != null && attempts < 2000);

        if (attempts >= 2000)
        {
            Debug.LogError("Horsey could not find an empty square!");
            return;
        }

        DuckX = x;
        DuckY = y;

        CreateDuckObject();
        PositionDuckObject();
        BlockDuckSquare(game);

        Debug.Log($"Horsey spawned at ({DuckX},{DuckY})");
    }

    public void OnPieceMoved()
    {
        if (Game.currentMinigame != Game.Minigame.Duck) return;

        GameObject a = GameObject.FindGameObjectWithTag("Duck");
        Game game = GetComponent<Game>();
        a.GetComponent<Chessman>().player = game.currentPlayer;
        WaitingForDuckPlacement = true;
        ShowDuckPlacementPlates();
    }

    public void OnBotPieceMoved()
    {
        if (Game.currentMinigame != Game.Minigame.Duck) return;

        Game game = GetComponent<Game>();
        UnblockDuckSquare(game);

        List<(int, int)> candidates = new List<(int, int)>();
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
                if (game.GetPosition(x, y) == null && !(x == DuckX && y == DuckY))
                    candidates.Add((x, y));

        if (candidates.Count == 0)
        {
            BlockDuckSquare(game);
            return;
        }

        var chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        PlaceDuck(chosen.Item1, chosen.Item2, isBot: true);
    }

    public void PlaceDuck(int tx, int ty, bool isBot = false)
    {
        if (!isBot)
        {
            Game game = GetComponent<Game>();

            UnblockDuckSquare(game);

            DuckX = tx;
            DuckY = ty;

            BlockDuckSquare(game);
            PositionDuckObject(animate: true);
            DestroyDuckPlates();

            WaitingForDuckPlacement = false;
            game.NextTurn();
            GameObject controller = GameObject.FindGameObjectWithTag("GameController");
            controller.GetComponent<ChessBot>().BotTurn();
        }
        else
        {
            StartCoroutine(Wait(2, tx, ty));
        }
    }
    public IEnumerator Wait(float time, int tx, int ty)
    {
        yield return new WaitForSeconds(time);
        Game game = GetComponent<Game>();

        UnblockDuckSquare(game);

        DuckX = tx;
        DuckY = ty;

        BlockDuckSquare(game);
        PositionDuckObject(animate: true);
        DestroyDuckPlates();

        WaitingForDuckPlacement = false;

        game.NextTurn();

        yield return null;
    }
    public bool IsDuckAt(int x, int y) => (DuckX == x && DuckY == y);

    public bool IsPlacingDuck() => WaitingForDuckPlacement;
    private void CreateDuckObject()
    {
        if (duckObject != null) Destroy(duckObject);

        duckObject = new GameObject("duck");
        duckObject.AddComponent<AudioSource>();
        duckObject.AddComponent<Chessman>();
        duckObject.GetComponent<Chessman>().Activate();
        duckObject.GetComponent<Chessman>().player = GameObject.FindFirstObjectByType<Game>().currentPlayer;
        duckObject.tag = "Duck";

        SpriteRenderer sr = duckObject.AddComponent<SpriteRenderer>();
        if (duckSprite != null)
        {
            sr.sprite = duckSprite;
        }
        else
        {
            if (movePlatePrefab != null)
                sr.sprite = movePlatePrefab.GetComponent<SpriteRenderer>().sprite;
            sr.color = new Color(1f, 0.8f, 0f, 1f);
        }
        sr.sortingOrder = 4000;

        float scale = Game.multiplier / 2.4f / 4f;
        duckObject.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void PositionDuckObject(bool animate = false)
    {
        if (duckObject == null) return;

        float boardSize = (2048f * Game.multiplier) / 100f;
        float squareSize = boardSize / 8f;
        float halfBoard = (boardSize / 2f) - (squareSize / 2f);
        int r = Game.white ? 1 : -1;

        float wx = (DuckX * squareSize) - halfBoard;
        float wy = (DuckY * squareSize) - halfBoard;
        Vector3 target = new Vector3(wx * r, wy * r, DUCK_Z);

        duckObject.GetComponent<AudioSource>().clip = clip;
        duckObject.GetComponent<AudioSource>().Play();

        if (animate)
            StartCoroutine(AnimateDuckMove(target));
        else
            duckObject.transform.position = target;
    }

    private IEnumerator AnimateDuckMove(Vector3 target)
    {
        if (duckObject == null) yield break;

        Vector3 start = duckObject.transform.position;
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (duckObject == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            float arc = Mathf.Sin(Mathf.PI * t) * 0.3f;
            Vector3 pos = Vector3.Lerp(start, target, t);
            pos.y += arc;
            pos.z = DUCK_Z;

            duckObject.transform.position = pos;
            yield return null;
        }

        if (duckObject != null)
            duckObject.transform.position = target;
    }

    private void BlockDuckSquare(Game game)
    {
        if (DuckX < 0) return;
        game.positions[DuckX, DuckY] = duckObject;
    }

    private void UnblockDuckSquare(Game game)
    {
        if (DuckX < 0 || duckObject == null)
            return;
        if (game.positions[DuckX, DuckY] == duckObject)
            game.positions[DuckX, DuckY] = null;
    }

    private void ShowDuckPlacementPlates()
    {
        DestroyDuckPlates();

        if (movePlatePrefab == null)
        {
            Debug.LogError("Horsey movePlatePrefab not assigned on DuckChessManager! Fuck you!");
            return;
        }

        Game game = GetComponent<Game>();

        float boardSize = (2048f * Game.multiplier) / 100f;
        float squareSize = boardSize / 8f;
        float halfBoard = (boardSize / 2f) - (squareSize / 2f);
        int r = Game.white ? 1 : -1;

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (game.GetPosition(x, y) != null)
                    continue;
                if (x == DuckX && y == DuckY)
                    continue;

                float wx = (x * squareSize) - halfBoard;
                float wy = (y * squareSize) - halfBoard;

                GameObject plate = Instantiate(
                    movePlatePrefab,
                    new Vector3(wx * r, wy * r, DUCK_PLATE_Z),
                    Quaternion.identity);

                plate.transform.localScale = new Vector2(
                    plate.transform.localScale.x * Game.multiplier / 2.4f,
                    plate.transform.localScale.y * Game.multiplier / 2.4f);

                plate.GetComponent<SpriteRenderer>().color = duckPlateColor;
                plate.tag = DUCK_PLATE_TAG;

                DuckPlate dp = plate.AddComponent<DuckPlate>();
                dp.targetX = x;
                dp.targetY = y;
                dp.manager = this;

                duckPlates.Add(plate);
            }
        }
    }

    private void DestroyDuckPlates()
    {
        foreach (GameObject p in duckPlates)
            if (p != null) Destroy(p);
        duckPlates.Clear();

        GameObject[] stragglers = GameObject.FindGameObjectsWithTag(DUCK_PLATE_TAG);
        foreach (GameObject s in stragglers)
            Destroy(s);
    }
}

public class DuckPlate : MonoBehaviour
{
    public int targetX;
    public int targetY;
    public DuckChessManager manager;

    private void OnMouseUp()
    {
        if (manager != null)
        {
            manager.PlaceDuck(targetX, targetY, isBot: false);
        }
    }
}