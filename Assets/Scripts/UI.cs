using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public GameObject logo,windowsLogo,windowsLego,windowsBack;
    public GameObject AndroidLogo,AndroidCoolBackground;
    public static bool pressed=false;
    public static bool win = true;
    public static bool DarkMode=false;

    public Text lv;
    public Text location;
    public Text score;
    public GameObject DarkModeObj;
    public AudioClip clip1,clip2;
    public AudioSource audioSource;
    public void Awake()
    {
#if UNITY_STANDALONE_WIN
        if (Random.Range(1, 10) >3)
            windowsLogo.SetActive(true);
        else
        {
            windowsLogo.SetActive(true);
            windowsLego.SetActive(true);
            windowsBack.GetComponent<Image>().color = new Color32(217,16,16,255);
        }
        logo.SetActive(false);
        win = true;
#endif
        if (!win)
        {
            AndroidCoolBackground.SetActive(true);
        }

        
        
        if (DarkMode)
        {
            //Protocol();
            audioSource.clip = clip2;
            audioSource.Play();
        }
    }
    public void Start()
    {   
        if (!pressed)
        {
            StartCoroutine(Wait());
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            logo.SetActive(false);
            windowsLogo.SetActive(false);
        }
    }
    public IEnumerator Wait()
    {
        while (true)
        {
            if (win && (Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.KeypadEnter) || Input.GetKey(KeyCode.Return)))
            {
                break;
            }
            else if (! win && (Input.touchCount!=0 || Input.anyKeyDown))
            {
                break;
            }
            yield return null;
        }
        pressed = true;
        logo.SetActive(false);
        windowsLogo.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        yield break;
    }
    public void Protocol()
    {
        DarkModeObj.SetActive(true);
        int Lv = PlayerPrefs.GetInt("lv");
        if (Lv == 0)
        {
            Lv = 1;
            PlayerPrefs.GetInt("lv", 1);
        }
        lv.text = "LV "+Lv;
        score.text = Lv.ToString() + ":0";
        if (Lv < 5)
        {
            location.text = "Fighting";
        }
        else if (Lv < 10)
        {
            location.text = "Rising";
        }
        else if (Lv < 15)
        {
            location.text = "Withering";
        }
        else if (Lv < 20)
        {
            location.text = "The End";
        }
        else
        {
            location.text = "endgame";
        }

    }
}
