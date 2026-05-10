using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
public class Settings : MonoBehaviour
{
    public GameObject Game;
    public GameObject Audio;
    public GameObject Video;
    public GameObject Rules;
    public Image Selected;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseSettings();
        }
    }
    public void ChooseSetting(int choice)
    {
        Game.SetActive(false);
        Audio.SetActive(false);
        Video.SetActive(false);
        Rules.SetActive(false);
        switch (choice)
        {
            case 1:
                Audio.SetActive(true);
                Selected.rectTransform.anchoredPosition = new Vector2(-816, 200);
                break;
            case 2:
                Video.SetActive(true);
                Selected.rectTransform.anchoredPosition = new Vector2(-816, 63);
                break;
            case 3:
                Rules.SetActive(true);
                Selected.rectTransform.anchoredPosition = new Vector2(-816, -56);
                break;
            default:
                Game.SetActive(true);
                Selected.rectTransform.anchoredPosition = new Vector2(-816, 320);
                break;
        }
    }
    
   
    
    public IEnumerator Fade(float time)
    {
        AudioSource a = FindFirstObjectByType<AudioSource>();
        float startAlpha = 1f;
        float endAlpha = 0f;
        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            a.volume = Mathf.Lerp(startAlpha, endAlpha, elapsed / time);
            yield return null;
        }

        a.volume = 0;
    }
    public void CloseSettings()
    {
        SceneManager.LoadScene("Game UI");
    }
}
