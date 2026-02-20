using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;
    }
    public void LoadScene(string sceneName)
    {
        PlayerPrefs.SetString("Continue", "no");
        PlayerPrefs.SetString("Multiplayer", "no");
        SceneManager.LoadScene(sceneName);
    }

    public void LoadLastGame(string sceneName)
    {
        PlayerPrefs.SetString("Continue","yes");
        PlayerPrefs.SetString("Multiplayer", "no");
        SceneManager.LoadScene(sceneName);
    }
    public void SaveAndClose(string sceneName)
    {
        GameObject controller = GameObject.FindGameObjectWithTag("GameController");
        controller.GetComponent<Game>().SaveFenBoard();
        SceneManager.LoadScene(sceneName);
    }

    public void QuitApplication()
    {
        Debug.Log("s-a inchis");
        Application.Quit();
    }
}