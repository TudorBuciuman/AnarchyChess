using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MoveTrackerUI : MonoBehaviour
{
    public ScrollRect scrollRect;
    public RectTransform content;
    public GameObject moveEntryPrefab;

    public GameObject img;
    private Color whiteColor = new Color(0.95f, 0.95f, 0.95f);
    private Color blackColor = new Color(0.70f, 0.88f, 1.00f);
    private Color moveNumberColor = new Color(0.95f, 0.95f, 0.95f);
    private Color rowEvenColor = new Color(1f, 1f, 1f, 0.04f);
    private Color rowOddColor = new Color(0f, 0f, 0f, 0.10f);


    private readonly List<Text> rowTexts = new List<Text>();
    private int moveNumber = 1;   
    private bool whiteToMove = true; 
    public void RecordMove(
        GameObject piece,
        int fromX, int fromY,
        int toX, int toY,
        bool isCapture = false,
        bool isCheck = false,
        bool isCheckmate = false,
        string castleSide = null,
        string promotionPiece = null)
    {
        string notation = BuildNotation(
            piece, fromX, fromY, toX, toY,
            isCapture, isCheck, isCheckmate, castleSide, promotionPiece);

        AppendNotation(notation);
    }
    public void Resetter(bool show)
    {
        if (!show)
        {
            img.gameObject.SetActive(false);
        }
        else
        {
            StartCoroutine(Colorful());
        }
    }
    public void ResetTracker()
    {
        foreach (Text t in rowTexts)
            if (t != null) Destroy(t.transform.parent.gameObject);

        rowTexts.Clear();
        moveNumber = 1;
        whiteToMove = true;
    }
    public IEnumerator Colorful()
    {
        float t = 0;
        while (t <= 8)
        {
            img.GetComponent<Image>().color = new Color(1,1,1,Mathf.Lerp(0,0.47f,t/8f));
            t += Time.deltaTime;
            yield return null;
        }
        yield return null;
    }

    private string BuildNotation(
        GameObject piece,
        int fromX, int fromY,
        int toX, int toY,
        bool isCapture, bool isCheck, bool isCheckmate,
        string castleSide, string promotionPiece)
    {
        if (!string.IsNullOrEmpty(castleSide))
        {
            string castle = (castleSide == "kingside") ? "O-O" : "O-O-O";
            return castle + CheckSuffix(isCheck, isCheckmate);
        }

        string pieceName = piece != null ? piece.name : "";
        string pieceSymbol = PieceSymbol(pieceName);
        bool isPawn = pieceName.Contains("pawn");

        string fromFile = FileChar(fromX).ToString();
        string toSquare = FileChar(toX).ToString() + RankChar(toY).ToString();

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (isPawn)
        {
            if (isCapture)
                sb.Append(fromFile).Append('x');
            sb.Append(toSquare);
        }
        else
        {
            sb.Append(pieceSymbol);
            sb.Append(fromFile); 
            if (isCapture) sb.Append('x');
            sb.Append(toSquare);
        }

        if (!string.IsNullOrEmpty(promotionPiece))
            sb.Append('=').Append(PieceSymbol(promotionPiece));

        sb.Append(CheckSuffix(isCheck, isCheckmate));

        return sb.ToString();
    }


    private void AppendNotation(string notation)
    {
        if (whiteToMove)
        {
            GameObject row = Instantiate(moveEntryPrefab, content);
            Text txt = row.GetComponentInChildren<Text>();

            Image bg = row.GetComponent<Image>();
            if (bg != null)
                bg.color = (rowTexts.Count % 2 == 0) ? rowEvenColor : rowOddColor;
            if (moveNumber < 10)
                txt.text = " ";
            else
                txt.text = "";
                txt.text += $"<color=#{ColorToHex(moveNumberColor)}>{moveNumber}.</color>";
            txt.text+=$" <color=#{ColorToHex(whiteColor)}>{notation}</color>";
            rowTexts.Add(txt);
        }
        else
        {
            if (rowTexts.Count > 0)
            {       
                Text last = rowTexts[rowTexts.Count - 1];
                last.text += $"\u00A0\u00A0<color=#{ColorToHex(blackColor)}>{notation}</color>";
            }
            moveNumber++;
        }

        whiteToMove = !whiteToMove;
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        StartCoroutine(ScrollNextFrame());
    }

    private System.Collections.IEnumerator ScrollNextFrame()
    {
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;

        yield return new WaitForEndOfFrame();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    private static string PieceSymbol(string pieceName)
    {
        if (pieceName.Contains("knight")) return "N";
        if (pieceName.Contains("bishop")) return "B";
        if (pieceName.Contains("rook")) return "R";
        if (pieceName.Contains("queen")) return "Q";
        if (pieceName.Contains("king")) return "K";
        return ""; 
    }

    private static char FileChar(int x) => (char)('a' + x); 
    private static char RankChar(int y) => (char)('1' + y); 

    private static string CheckSuffix(bool check, bool checkmate)
    {
        if (checkmate) return "#";
        if (check) return "+";
        return "";
    }

    private static string ColorToHex(Color c)
    {
        return ColorUtility.ToHtmlStringRGB(c);
    }
}