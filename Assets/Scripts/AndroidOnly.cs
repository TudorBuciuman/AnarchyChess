using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AndroidOnly : MonoBehaviour
{
    public GameObject androidonly;
    public GameObject winonly;
    void Awake()
    {
        if (!UI.win)
        {
            androidonly.SetActive(true);
            winonly.SetActive(false);
        }    
    }
}
