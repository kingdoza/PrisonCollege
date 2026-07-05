using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerKiller : MonoBehaviour
{
    void Awake()
    {
        GameObject temp = GameObject.FindWithTag("Player");
        if(temp!=null) DestroyImmediate(temp);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
