using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MinigameAnnouncer : MonoBehaviour
{
    public Text displaySelection;
    public float displayTime = 2.0f;
    public float fadeDuration = 4.0f;

    private Coroutine fadeCoroutine;

    void Awake()
    {
        if (displaySelection != null)
        {
            Color c = displaySelection.color;
            c.a = 0;
            displaySelection.color = c;
        }
    }

    public void ShowMinigame(string name)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeSequence(name));
    }

    private IEnumerator FadeSequence(string name)
    {
        displaySelection.text = name;
        if(Game.matchTheme==Game.Colors.white)
        displaySelection.GetComponent<Text>().color = Color.black;

        yield return StartCoroutine(Fade(0, 1));

        yield return new WaitForSeconds(displayTime);

        yield return StartCoroutine(Fade(1, 0));
    }

    private IEnumerator Fade(float start, float end)
    {
        float elapsed = 0;
        Color c = displaySelection.color;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(start, end, elapsed / fadeDuration);
            displaySelection.color = c;
            yield return null;
        }
    }
}